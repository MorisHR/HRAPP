using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.AttendanceDtos;

/// <summary>
/// Full attendance details
/// </summary>
public class AttendanceDetailsDto
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

    public string? Remarks { get; set; }

    public bool IsRegularized { get; set; }
    public Guid? RegularizedBy { get; set; }
    public string? RegularizedByName { get; set; }
    public DateTime? RegularizedAt { get; set; }

    public bool IsSunday { get; set; }
    public bool IsPublicHoliday { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
