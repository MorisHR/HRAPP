namespace HRMS.Core.Enums;

/// <summary>
/// Timesheet workflow status
/// </summary>
public enum TimesheetStatus
{
    /// <summary>
    /// Initial state - can be edited by employee
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Submitted for manager approval
    /// </summary>
    Submitted = 2,

    /// <summary>
    /// Approved by manager - ready for payroll
    /// </summary>
    Approved = 3,

    /// <summary>
    /// Rejected by manager - needs corrections
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Locked for payroll processing - immutable
    /// </summary>
    Locked = 5
}
