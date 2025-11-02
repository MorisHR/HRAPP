namespace HRMS.Core.Enums;

/// <summary>
/// Types of salary components (earnings and deductions)
/// </summary>
public enum SalaryComponentType
{
    // === EARNINGS ===

    /// <summary>
    /// Basic Salary
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Housing Allowance
    /// </summary>
    HousingAllowance = 2,

    /// <summary>
    /// Transport Allowance
    /// </summary>
    TransportAllowance = 3,

    /// <summary>
    /// Meal Allowance
    /// </summary>
    MealAllowance = 4,

    /// <summary>
    /// Mobile/Phone Allowance
    /// </summary>
    MobileAllowance = 5,

    /// <summary>
    /// Overtime Pay (calculated from attendance)
    /// </summary>
    OvertimePay = 6,

    /// <summary>
    /// Performance Bonus
    /// </summary>
    Bonus = 7,

    /// <summary>
    /// Sales Commission
    /// </summary>
    Commission = 8,

    /// <summary>
    /// 13th Month Bonus
    /// </summary>
    ThirteenthMonth = 9,

    /// <summary>
    /// Leave Encashment
    /// </summary>
    LeaveEncashment = 10,

    /// <summary>
    /// Other Allowance
    /// </summary>
    OtherAllowance = 11,

    // === DEDUCTIONS ===

    /// <summary>
    /// Loan Repayment
    /// </summary>
    Loan = 50,

    /// <summary>
    /// Salary Advance Deduction
    /// </summary>
    Advance = 51,

    /// <summary>
    /// Unpaid Leave Deduction
    /// </summary>
    UnpaidLeave = 52,

    /// <summary>
    /// Other Deduction
    /// </summary>
    OtherDeduction = 53
}
