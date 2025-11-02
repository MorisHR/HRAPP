using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.AttendanceDtos;

/// <summary>
/// Approve or reject attendance correction
/// </summary>
public class ApproveAttendanceCorrectionDto
{
    [Required]
    public bool IsApproved { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }
}
