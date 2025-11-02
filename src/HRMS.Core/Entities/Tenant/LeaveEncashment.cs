using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Leave encashment calculation for final settlement
/// </summary>
public class LeaveEncashment : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public DateTime CalculationDate { get; set; }
    public DateTime LastWorkingDay { get; set; }

    public decimal UnusedAnnualLeaveDays { get; set; }
    public decimal UnusedSickLeaveDays { get; set; }
    public decimal TotalEncashableDays { get; set; }

    public decimal DailySalary { get; set; }
    public decimal TotalEncashmentAmount { get; set; }

    public string? CalculationDetails { get; set; }   // JSON with breakdown
    public bool IsPaid { get; set; } = false;
    public DateTime? PaidDate { get; set; }
    public string? PaymentReference { get; set; }

    // Navigation
    public virtual Employee? Employee { get; set; }
}
