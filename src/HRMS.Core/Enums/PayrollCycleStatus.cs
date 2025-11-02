namespace HRMS.Core.Enums;

/// <summary>
/// Payroll cycle processing status
/// </summary>
public enum PayrollCycleStatus
{
    /// <summary>
    /// Draft - cycle created, not yet processed
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Processing - calculations in progress
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Calculated - processing complete, awaiting approval
    /// </summary>
    Calculated = 3,

    /// <summary>
    /// Approved - verified and approved for payment
    /// </summary>
    Approved = 4,

    /// <summary>
    /// Paid - all payments completed
    /// </summary>
    Paid = 5,

    /// <summary>
    /// Cancelled - cycle cancelled
    /// </summary>
    Cancelled = 6
}
