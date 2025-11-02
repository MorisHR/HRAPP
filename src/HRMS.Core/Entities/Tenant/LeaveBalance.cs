using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Track leave balance per employee, per leave type, per year
/// </summary>
public class LeaveBalance : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }

    public decimal TotalEntitlement { get; set; }     // Total days entitled for the year
    public decimal UsedDays { get; set; } = 0;        // Days already taken
    public decimal PendingDays { get; set; } = 0;     // Days in pending applications
    public decimal AvailableDays => TotalEntitlement - UsedDays - PendingDays;

    public decimal CarriedForward { get; set; } = 0;  // Days brought from previous year
    public decimal Accrued { get; set; } = 0;         // Days accrued so far (for monthly accrual)

    public DateTime? LastAccrualDate { get; set; }
    public DateTime? ExpiryDate { get; set; }         // When carried forward days expire

    // Navigation
    public virtual Employee? Employee { get; set; }
    public virtual LeaveType? LeaveType { get; set; }
}
