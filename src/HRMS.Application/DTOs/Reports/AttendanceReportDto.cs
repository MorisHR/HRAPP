namespace HRMS.Application.DTOs.Reports;

/// <summary>
/// Monthly attendance report
/// </summary>
public class MonthlyAttendanceReportDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;

    public int TotalWorkingDays { get; set; }
    public int TotalEmployees { get; set; }

    public decimal AverageAttendancePercentage { get; set; }
    public int TotalPresent { get; set; }
    public int TotalAbsent { get; set; }
    public int TotalLateArrivals { get; set; }
    public int TotalEarlyDepartures { get; set; }

    public List<EmployeeAttendanceSummaryDto> EmployeeAttendance { get; set; } = new();
}

public class EmployeeAttendanceSummaryDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int LeaveDays { get; set; }

    public decimal WorkingHours { get; set; }
    public decimal OvertimeHours { get; set; }

    public decimal AttendancePercentage { get; set; }
}

/// <summary>
/// Overtime report
/// </summary>
public class OvertimeReportDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;

    public decimal TotalOvertimeHours { get; set; }
    public decimal TotalOvertimeCost { get; set; }

    public List<EmployeeOvertimeDto> EmployeeOvertimes { get; set; } = new();
}

public class EmployeeOvertimeDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    public decimal RegularOvertimeHours { get; set; }
    public decimal SundayOvertimeHours { get; set; }
    public decimal PublicHolidayOvertimeHours { get; set; }

    public decimal TotalOvertimeHours { get; set; }
    public decimal TotalOvertimePay { get; set; }
}
