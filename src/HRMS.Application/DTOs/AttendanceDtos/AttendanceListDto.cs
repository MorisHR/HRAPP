using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.AttendanceDtos;

/// <summary>
/// Attendance list view with employee information
/// </summary>
public class AttendanceListDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }

    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }

    public decimal WorkingHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal? OvertimeRate { get; set; }

    public AttendanceStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;

    public int? LateArrivalMinutes { get; set; }
    public int? EarlyDepartureMinutes { get; set; }

    public bool IsRegularized { get; set; }
    public bool IsSunday { get; set; }
    public bool IsPublicHoliday { get; set; }
}
