using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// DTO representing a payroll cycle
/// </summary>
public class PayrollCycleDto
{
    public Guid Id { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public PayrollCycleStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;

    // Financial Summary
    public decimal TotalGrossSalary { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetSalary { get; set; }
    public decimal TotalNPFEmployee { get; set; }
    public decimal TotalNPFEmployer { get; set; }
    public decimal TotalNSFEmployee { get; set; }
    public decimal TotalNSFEmployer { get; set; }
    public decimal TotalCSGEmployee { get; set; }
    public decimal TotalCSGEmployer { get; set; }
    public decimal TotalPRGF { get; set; }
    public decimal TotalTrainingLevy { get; set; }
    public decimal TotalPAYE { get; set; }
    public decimal TotalOvertimePay { get; set; }

    // Process Information
    public int EmployeeCount { get; set; }
    /// <summary>
    /// Username or email of the user who processed this payroll cycle
    /// FIXED: Changed from Guid to string (CRITICAL-1)
    /// </summary>
    public string? ProcessedBy { get; set; }
    public string? ProcessedByName { get; set; }
    public DateTime? ProcessedAt { get; set; }
    /// <summary>
    /// Username or email of the user who approved this payroll cycle
    /// FIXED: Changed from Guid to string (CRITICAL-1)
    /// </summary>
    public string? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
