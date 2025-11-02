using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Represents additional salary components (allowances or deductions) for an employee
/// Can be recurring (monthly) or one-time payments
/// </summary>
public class SalaryComponent : BaseEntity
{
    /// <summary>
    /// Employee to whom this salary component applies
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Type/category of the salary component
    /// </summary>
    public SalaryComponentType ComponentType { get; set; }

    /// <summary>
    /// Name/description of the component (e.g., "Car Allowance", "Medical Insurance")
    /// </summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>
    /// Amount of the component in MUR
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (default: MUR)
    /// </summary>
    public string Currency { get; set; } = "MUR";

    /// <summary>
    /// Indicates if this is a recurring monthly component
    /// True = Applied every month
    /// False = One-time payment/deduction
    /// </summary>
    public bool IsRecurring { get; set; } = true;

    /// <summary>
    /// Indicates if this component is a deduction (true) or earning/allowance (false)
    /// </summary>
    public bool IsDeduction { get; set; } = false;

    /// <summary>
    /// Indicates if this component is taxable for PAYE calculation
    /// </summary>
    public bool IsTaxable { get; set; } = true;

    /// <summary>
    /// Indicates if this component should be included in statutory calculations (NPF, NSF, CSG base)
    /// </summary>
    public bool IncludeInStatutory { get; set; } = false;

    /// <summary>
    /// Date from which this component becomes effective
    /// </summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>
    /// Optional end date for the component
    /// Null = Ongoing (until manually deactivated)
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Indicates if this component is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Additional description or notes about this component
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Calculation method (Fixed, Percentage)
    /// Fixed = Use Amount as-is
    /// Percentage = Calculate based on basic salary (Amount = percentage value)
    /// </summary>
    public string CalculationMethod { get; set; } = "Fixed";

    /// <summary>
    /// Reference salary base for percentage calculations (BasicSalary, GrossSalary)
    /// Only used when CalculationMethod = "Percentage"
    /// </summary>
    public string? PercentageBase { get; set; }

    /// <summary>
    /// Priority/order for calculation (lower number = calculated first)
    /// Useful when components depend on each other
    /// </summary>
    public int CalculationOrder { get; set; } = 100;

    // Approval tracking
    /// <summary>
    /// Indicates if this component requires approval
    /// </summary>
    public bool RequiresApproval { get; set; } = false;

    /// <summary>
    /// Indicates if this component has been approved
    /// </summary>
    public bool IsApproved { get; set; } = true;

    /// <summary>
    /// ID of the user who approved this component
    /// </summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>
    /// Date when this component was approved
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Reference to the employee
    /// </summary>
    public virtual Employee Employee { get; set; } = null!;
}
