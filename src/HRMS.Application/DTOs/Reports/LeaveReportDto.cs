namespace HRMS.Application.DTOs.Reports;

/// <summary>
/// Leave balance report for all employees
/// </summary>
public class LeaveBalanceReportDto
{
    public int Year { get; set; }
    public List<EmployeeLeaveBalanceDto> EmployeeBalances { get; set; } = new();
}

public class EmployeeLeaveBalanceDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    public List<LeaveTypeBalanceDto> LeaveBalances { get; set; } = new();
}

public class LeaveTypeBalanceDto
{
    public string LeaveType { get; set; } = string.Empty;
    public decimal TotalEntitlement { get; set; }
    public decimal UsedDays { get; set; }
    public decimal PendingDays { get; set; }
    public decimal AvailableDays { get; set; }
    public decimal CarriedForward { get; set; }
}

/// <summary>
/// Leave utilization report
/// </summary>
public class LeaveUtilizationReportDto
{
    public int Year { get; set; }
    public List<LeaveTypeUtilizationDto> LeaveTypeUtilization { get; set; } = new();
}

public class LeaveTypeUtilizationDto
{
    public string LeaveType { get; set; } = string.Empty;
    public int TotalApplications { get; set; }
    public int ApprovedApplications { get; set; }
    public int RejectedApplications { get; set; }
    public int PendingApplications { get; set; }
    public decimal TotalDaysApplied { get; set; }
    public decimal TotalDaysApproved { get; set; }
}
