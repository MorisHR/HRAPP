namespace HRMS.Core.Enums;

/// <summary>
/// Status of a leave application
/// </summary>
public enum LeaveStatus
{
    Draft = 0,                // Saved but not submitted
    PendingApproval = 1,      // Submitted, waiting for manager approval
    ManagerApproved = 2,      // Approved by department manager (if multi-level)
    Approved = 3,             // Fully approved
    Rejected = 4,             // Rejected by approver
    Cancelled = 5,            // Cancelled by employee
    Withdrawn = 6             // Withdrawn before approval
}
