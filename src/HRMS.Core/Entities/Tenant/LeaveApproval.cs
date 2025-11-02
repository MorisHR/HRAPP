using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Multi-level approval tracking
/// </summary>
public class LeaveApproval : BaseEntity
{
    public Guid LeaveApplicationId { get; set; }
    public int ApprovalLevel { get; set; }            // 1 = Manager, 2 = HR, etc.
    public string ApproverRole { get; set; } = string.Empty;  // "DepartmentManager", "HRManager"
    public Guid? ApproverId { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTime? ActionDate { get; set; }
    public string? Comments { get; set; }
    public string? RequestedInfo { get; set; }        // If more info requested

    public bool IsCurrentLevel { get; set; } = true;
    public bool IsComplete { get; set; } = false;

    // Navigation
    public virtual LeaveApplication? LeaveApplication { get; set; }
    public virtual Employee? Approver { get; set; }
}
