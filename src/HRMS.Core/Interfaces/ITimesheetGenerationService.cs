using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;

namespace HRMS.Core.Interfaces;

/// <summary>
/// Service for auto-generating timesheets from attendance records
/// </summary>
public interface ITimesheetGenerationService
{
    /// <summary>
    /// Auto-generate timesheets for all active employees for given period
    /// </summary>
    Task<int> GenerateTimesheetsForPeriodAsync(
        DateTime periodStart,
        DateTime periodEnd,
        PeriodType periodType,
        Guid tenantId);

    /// <summary>
    /// Generate or regenerate timesheet for single employee
    /// </summary>
    Task<Timesheet> GenerateTimesheetForEmployeeAsync(
        Guid employeeId,
        DateTime periodStart,
        DateTime periodEnd,
        PeriodType periodType);

    /// <summary>
    /// Regenerate timesheet if changes in attendance (only for Draft status)
    /// </summary>
    Task<Timesheet> RegenerateTimesheetAsync(Guid timesheetId);

    /// <summary>
    /// Get existing timesheet or create new one for employee and period
    /// </summary>
    Task<Timesheet> GetOrCreateTimesheetAsync(
        Guid employeeId,
        DateTime periodStart,
        DateTime periodEnd,
        PeriodType periodType);

    /// <summary>
    /// Calculate weekly overtime threshold based on employee's industry sector
    /// Manufacturing/Shops/Hotels: 45 hrs, Others: 40 hrs
    /// </summary>
    Task<decimal> GetOvertimeThresholdForEmployeeAsync(Guid employeeId);
}
