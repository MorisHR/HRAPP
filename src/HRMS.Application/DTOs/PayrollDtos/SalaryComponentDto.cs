using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// DTO representing a salary component
/// </summary>
public class SalaryComponentDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;

    public SalaryComponentType ComponentType { get; set; }
    public string ComponentTypeDisplay { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MUR";

    public bool IsRecurring { get; set; }
    public bool IsDeduction { get; set; }
    public bool IsTaxable { get; set; }
    public bool IncludeInStatutory { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }

    public string? Description { get; set; }
    public string CalculationMethod { get; set; } = "Fixed";
    public string? PercentageBase { get; set; }

    public bool RequiresApproval { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
