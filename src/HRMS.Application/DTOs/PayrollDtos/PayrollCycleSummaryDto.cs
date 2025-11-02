using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// Lightweight DTO for listing payroll cycles
/// </summary>
public class PayrollCycleSummaryDto
{
    public Guid Id { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public string PeriodDisplay { get; set; } = string.Empty; // "November 2025"
    public PayrollCycleStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;

    public int EmployeeCount { get; set; }
    public decimal TotalNetSalary { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
