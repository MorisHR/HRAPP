namespace HRMS.Application.DTOs.AttendanceDtos;

/// <summary>
/// Monthly attendance summary for an employee
/// </summary>
public class MonthlyAttendanceSummaryDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;

    public int Year { get; set; }
    public int Month { get; set; }

    public int TotalWorkingDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int HalfDays { get; set; }
    public int LeaveDays { get; set; }
    public int WeekendDays { get; set; }
    public int PublicHolidayDays { get; set; }

    public decimal TotalWorkingHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }

    public int TotalLateMinutes { get; set; }
    public int TotalEarlyDepartureMinutes { get; set; }

    public decimal AttendancePercentage { get; set; }

    public List<AttendanceListDto> DailyRecords { get; set; } = new();
}
