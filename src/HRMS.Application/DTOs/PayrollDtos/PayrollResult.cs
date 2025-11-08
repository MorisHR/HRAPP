namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// Detailed payroll calculation result from approved timesheets
/// Contains complete breakdown of hours, earnings, deductions, and net pay
/// </summary>
public class PayrollResult
{
    // Employee Information
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Basic Salary Information
    public decimal BasicSalary { get; set; }
    public decimal HourlyRate { get; set; }

    // Hours Breakdown (from approved timesheets)
    public decimal TotalRegularHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal TotalHolidayHours { get; set; }
    public decimal TotalLeaveHours { get; set; }
    public decimal TotalPayableHours { get; set; }

    // Working Days Information
    public int WorkingDays { get; set; }
    public int ActualDaysWorked { get; set; }
    public decimal PaidLeaveDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }

    // Gross Pay Calculation
    public decimal RegularPay { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal HolidayPay { get; set; }
    public decimal LeavePay { get; set; }

    // Allowances (from employee record)
    public decimal HousingAllowance { get; set; }
    public decimal TransportAllowance { get; set; }
    public decimal MealAllowance { get; set; }
    public decimal MobileAllowance { get; set; }
    public decimal OtherAllowances { get; set; }

    // Special Payments
    public decimal ThirteenthMonthBonus { get; set; }
    public decimal LeaveEncashment { get; set; }
    public decimal GratuityPayment { get; set; }
    public decimal Commission { get; set; }

    // Total Gross
    public decimal TotalGrossSalary { get; set; }

    // Mauritius Statutory Deductions
    public MauritiusDeductions StatutoryDeductions { get; set; } = new();

    // Other Deductions
    public decimal LeaveDeductions { get; set; }
    public decimal LoanDeduction { get; set; }
    public decimal AdvanceDeduction { get; set; }
    public decimal MedicalInsurance { get; set; }
    public decimal OtherDeductions { get; set; }

    // Total Deductions
    public decimal TotalDeductions { get; set; }

    // Net Salary
    public decimal NetSalary { get; set; }

    // Timesheet References
    public List<Guid> TimesheetIds { get; set; } = new();
    public int TimesheetsProcessed { get; set; }

    // Calculation Metadata
    public DateTime CalculatedAt { get; set; }
    public string? CalculationNotes { get; set; }
    public bool HasWarnings { get; set; }
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Mauritius statutory deductions breakdown
/// Includes employee and employer contributions
/// </summary>
public class MauritiusDeductions
{
    // Employee Contributions (deducted from salary)
    public decimal NPF_Employee { get; set; }
    public decimal NSF_Employee { get; set; }
    public decimal CSG_Employee { get; set; }
    public decimal PAYE_Tax { get; set; }
    public decimal TotalEmployeeContributions { get; set; }

    // Employer Contributions (not deducted, but recorded)
    public decimal NPF_Employer { get; set; }
    public decimal NSF_Employer { get; set; }
    public decimal CSG_Employer { get; set; }
    public decimal PRGF_Contribution { get; set; }
    public decimal TrainingLevy { get; set; }
    public decimal TotalEmployerContributions { get; set; }

    // Calculation Details
    public bool IsBelowCSG_Threshold { get; set; }
    public decimal CSG_EmployeeRate { get; set; }
    public decimal CSG_EmployerRate { get; set; }
    public decimal PRGF_Rate { get; set; }
    public string TaxBracket { get; set; } = string.Empty;
}
