using System.ComponentModel.DataAnnotations;
using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// DTO for creating a new salary component
/// </summary>
public class CreateSalaryComponentDto
{
    /// <summary>
    /// Employee ID for whom this component is created
    /// </summary>
    [Required]
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Type of salary component
    /// </summary>
    [Required]
    public SalaryComponentType ComponentType { get; set; }

    /// <summary>
    /// Name of the component
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>
    /// Amount in MUR (or percentage value if CalculationMethod is Percentage)
    /// </summary>
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Amount must be positive")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Is this a recurring monthly component?
    /// </summary>
    public bool IsRecurring { get; set; } = true;

    /// <summary>
    /// Is this a deduction (true) or allowance/earning (false)?
    /// </summary>
    public bool IsDeduction { get; set; } = false;

    /// <summary>
    /// Is this component taxable?
    /// </summary>
    public bool IsTaxable { get; set; } = true;

    /// <summary>
    /// Include in statutory calculations?
    /// </summary>
    public bool IncludeInStatutory { get; set; } = false;

    /// <summary>
    /// Effective from date
    /// </summary>
    [Required]
    public DateTime EffectiveFrom { get; set; }

    /// <summary>
    /// Optional effective end date
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Calculation method (Fixed, Percentage)
    /// </summary>
    public string CalculationMethod { get; set; } = "Fixed";

    /// <summary>
    /// Base for percentage calculation (BasicSalary, GrossSalary)
    /// </summary>
    public string? PercentageBase { get; set; }

    /// <summary>
    /// Additional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Requires approval?
    /// </summary>
    public bool RequiresApproval { get; set; } = false;
}
