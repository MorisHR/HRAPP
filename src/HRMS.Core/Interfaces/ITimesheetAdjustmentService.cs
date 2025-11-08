using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;

namespace HRMS.Core.Interfaces;

/// <summary>
/// Service for handling timesheet adjustments and corrections
/// </summary>
public interface ITimesheetAdjustmentService
{
    /// <summary>
    /// Create adjustment request for timesheet entry
    /// </summary>
    Task<TimesheetAdjustment> CreateAdjustmentAsync(
        Guid timesheetEntryId,
        AdjustmentType adjustmentType,
        string fieldName,
        string oldValue,
        string newValue,
        string reason,
        Guid adjustedBy,
        string adjustedByName);

    /// <summary>
    /// Approve adjustment and apply changes
    /// </summary>
    Task<TimesheetAdjustment> ApproveAdjustmentAsync(
        Guid adjustmentId,
        Guid approvedBy,
        string approvedByName);

    /// <summary>
    /// Reject adjustment request
    /// </summary>
    Task<TimesheetAdjustment> RejectAdjustmentAsync(
        Guid adjustmentId,
        Guid rejectedBy,
        string rejectionReason);

    /// <summary>
    /// Get all pending adjustments for a timesheet
    /// </summary>
    Task<List<TimesheetAdjustment>> GetPendingAdjustmentsForTimesheetAsync(Guid timesheetId);

    /// <summary>
    /// Apply approved adjustment to timesheet entry and recalculate hours
    /// </summary>
    Task ApplyAdjustmentAsync(TimesheetAdjustment adjustment);
}
