using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Represents a payroll cycle for a specific month and year
/// Manages the entire payroll processing workflow from draft to payment
/// </summary>
public class PayrollCycle : BaseEntity
{
    /// <summary>
    /// Month of the payroll cycle (1-12)
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Year of the payroll cycle
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Current status of the payroll cycle
    /// </summary>
    public PayrollCycleStatus Status { get; set; } = PayrollCycleStatus.Draft;

    // Financial Summary
    /// <summary>
    /// Total gross salary for all employees in this cycle
    /// </summary>
    public decimal TotalGrossSalary { get; set; }

    /// <summary>
    /// Total deductions for all employees in this cycle
    /// </summary>
    public decimal TotalDeductions { get; set; }

    /// <summary>
    /// Total net salary to be paid to all employees
    /// </summary>
    public decimal TotalNetSalary { get; set; }

    /// <summary>
    /// Total NPF employee contribution (3% of basic salary)
    /// </summary>
    public decimal TotalNPFEmployee { get; set; }

    /// <summary>
    /// Total NPF employer contribution (6% of basic salary)
    /// </summary>
    public decimal TotalNPFEmployer { get; set; }

    /// <summary>
    /// Total NSF employee contribution (1% of basic salary)
    /// </summary>
    public decimal TotalNSFEmployee { get; set; }

    /// <summary>
    /// Total NSF employer contribution (2.5% of basic salary)
    /// </summary>
    public decimal TotalNSFEmployer { get; set; }

    /// <summary>
    /// Total CSG employee contribution (1.5% or 3% based on salary threshold)
    /// </summary>
    public decimal TotalCSGEmployee { get; set; }

    /// <summary>
    /// Total CSG employer contribution (3% or 6% based on salary threshold)
    /// </summary>
    public decimal TotalCSGEmployer { get; set; }

    /// <summary>
    /// Total PRGF employer contribution (4.3%, 5%, or 6.8% based on years of service)
    /// Only for employees hired after January 1, 2020
    /// </summary>
    public decimal TotalPRGF { get; set; }

    /// <summary>
    /// Total Training Levy (1.5% of basic salary - employer contribution)
    /// </summary>
    public decimal TotalTrainingLevy { get; set; }

    /// <summary>
    /// Total PAYE tax deductions
    /// </summary>
    public decimal TotalPAYE { get; set; }

    /// <summary>
    /// Total overtime pay for all employees
    /// </summary>
    public decimal TotalOvertimePay { get; set; }

    // Process tracking
    /// <summary>
    /// ID of the user who processed this payroll cycle
    /// </summary>
    public Guid? ProcessedBy { get; set; }

    /// <summary>
    /// Date and time when the payroll was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// ID of the user who approved this payroll cycle
    /// </summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>
    /// Date and time when the payroll was approved
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Planned or actual payment date
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Additional notes or comments about this payroll cycle
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Number of employees included in this payroll cycle
    /// </summary>
    public int EmployeeCount { get; set; }

    // Navigation properties
    /// <summary>
    /// Collection of individual payslips for this cycle
    /// </summary>
    public virtual ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();
}
