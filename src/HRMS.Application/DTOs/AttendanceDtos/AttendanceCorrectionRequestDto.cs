using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.AttendanceDtos;

/// <summary>
/// Request attendance correction
/// </summary>
public class AttendanceCorrectionRequestDto
{
    [Required]
    public Guid AttendanceId { get; set; }

    public DateTime? CorrectedCheckIn { get; set; }
    public DateTime? CorrectedCheckOut { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
