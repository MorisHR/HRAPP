using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.BackgroundJobs.Jobs;

/// <summary>
/// Background job to accrue leave balances for employees
/// Runs monthly on the 1st of each month at 1:00 AM
/// Accrues leave based on:
/// - Annual Leave: 22 days/year = 1.83 days/month
/// - Sick Leave: 15 days/year = 1.25 days/month (as per Mauritius Labour Law)
/// </summary>
public class LeaveAccrualJob
{
    private readonly ILogger<LeaveAccrualJob> _logger;
    private readonly MasterDbContext _masterContext;

    // Mauritius Labour Law 2025 - Leave Entitlements
    private const decimal ANNUAL_LEAVE_DAYS_PER_YEAR = 22m;
    private const decimal SICK_LEAVE_DAYS_PER_YEAR = 15m;
    private const decimal CASUAL_LEAVE_DAYS_PER_YEAR = 10m;

    private const decimal ANNUAL_LEAVE_MONTHLY_ACCRUAL = ANNUAL_LEAVE_DAYS_PER_YEAR / 12m; // 1.83
    private const decimal SICK_LEAVE_MONTHLY_ACCRUAL = SICK_LEAVE_DAYS_PER_YEAR / 12m;     // 1.25
    private const decimal CASUAL_LEAVE_MONTHLY_ACCRUAL = CASUAL_LEAVE_DAYS_PER_YEAR / 12m; // 0.83

    public LeaveAccrualJob(
        ILogger<LeaveAccrualJob> logger,
        MasterDbContext masterContext)
    {
        _logger = logger;
        _masterContext = masterContext;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Leave Accrual Job started at {Time}", DateTime.UtcNow);

        try
        {
            // Get all active tenants
            var tenants = await _masterContext.Tenants
                .Where(t => t.Status == Core.Enums.TenantStatus.Active && !t.IsDeleted)
                .ToListAsync();

            int totalEmployeesProcessed = 0;

            foreach (var tenant in tenants)
            {
                _logger.LogInformation("Processing leave accrual for tenant: {TenantName}", tenant.CompanyName);

                var employeesProcessed = await ProcessTenantLeaveAccrualAsync(tenant.SchemaName);
                totalEmployeesProcessed += employeesProcessed;
            }

            _logger.LogInformation("Leave Accrual Job completed. Total employees processed: {Count}", totalEmployeesProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Leave Accrual Job");
            throw;
        }
    }

    private async Task<int> ProcessTenantLeaveAccrualAsync(string schemaName)
    {
        // Create tenant-specific context
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        var connectionString = _masterContext.Database.GetConnectionString();
        optionsBuilder.UseNpgsql(connectionString);

        using var tenantContext = new TenantDbContext(optionsBuilder.Options, schemaName);

        int employeesProcessed = 0;
        var today = DateTime.UtcNow.Date;

        // Get all leave types for this tenant
        var leaveTypes = await tenantContext.LeaveTypes
            .Where(lt => !lt.IsDeleted)
            .ToListAsync();

        if (!leaveTypes.Any())
        {
            _logger.LogWarning("No leave types found for schema {Schema}. Skipping leave accrual.", schemaName);
            return 0;
        }

        // Find Annual Leave, Sick Leave, and Casual Leave types
        var annualLeaveType = leaveTypes.FirstOrDefault(lt => lt.TypeCode == LeaveTypeEnum.AnnualLeave);
        var sickLeaveType = leaveTypes.FirstOrDefault(lt => lt.TypeCode == LeaveTypeEnum.SickLeave);
        var casualLeaveType = leaveTypes.FirstOrDefault(lt => lt.TypeCode == LeaveTypeEnum.CasualLeave);

        // Get all active employees
        var activeEmployees = await tenantContext.Employees
            .Where(e => !e.IsDeleted)
            .ToListAsync();

        foreach (var employee in activeEmployees)
        {
            try
            {
                bool balanceUpdated = false;

                // Calculate employment duration (for probation checks, etc.)
                var employmentMonths = (today - employee.JoiningDate).Days / 30.0;

                // Skip employees who just joined (less than 1 month)
                if (employmentMonths < 1)
                {
                    _logger.LogDebug("Employee {EmployeeName} joined less than 1 month ago. Skipping accrual.",
                        $"{employee.FirstName} {employee.LastName}");
                    continue;
                }

                // Accrue Annual Leave
                if (annualLeaveType != null)
                {
                    var annualLeaveBalance = await GetOrCreateLeaveBalanceAsync(
                        tenantContext, employee.Id, annualLeaveType.Id);

                    annualLeaveBalance.Accrued += ANNUAL_LEAVE_MONTHLY_ACCRUAL;
                    annualLeaveBalance.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation("Accrued {Days} days of annual leave for employee {EmployeeName}. New accrued: {Balance}",
                        ANNUAL_LEAVE_MONTHLY_ACCRUAL, $"{employee.FirstName} {employee.LastName}",
                        annualLeaveBalance.Accrued);

                    balanceUpdated = true;
                }

                // Accrue Sick Leave
                if (sickLeaveType != null)
                {
                    var sickLeaveBalance = await GetOrCreateLeaveBalanceAsync(
                        tenantContext, employee.Id, sickLeaveType.Id);

                    sickLeaveBalance.Accrued += SICK_LEAVE_MONTHLY_ACCRUAL;
                    sickLeaveBalance.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation("Accrued {Days} days of sick leave for employee {EmployeeName}. New accrued: {Balance}",
                        SICK_LEAVE_MONTHLY_ACCRUAL, $"{employee.FirstName} {employee.LastName}",
                        sickLeaveBalance.Accrued);

                    balanceUpdated = true;
                }

                // Accrue Casual Leave (if applicable)
                if (casualLeaveType != null)
                {
                    var casualLeaveBalance = await GetOrCreateLeaveBalanceAsync(
                        tenantContext, employee.Id, casualLeaveType.Id);

                    casualLeaveBalance.Accrued += CASUAL_LEAVE_MONTHLY_ACCRUAL;
                    casualLeaveBalance.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation("Accrued {Days} days of casual leave for employee {EmployeeName}. New accrued: {Balance}",
                        CASUAL_LEAVE_MONTHLY_ACCRUAL, $"{employee.FirstName} {employee.LastName}",
                        casualLeaveBalance.Accrued);

                    balanceUpdated = true;
                }

                if (balanceUpdated)
                {
                    employeesProcessed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing leave accrual for employee {EmployeeId}", employee.Id);
                // Continue with next employee
            }
        }

        // Save all changes
        if (employeesProcessed > 0)
        {
            await tenantContext.SaveChangesAsync();
        }

        return employeesProcessed;
    }

    private async Task<Core.Entities.Tenant.LeaveBalance> GetOrCreateLeaveBalanceAsync(
        TenantDbContext context, Guid employeeId, Guid leaveTypeId)
    {
        // Try to find existing balance
        var balance = await context.LeaveBalances
            .FirstOrDefaultAsync(lb =>
                lb.EmployeeId == employeeId &&
                lb.LeaveTypeId == leaveTypeId &&
                !lb.IsDeleted);

        if (balance == null)
        {
            // Create new balance if not exists
            balance = new Core.Entities.Tenant.LeaveBalance
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                Year = DateTime.UtcNow.Year,
                TotalEntitlement = 0, // Will be set based on leave type
                Accrued = 0,
                UsedDays = 0,
                CarriedForward = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            context.LeaveBalances.Add(balance);
        }

        return balance;
    }
}
