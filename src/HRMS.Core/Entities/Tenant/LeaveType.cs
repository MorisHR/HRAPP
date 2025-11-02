using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Master data for leave types (Annual, Sick, etc.)
/// </summary>
public class LeaveType : BaseEntity
{
    public LeaveTypeEnum TypeCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal DefaultEntitlement { get; set; }     // Days per year (e.g., 22 for Annual Leave)
    public bool RequiresApproval { get; set; } = true;
    public bool IsPaid { get; set; } = true;

    public bool CanCarryForward { get; set; } = false;
    public int MaxCarryForwardDays { get; set; } = 0;

    public bool RequiresDocumentation { get; set; } = false;  // E.g., medical certificate
    public int MinDaysNotice { get; set; } = 0;
    public int MaxConsecutiveDays { get; set; } = 365;

    public bool IsActive { get; set; } = true;
    public string? ApprovalWorkflow { get; set; }       // JSON config for multi-level approval

    // Navigation
    public virtual ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public virtual ICollection<LeaveApplication> LeaveApplications { get; set; } = new List<LeaveApplication>();
}
