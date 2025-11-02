using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// Detailed DTO for individual payslip with all breakdown
/// </summary>
public class PayslipDetailsDto
{
    public Guid Id { get; set; }
    public Guid PayrollCycleId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string PayslipNumber { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;

    public int Month { get; set; }
    public int Year { get; set; }
    public string PeriodDisplay { get; set; } = string.Empty;

    // Earnings
    public decimal BasicSalary { get; set; }
    public decimal HousingAllowance { get; set; }
    public decimal TransportAllowance { get; set; }
    public decimal MealAllowance { get; set; }
    public decimal MobileAllowance { get; set; }
    public decimal OtherAllowances { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal ThirteenthMonthBonus { get; set; }
    public decimal LeaveEncashment { get; set; }
    public decimal GratuityPayment { get; set; }
    public decimal Commission { get; set; }
    public decimal TotalGrossSalary { get; set; }

    // Attendance
    public int WorkingDays { get; set; }
    public int ActualDaysWorked { get; set; }
    public decimal PaidLeaveDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
    public decimal LeaveDeductions { get; set; }

    // Statutory Deductions (Employee)
    public decimal NPF_Employee { get; set; }
    public decimal NSF_Employee { get; set; }
    public decimal CSG_Employee { get; set; }
    public decimal PAYE_Tax { get; set; }

    // Employer Contributions
    public decimal NPF_Employer { get; set; }
    public decimal NSF_Employer { get; set; }
    public decimal CSG_Employer { get; set; }
    public decimal PRGF_Contribution { get; set; }
    public decimal TrainingLevy { get; set; }

    // Other Deductions
    public decimal LoanDeduction { get; set; }
    public decimal AdvanceDeduction { get; set; }
    public decimal MedicalInsurance { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal TotalDeductions { get; set; }

    // Net Salary
    public decimal NetSalary { get; set; }

    // Payment
    public PaymentStatus PaymentStatus { get; set; }
    public string PaymentStatusDisplay { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public string? BankAccountNumber { get; set; }

    public string? Remarks { get; set; }
    public bool IsDelivered { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
