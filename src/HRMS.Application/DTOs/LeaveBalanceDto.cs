namespace HRMS.Application.DTOs;

public class LeaveBalanceDto
{
    public Guid Id { get; set; }
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public int Year { get; set; }

    public decimal TotalEntitlement { get; set; }
    public decimal UsedDays { get; set; }
    public decimal PendingDays { get; set; }
    public decimal AvailableDays { get; set; }
    public decimal CarriedForward { get; set; }

    public DateTime? ExpiryDate { get; set; }
}
