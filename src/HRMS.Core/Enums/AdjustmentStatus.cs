namespace HRMS.Core.Enums;

/// <summary>
/// Status of timesheet adjustment request
/// </summary>
public enum AdjustmentStatus
{
    /// <summary>
    /// Adjustment is pending approval
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Adjustment has been approved
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Adjustment has been rejected
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Adjustment applied automatically (no approval needed)
    /// </summary>
    AutoApplied = 4
}
