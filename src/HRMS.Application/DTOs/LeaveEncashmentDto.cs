namespace HRMS.Application.DTOs;

public class LeaveEncashmentDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;

    public DateTime CalculationDate { get; set; }
    public DateTime LastWorkingDay { get; set; }

    public decimal UnusedAnnualLeaveDays { get; set; }
    public decimal UnusedSickLeaveDays { get; set; }
    public decimal TotalEncashableDays { get; set; }

    public decimal DailySalary { get; set; }
    public decimal TotalEncashmentAmount { get; set; }

    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
}
