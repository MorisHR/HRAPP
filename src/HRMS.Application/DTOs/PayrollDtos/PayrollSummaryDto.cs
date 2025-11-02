namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// DTO for payroll summary and reports
/// </summary>
public class PayrollSummaryDto
{
    public Guid PayrollCycleId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string PeriodDisplay { get; set; } = string.Empty;

    public int TotalEmployees { get; set; }
    public int ProcessedEmployees { get; set; }
    public int PaidEmployees { get; set; }
    public int PendingPayments { get; set; }

    // Financial Totals
    public decimal TotalBasicSalary { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal TotalOvertimePay { get; set; }
    public decimal TotalBonuses { get; set; }
    public decimal TotalGrossSalary { get; set; }

    // Statutory Deductions
    public decimal TotalNPFEmployee { get; set; }
    public decimal TotalNSFEmployee { get; set; }
    public decimal TotalCSGEmployee { get; set; }
    public decimal TotalPAYE { get; set; }
    public decimal TotalStatutoryDeductions { get; set; }

    // Employer Contributions
    public decimal TotalNPFEmployer { get; set; }
    public decimal TotalNSFEmployer { get; set; }
    public decimal TotalCSGEmployer { get; set; }
    public decimal TotalPRGF { get; set; }
    public decimal TotalTrainingLevy { get; set; }
    public decimal TotalEmployerContributions { get; set; }

    // Other Deductions
    public decimal TotalLoanDeductions { get; set; }
    public decimal TotalOtherDeductions { get; set; }
    public decimal TotalDeductions { get; set; }

    // Net
    public decimal TotalNetSalary { get; set; }

    // Cost Analysis
    public decimal TotalCostToCompany { get; set; }

    // Department Breakdown
    public List<DepartmentPayrollSummary> DepartmentBreakdown { get; set; } = new();
}

/// <summary>
/// Summary for a department
/// </summary>
public class DepartmentPayrollSummary
{
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal TotalGrossSalary { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetSalary { get; set; }
    public decimal TotalCostToCompany { get; set; }
}
