using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.AttendanceDtos;

/// <summary>
/// Manual attendance entry (check-in/check-out)
/// </summary>
public class CreateAttendanceDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }

    public Guid? ShiftId { get; set; }
    public Guid? AttendanceMachineId { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }
}
