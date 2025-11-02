namespace HRMS.Application.DTOs.Reports;

/// <summary>
/// Headcount report
/// </summary>
public class HeadcountReportDto
{
    public DateTime ReportDate { get; set; }
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int InactiveEmployees { get; set; }

    public List<DepartmentHeadcountDto> DepartmentBreakdown { get; set; } = new();
    public List<DesignationHeadcountDto> DesignationBreakdown { get; set; } = new();
}

public class DepartmentHeadcountDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public int MaleCount { get; set; }
    public int FemaleCount { get; set; }
}

public class DesignationHeadcountDto
{
    public string DesignationName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
}

/// <summary>
/// Expatriate report with document expiry tracking
/// </summary>
public class ExpatriateReportDto
{
    public int TotalExpatriates { get; set; }
    public List<ExpatriateEmployeeDto> Expatriates { get; set; } = new();
}

public class ExpatriateEmployeeDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;

    public string? PassportNumber { get; set; }
    public DateTime? PassportExpiryDate { get; set; }
    public int? PassportDaysUntilExpiry { get; set; }

    public string? VisaNumber { get; set; }
    public DateTime? VisaExpiryDate { get; set; }
    public int? VisaDaysUntilExpiry { get; set; }

    public string? WorkPermitNumber { get; set; }
    public DateTime? WorkPermitExpiryDate { get; set; }
    public int? WorkPermitDaysUntilExpiry { get; set; }
}

/// <summary>
/// Turnover analysis report
/// </summary>
public class TurnoverReportDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;

    public int StartingHeadcount { get; set; }
    public int NewJoiners { get; set; }
    public int Exits { get; set; }
    public int EndingHeadcount { get; set; }

    public decimal TurnoverRate { get; set; }

    public List<NewJoinerDto> NewJoinersList { get; set; } = new();
    public List<ExitDto> ExitsList { get; set; } = new();
}

public class NewJoinerDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public DateTime JoiningDate { get; set; }
}

public class ExitDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public DateTime? ExitDate { get; set; }
    public string? ExitReason { get; set; }
}
