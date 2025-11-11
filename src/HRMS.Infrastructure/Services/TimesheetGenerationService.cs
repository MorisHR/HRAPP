using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for auto-generating timesheets from attendance records
/// Implements Mauritius labor law overtime calculations
/// </summary>
public class TimesheetGenerationService : ITimesheetGenerationService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<TimesheetGenerationService> _logger;

    public TimesheetGenerationService(
        TenantDbContext context,
        ILogger<TimesheetGenerationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> GenerateTimesheetsForPeriodAsync(
        DateTime periodStart,
        DateTime periodEnd,
        PeriodType periodType,
        Guid tenantId)
    {
        _logger.LogInformation(
            "Generating timesheets for all employees. Period: {Start} to {End}, Type: {Type}",
            periodStart, periodEnd, periodType);

        // Get all active employees
        var employees = await _context.Employees
            .Where(e => !e.IsDeleted && e.EmploymentStatus == "Active")
            .ToListAsync();

        int generatedCount = 0;

        foreach (var employee in employees)
        {
            try
            {
                await GenerateTimesheetForEmployeeAsync(
                    employee.Id,
                    periodStart,
                    periodEnd,
                    periodType);

                generatedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to generate timesheet for employee {EmployeeId} ({EmployeeName})",
                    employee.Id, employee.FullName);
            }
        }

        _logger.LogInformation(
            "Generated {Count} timesheets out of {Total} employees",
            generatedCount, employees.Count);

        return generatedCount;
    }

    public async Task<Timesheet> GenerateTimesheetForEmployeeAsync(
        Guid employeeId,
        DateTime periodStart,
        DateTime periodEnd,
        PeriodType periodType)
    {
        _logger.LogInformation(
            "Generating timesheet for employee {EmployeeId} for period {Start} to {End}",
            employeeId, periodStart, periodEnd);

        // Check if timesheet already exists
        var existingTimesheet = await _context.Timesheets
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t =>
                t.EmployeeId == employeeId &&
                t.PeriodStart == periodStart &&
                t.PeriodEnd == periodEnd &&
                !t.IsDeleted);

        if (existingTimesheet != null)
        {
            // If already submitted or approved, don't regenerate
            if (existingTimesheet.Status != TimesheetStatus.Draft)
            {
                _logger.LogWarning(
                    "Timesheet {TimesheetId} already exists with status {Status}. Skipping.",
                    existingTimesheet.Id, existingTimesheet.Status);
                return existingTimesheet;
            }

            // Regenerate for Draft status
            return await RegenerateTimesheetAsync(existingTimesheet.Id);
        }

        // Get employee details
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            throw new InvalidOperationException($"Employee {employeeId} not found");
        }

        // Create new timesheet
        var timesheet = new Timesheet
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PeriodType = periodType,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Status = TimesheetStatus.Draft,
            TenantId = Guid.Empty, // TenantId will be set by tenant context
            CreatedAt = DateTime.UtcNow
        };

        // Get attendance records for the period
        var attendanceRecords = await _context.Attendances
            .Where(a =>
                a.EmployeeId == employeeId &&
                a.Date >= periodStart &&
                a.Date <= periodEnd &&
                !a.IsDeleted)
            .OrderBy(a => a.Date)
            .ToListAsync();

        // Get public holidays for the period
        var publicHolidays = await _context.PublicHolidays
            .Where(h =>
                h.Date >= periodStart &&
                h.Date <= periodEnd &&
                !h.IsDeleted)
            .Select(h => h.Date.Date)
            .ToListAsync();

        // Get leave applications for the period
        var leaveApplications = await _context.LeaveApplications
            .Where(la =>
                la.EmployeeId == employeeId &&
                la.Status == LeaveStatus.Approved &&
                la.StartDate <= periodEnd &&
                la.EndDate >= periodStart &&
                !la.IsDeleted)
            .Include(la => la.LeaveType)
            .ToListAsync();

        // Generate entries for each day in the period
        var currentDate = periodStart.Date;
        while (currentDate <= periodEnd.Date)
        {
            var entry = await GenerateTimesheetEntryForDateAsync(
                timesheet.Id,
                currentDate,
                attendanceRecords.FirstOrDefault(a => a.Date.Date == currentDate),
                publicHolidays.Contains(currentDate),
                leaveApplications.FirstOrDefault(la =>
                    currentDate >= la.StartDate.Date &&
                    currentDate <= la.EndDate.Date));

            timesheet.Entries.Add(entry);
            currentDate = currentDate.AddDays(1);
        }

        // Apply overtime rules
        await ApplyOvertimeRulesAsync(timesheet, employee);

        // Calculate totals
        timesheet.CalculateTotals();

        // Save to database
        _context.Timesheets.Add(timesheet);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Timesheet {TimesheetId} generated for employee {EmployeeId} with {EntryCount} entries",
            timesheet.Id, employeeId, timesheet.Entries.Count);

        return timesheet;
    }

    public async Task<Timesheet> RegenerateTimesheetAsync(Guid timesheetId)
    {
        _logger.LogInformation("Regenerating timesheet {TimesheetId}", timesheetId);

        var timesheet = await _context.Timesheets
            .Include(t => t.Entries)
            .Include(t => t.Employee)
            .ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(t => t.Id == timesheetId && !t.IsDeleted);

        if (timesheet == null)
        {
            throw new InvalidOperationException($"Timesheet {timesheetId} not found");
        }

        if (timesheet.Status != TimesheetStatus.Draft)
        {
            throw new InvalidOperationException(
                "Can only regenerate Draft timesheets. Current status: " + timesheet.Status);
        }

        // Remove existing entries
        _context.TimesheetEntries.RemoveRange(timesheet.Entries);
        timesheet.Entries.Clear();

        // Regenerate like new
        var attendanceRecords = await _context.Attendances
            .Where(a =>
                a.EmployeeId == timesheet.EmployeeId &&
                a.Date >= timesheet.PeriodStart &&
                a.Date <= timesheet.PeriodEnd &&
                !a.IsDeleted)
            .OrderBy(a => a.Date)
            .ToListAsync();

        var publicHolidays = await _context.PublicHolidays
            .Where(h =>
                h.Date >= timesheet.PeriodStart &&
                h.Date <= timesheet.PeriodEnd &&
                !h.IsDeleted)
            .Select(h => h.Date.Date)
            .ToListAsync();

        var leaveApplications = await _context.LeaveApplications
            .Where(la =>
                la.EmployeeId == timesheet.EmployeeId &&
                la.Status == LeaveStatus.Approved &&
                la.StartDate <= timesheet.PeriodEnd &&
                la.EndDate >= timesheet.PeriodStart &&
                !la.IsDeleted)
            .Include(la => la.LeaveType)
            .ToListAsync();

        var currentDate = timesheet.PeriodStart.Date;
        while (currentDate <= timesheet.PeriodEnd.Date)
        {
            var entry = await GenerateTimesheetEntryForDateAsync(
                timesheet.Id,
                currentDate,
                attendanceRecords.FirstOrDefault(a => a.Date.Date == currentDate),
                publicHolidays.Contains(currentDate),
                leaveApplications.FirstOrDefault(la =>
                    currentDate >= la.StartDate.Date &&
                    currentDate <= la.EndDate.Date));

            timesheet.Entries.Add(entry);
            currentDate = currentDate.AddDays(1);
        }

        // Apply overtime rules
        await ApplyOvertimeRulesAsync(timesheet, timesheet.Employee!);

        // Recalculate totals
        timesheet.CalculateTotals();
        timesheet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Timesheet {TimesheetId} regenerated successfully", timesheetId);

        return timesheet;
    }

    public async Task<Timesheet> GetOrCreateTimesheetAsync(
        Guid employeeId,
        DateTime periodStart,
        DateTime periodEnd,
        PeriodType periodType)
    {
        var existing = await _context.Timesheets
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t =>
                t.EmployeeId == employeeId &&
                t.PeriodStart == periodStart &&
                t.PeriodEnd == periodEnd &&
                !t.IsDeleted);

        if (existing != null)
        {
            return existing;
        }

        return await GenerateTimesheetForEmployeeAsync(
            employeeId, periodStart, periodEnd, periodType);
    }

    public async Task<decimal> GetOvertimeThresholdForEmployeeAsync(Guid employeeId)
    {
        // Get employee's department and sector configuration
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            return 40m; // Default to 40 hours
        }

        // Get tenant sector configuration
        var sectorConfig = await _context.TenantSectorConfigurations
            .FirstOrDefaultAsync();

        if (sectorConfig == null)
        {
            return 40m; // Default
        }

        // Check sector code for industry-specific rules
        var sectorCode = sectorConfig.SectorCode?.ToUpper() ?? "";

        // Mauritius Labor Law:
        // Manufacturing, Shops, Hotels: 45 hours/week
        // Others: 40 hours/week
        if (sectorCode.Contains("MANUFACTURING") ||
            sectorCode.Contains("SHOP") ||
            sectorCode.Contains("RETAIL") ||
            sectorCode.Contains("HOTEL") ||
            sectorCode.Contains("HOSPITALITY"))
        {
            return 45m;
        }

        return 40m;
    }

    private Task<TimesheetEntry> GenerateTimesheetEntryForDateAsync(
        Guid timesheetId,
        DateTime date,
        Attendance? attendance,
        bool isPublicHoliday,
        LeaveApplication? leaveApplication)
    {
        var entry = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = timesheetId,
            Date = date,
            AttendanceId = attendance?.Id,
            IsWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday,
            IsHoliday = isPublicHoliday,
            IsOnLeave = leaveApplication != null,
            CreatedAt = DateTime.UtcNow
        };

        // Handle leave days
        if (leaveApplication != null)
        {
            var leaveTypeCode = leaveApplication.LeaveType?.TypeCode;

            if (leaveTypeCode == LeaveTypeEnum.AnnualLeave)
            {
                entry.AnnualLeaveHours = 8m;
                entry.DayType = DayType.AnnualLeave;
            }
            else if (leaveTypeCode == LeaveTypeEnum.SickLeave)
            {
                entry.SickLeaveHours = 8m;
                entry.DayType = DayType.SickLeave;
            }
            else if (leaveTypeCode == LeaveTypeEnum.CasualLeave)
            {
                entry.AnnualLeaveHours = 8m; // Treat casual as annual
                entry.DayType = DayType.CasualLeave;
            }
            else
            {
                entry.DayType = DayType.UnpaidLeave;
            }

            entry.ActualHours = 8m;
            return Task.FromResult(entry);
        }

        // Handle attendance records
        if (attendance != null)
        {
            entry.ClockInTime = attendance.CheckInTime;
            entry.ClockOutTime = attendance.CheckOutTime;

            // Calculate hours
            entry.CalculateHours();

            if (isPublicHoliday)
            {
                entry.DayType = DayType.Holiday;
            }
            else if (entry.IsWeekend)
            {
                entry.DayType = DayType.Weekend;
            }
            else
            {
                entry.DayType = DayType.Regular;
            }
        }
        else
        {
            // No attendance record
            if (isPublicHoliday)
            {
                entry.DayType = DayType.Holiday;
                // Holiday - no work expected
            }
            else if (entry.IsWeekend)
            {
                entry.DayType = DayType.Weekend;
                // Weekend - no work expected
            }
            else
            {
                // Regular workday with no attendance = absent
                entry.IsAbsent = true;
                entry.DayType = DayType.Absent;
            }
        }

        return Task.FromResult(entry);
    }

    private async Task ApplyOvertimeRulesAsync(Timesheet timesheet, Employee employee)
    {
        // Get weekly overtime threshold
        var weeklyThreshold = await GetOvertimeThresholdForEmployeeAsync(employee.Id);

        // Calculate total regular hours for the week
        var totalRegularHours = timesheet.Entries
            .Where(e => !e.IsHoliday && !e.IsWeekend && !e.IsOnLeave)
            .Sum(e => e.RegularHours);

        // If over threshold, move excess to overtime
        if (totalRegularHours > weeklyThreshold)
        {
            var excessHours = totalRegularHours - weeklyThreshold;

            // Apply overtime to the last entries with regular hours
            foreach (var entry in timesheet.Entries
                .Where(e => e.RegularHours > 0)
                .OrderByDescending(e => e.Date))
            {
                if (excessHours <= 0) break;

                if (entry.RegularHours >= excessHours)
                {
                    entry.OvertimeHours += excessHours;
                    entry.RegularHours -= excessHours;
                    excessHours = 0;
                }
                else
                {
                    entry.OvertimeHours += entry.RegularHours;
                    excessHours -= entry.RegularHours;
                    entry.RegularHours = 0;
                }
            }
        }
    }
}
