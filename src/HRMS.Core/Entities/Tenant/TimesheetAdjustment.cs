using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Tracks adjustments/corrections made to timesheet entries
/// Maintains audit trail of all changes
/// Requires approval for locked timesheets
/// </summary>
public class TimesheetAdjustment : BaseEntity
{
    public Guid TimesheetEntryId { get; set; }

    // Adjustment details
    public AdjustmentType AdjustmentType { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Reason { get; set; } = string.Empty;

    // Who made the adjustment
    public Guid AdjustedBy { get; set; }
    public string? AdjustedByName { get; set; }
    public DateTime AdjustedAt { get; set; }

    // Approval tracking (for locked timesheets)
    public AdjustmentStatus Status { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation properties
    public virtual TimesheetEntry? TimesheetEntry { get; set; }
}
