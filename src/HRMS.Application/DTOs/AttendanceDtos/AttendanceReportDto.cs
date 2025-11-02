namespace HRMS.Application.DTOs.AttendanceDtos;

/// <summary>
/// Attendance report with filters
/// </summary>
public class AttendanceReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    public int TotalEmployees { get; set; }
    public decimal AverageAttendancePercentage { get; set; }

    public int TotalPresentDays { get; set; }
    public int TotalAbsentDays { get; set; }
    public int TotalLateDays { get; set; }

    public decimal TotalWorkingHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }

    public List<EmployeeAttendanceSummary> EmployeeSummaries { get; set; } = new();
}

public class EmployeeAttendanceSummary
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;

    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }

    public decimal TotalWorkingHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal AttendancePercentage { get; set; }
}
