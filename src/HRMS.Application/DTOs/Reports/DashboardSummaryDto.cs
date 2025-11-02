namespace HRMS.Application.DTOs.Reports;

/// <summary>
/// Dashboard summary with key performance indicators
/// </summary>
public class DashboardSummaryDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int EmployeesOnLeave { get; set; }
    public int EmployeesOnProbation { get; set; }

    public decimal TodayAttendancePercentage { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public int LateToday { get; set; }

    public int PendingLeaveApprovals { get; set; }
    public int DocumentsExpiringThisMonth { get; set; }

    public decimal TotalOvertimeHoursThisMonth { get; set; }
    public decimal TotalPayrollCostThisMonth { get; set; }

    public int NewJoinersThisMonth { get; set; }
    public int ExitsThisMonth { get; set; }
}
