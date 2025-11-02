using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// DTO for employee viewing their own payslips
/// </summary>
public class EmployeePayslipDto
{
    public Guid Id { get; set; }
    public string PayslipNumber { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public string PeriodDisplay { get; set; } = string.Empty;

    public decimal TotalGrossSalary { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetSalary { get; set; }

    public PaymentStatus PaymentStatus { get; set; }
    public string PaymentStatusDisplay { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }

    public bool IsDelivered { get; set; }
    public DateTime CreatedAt { get; set; }
}
