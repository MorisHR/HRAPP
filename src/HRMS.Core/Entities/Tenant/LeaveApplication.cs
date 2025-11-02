using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Leave application/request
/// </summary>
public class LeaveApplication : BaseEntity
{
    public string ApplicationNumber { get; set; } = string.Empty;  // LEV-2025-0001
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }            // Calculated working days
    public LeaveCalculationType CalculationType { get; set; } = LeaveCalculationType.WorkingDays;

    public string Reason { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? ContactAddress { get; set; }

    public LeaveStatus Status { get; set; } = LeaveStatus.PendingApproval;
    public DateTime? AppliedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApproverComments { get; set; }

    public DateTime? RejectedDate { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime? CancelledDate { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }

    public string? AttachmentPath { get; set; }       // Path to medical certificate, etc.
    public bool RequiresHRApproval { get; set; } = false;

    // Navigation
    public virtual Employee? Employee { get; set; }
    public virtual LeaveType? LeaveType { get; set; }
    public virtual Employee? Approver { get; set; }
    public virtual Employee? Rejector { get; set; }
    public virtual ICollection<LeaveApproval> Approvals { get; set; } = new List<LeaveApproval>();
}
