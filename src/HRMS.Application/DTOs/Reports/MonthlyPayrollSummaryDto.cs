namespace HRMS.Application.DTOs.Reports;

/// <summary>
/// Monthly payroll summary report
/// </summary>
public class MonthlyPayrollSummaryDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;

    public int TotalEmployees { get; set; }
    public decimal TotalGrossSalary { get; set; }
    public decimal TotalBasicSalary { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal TotalOvertimePay { get; set; }

    // Statutory Deductions - Employee
    public decimal TotalCSG_Employee { get; set; }
    public decimal TotalNSF_Employee { get; set; }
    public decimal TotalPAYE { get; set; }

    // Statutory Deductions - Employer
    public decimal TotalCSG_Employer { get; set; }
    public decimal TotalNSF_Employer { get; set; }
    public decimal TotalPRGF { get; set; }
    public decimal TotalTrainingLevy { get; set; }

    public decimal TotalOtherDeductions { get; set; }
    public decimal TotalNetSalary { get; set; }

    public List<DepartmentPayrollCostDto> DepartmentBreakdown { get; set; } = new();
}

public class DepartmentPayrollCostDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal TotalGrossSalary { get; set; }
    public decimal TotalNetSalary { get; set; }
}
