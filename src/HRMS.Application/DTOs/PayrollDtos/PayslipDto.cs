using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// DTO for payslip summary in lists
/// </summary>
public class PayslipDto
{
    public Guid Id { get; set; }
    public Guid PayrollCycleId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string PayslipNumber { get; set; } = string.Empty;

    public int Month { get; set; }
    public int Year { get; set; }
    public string PeriodDisplay { get; set; } = string.Empty;

    public decimal BasicSalary { get; set; }
    public decimal TotalGrossSalary { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetSalary { get; set; }

    public PaymentStatus PaymentStatus { get; set; }
    public string PaymentStatusDisplay { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public bool IsDelivered { get; set; }
}
