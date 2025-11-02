using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.BackgroundJobs.Jobs;

/// <summary>
/// Background job to automatically mark employees as absent
/// Runs daily after shift times (e.g., 11:00 PM)
/// Marks employees absent if:
/// - No attendance record for the day
/// - Not on approved leave
/// - Not a public holiday
/// </summary>
public class AbsentMarkingJob
{
    private readonly ILogger<AbsentMarkingJob> _logger;
    private readonly MasterDbContext _masterContext;

    public AbsentMarkingJob(
        ILogger<AbsentMarkingJob> logger,
        MasterDbContext masterContext)
    {
        _logger = logger;
        _masterContext = masterContext;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Absent Marking Job started at {Time}", DateTime.UtcNow);

        try
        {
            // Get all active tenants
            var tenants = await _masterContext.Tenants
                .Where(t => t.Status == Core.Enums.TenantStatus.Active && !t.IsDeleted)
                .ToListAsync();

            int totalAbsentMarked = 0;

            foreach (var tenant in tenants)
            {
                _logger.LogInformation("Processing absent marking for tenant: {TenantName}", tenant.CompanyName);

                var absentMarked = await ProcessTenantAbsentMarkingAsync(tenant.SchemaName);
                totalAbsentMarked += absentMarked;
            }

            _logger.LogInformation("Absent Marking Job completed. Total employees marked absent: {Count}", totalAbsentMarked);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Absent Marking Job");
            throw;
        }
    }

    private async Task<int> ProcessTenantAbsentMarkingAsync(string schemaName)
    {
        // Create tenant-specific context
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        var connectionString = _masterContext.Database.GetConnectionString();
        optionsBuilder.UseNpgsql(connectionString);

        using var tenantContext = new TenantDbContext(optionsBuilder.Options, schemaName);

        int absentMarked = 0;
        var today = DateTime.UtcNow.Date;

        // Check if today is a public holiday
        var isPublicHoliday = await tenantContext.PublicHolidays
            .AnyAsync(ph => ph.Date.Date == today && !ph.IsDeleted);

        if (isPublicHoliday)
        {
            _logger.LogInformation("Today is a public holiday in schema {Schema}. Skipping absent marking.", schemaName);
            return 0;
        }

        // Get all active employees
        var activeEmployees = await tenantContext.Employees
            .Where(e => !e.IsDeleted)
            .ToListAsync();

        foreach (var employee in activeEmployees)
        {
            try
            {
                // Check if employee has an attendance record for today
                var hasAttendanceRecord = await tenantContext.Attendances
                    .AnyAsync(a => a.EmployeeId == employee.Id && a.Date.Date == today);

                if (hasAttendanceRecord)
                {
                    // Employee has already checked in or has an attendance record
                    continue;
                }

                // Check if employee is on approved leave today
                var isOnLeave = await tenantContext.LeaveApplications
                    .AnyAsync(la =>
                        la.EmployeeId == employee.Id &&
                        la.Status == LeaveStatus.Approved &&
                        la.StartDate.Date <= today &&
                        la.EndDate.Date >= today &&
                        !la.IsDeleted);

                if (isOnLeave)
                {
                    // Employee is on approved leave
                    _logger.LogDebug("Employee {EmployeeName} is on approved leave. Skipping absent marking.",
                        $"{employee.FirstName} {employee.LastName}");
                    continue;
                }

                // Employee should have been present but has no attendance record
                // Mark as absent
                var absentRecord = new Core.Entities.Tenant.Attendance
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employee.Id,
                    Date = today,
                    Status = AttendanceStatus.Absent,
                    CheckInTime = null,
                    CheckOutTime = null,
                    WorkingHours = 0,
                    OvertimeHours = 0,
                    Remarks = "Automatically marked absent by system",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                tenantContext.Attendances.Add(absentRecord);
                absentMarked++;

                _logger.LogInformation("Marked employee {EmployeeName} ({EmployeeId}) as absent for {Date}",
                    $"{employee.FirstName} {employee.LastName}", employee.Id, today.ToShortDateString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing absent marking for employee {EmployeeId}", employee.Id);
                // Continue with next employee
            }
        }

        // Save all changes
        if (absentMarked > 0)
        {
            await tenantContext.SaveChangesAsync();
        }

        return absentMarked;
    }
}
