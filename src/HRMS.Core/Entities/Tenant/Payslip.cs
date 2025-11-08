using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Represents an individual employee's payslip for a specific payroll cycle
/// Contains all earnings, deductions, and statutory contributions
/// </summary>
public class Payslip : BaseEntity
{
    /// <summary>
    /// Reference to the payroll cycle this payslip belongs to
    /// </summary>
    public Guid PayrollCycleId { get; set; }

    /// <summary>
    /// Employee for whom this payslip is generated
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Month of the payslip (1-12)
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Year of the payslip
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Unique payslip number (e.g., PS-2025-11-001)
    /// </summary>
    public string PayslipNumber { get; set; } = string.Empty;

    // Earnings - Basic Salary
    /// <summary>
    /// Employee's basic monthly salary
    /// </summary>
    public decimal BasicSalary { get; set; }

    // Earnings - Allowances
    /// <summary>
    /// Housing allowance amount
    /// </summary>
    public decimal HousingAllowance { get; set; }

    /// <summary>
    /// Transport/conveyance allowance
    /// </summary>
    public decimal TransportAllowance { get; set; }

    /// <summary>
    /// Meal/food allowance
    /// </summary>
    public decimal MealAllowance { get; set; }

    /// <summary>
    /// Mobile/communication allowance
    /// </summary>
    public decimal MobileAllowance { get; set; }

    /// <summary>
    /// Other miscellaneous allowances
    /// </summary>
    public decimal OtherAllowances { get; set; }

    // Earnings - Overtime
    /// <summary>
    /// Total overtime hours worked in this month
    /// </summary>
    public decimal OvertimeHours { get; set; }

    /// <summary>
    /// Total overtime payment (calculated from attendance with sector-aware rates)
    /// </summary>
    public decimal OvertimePay { get; set; }

    // Earnings - Special Payments
    /// <summary>
    /// 13th month bonus (typically paid in December or pro-rated)
    /// </summary>
    public decimal ThirteenthMonthBonus { get; set; }

    /// <summary>
    /// Leave encashment amount (for unused annual leave)
    /// </summary>
    public decimal LeaveEncashment { get; set; }

    /// <summary>
    /// Gratuity payment (for employees leaving, hired before 2020)
    /// </summary>
    public decimal GratuityPayment { get; set; }

    /// <summary>
    /// Commission or performance bonus
    /// </summary>
    public decimal Commission { get; set; }

    /// <summary>
    /// Total gross salary (sum of all earnings)
    /// </summary>
    public decimal TotalGrossSalary { get; set; }

    // Attendance-based Information
    /// <summary>
    /// Number of working days in the month
    /// </summary>
    public int WorkingDays { get; set; }

    /// <summary>
    /// Number of days actually worked (excluding leaves, absences)
    /// </summary>
    public int ActualDaysWorked { get; set; }

    /// <summary>
    /// Number of days of paid leave taken
    /// </summary>
    public decimal PaidLeaveDays { get; set; }

    /// <summary>
    /// Number of days of unpaid leave taken
    /// </summary>
    public decimal UnpaidLeaveDays { get; set; }

    /// <summary>
    /// Deduction amount for unpaid leave
    /// </summary>
    public decimal LeaveDeductions { get; set; }

    // Statutory Deductions (Employee Portion)
    /// <summary>
    /// NPF employee contribution (3% of basic salary) - DEPRECATED, replaced by CSG
    /// Kept for legacy employees
    /// </summary>
    public decimal NPF_Employee { get; set; }

    /// <summary>
    /// NSF employee contribution (1% of basic salary)
    /// National Savings Fund
    /// </summary>
    public decimal NSF_Employee { get; set; }

    /// <summary>
    /// CSG employee contribution (1.5% for salary ≤ MUR 50,000, 3% for salary > MUR 50,000)
    /// Contribution Sociale Généralisée - Replaces NPF for new employees
    /// </summary>
    public decimal CSG_Employee { get; set; }

    /// <summary>
    /// PAYE tax deduction (progressive tax based on annual income)
    /// Pay As You Earn - Income tax
    /// Brackets: MUR 390,000 (0%), 390,001-550,000 (10%), 550,001-650,000 (12%), 650,001+ (20%)
    /// </summary>
    public decimal PAYE_Tax { get; set; }

    // Employer Contributions (Recorded but not deducted from employee)
    /// <summary>
    /// NPF employer contribution (6% of basic salary) - DEPRECATED
    /// </summary>
    public decimal NPF_Employer { get; set; }

    /// <summary>
    /// NSF employer contribution (2.5% of basic salary)
    /// </summary>
    public decimal NSF_Employer { get; set; }

    /// <summary>
    /// CSG employer contribution (3% for salary ≤ MUR 50,000, 6% for salary > MUR 50,000)
    /// </summary>
    public decimal CSG_Employer { get; set; }

    /// <summary>
    /// PRGF employer contribution (4.3%, 5%, or 6.8% based on years of service)
    /// Portable Retirement Gratuity Fund - Only for employees hired after January 1, 2020
    /// Replaces traditional gratuity system
    /// </summary>
    public decimal PRGF_Contribution { get; set; }

    /// <summary>
    /// Training Levy employer contribution (1.5% of basic salary)
    /// Mandatory contribution to HRDC (Human Resource Development Council)
    /// </summary>
    public decimal TrainingLevy { get; set; }

    // Other Deductions
    /// <summary>
    /// Loan repayment deduction
    /// </summary>
    public decimal LoanDeduction { get; set; }

    /// <summary>
    /// Advance salary deduction
    /// </summary>
    public decimal AdvanceDeduction { get; set; }

    /// <summary>
    /// Medical insurance deduction (if applicable)
    /// </summary>
    public decimal MedicalInsurance { get; set; }

    /// <summary>
    /// Other miscellaneous deductions
    /// </summary>
    public decimal OtherDeductions { get; set; }

    /// <summary>
    /// Total amount of all deductions
    /// </summary>
    public decimal TotalDeductions { get; set; }

    // Net Salary
    /// <summary>
    /// Final net salary to be paid to employee (Gross - Total Deductions)
    /// </summary>
    public decimal NetSalary { get; set; }

    // Payment Information
    /// <summary>
    /// Current payment status of this payslip
    /// </summary>
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Date and time when payment was made
    /// </summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Payment method (BankTransfer, Cash, Cheque)
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Bank transaction reference or cheque number
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Bank account number where payment was sent
    /// </summary>
    public string? BankAccountNumber { get; set; }

    // Additional Information
    /// <summary>
    /// Any remarks or notes for this payslip
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Indicates if this payslip has been sent/delivered to employee
    /// </summary>
    public bool IsDelivered { get; set; }

    /// <summary>
    /// Date when payslip was delivered/sent to employee
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    // Timesheet Integration
    /// <summary>
    /// JSON array of timesheet IDs used for calculating this payslip
    /// Format: ["guid1", "guid2", ...]
    /// </summary>
    public string? TimesheetIdsJson { get; set; }

    /// <summary>
    /// Number of approved timesheets processed for this payslip
    /// </summary>
    public int TimesheetsProcessed { get; set; }

    /// <summary>
    /// Indicates if this payslip was calculated from timesheets (true) or manual entry (false)
    /// </summary>
    public bool IsCalculatedFromTimesheets { get; set; }

    // Navigation properties
    /// <summary>
    /// Reference to the parent payroll cycle
    /// </summary>
    public virtual PayrollCycle PayrollCycle { get; set; } = null!;

    /// <summary>
    /// Reference to the employee
    /// </summary>
    public virtual Employee Employee { get; set; } = null!;
}
