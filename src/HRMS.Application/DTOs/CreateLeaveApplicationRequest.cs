using System.ComponentModel.DataAnnotations;
using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class CreateLeaveApplicationRequest
{
    [Required]
    public Guid LeaveTypeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;

    public string? ContactNumber { get; set; }
    public string? ContactAddress { get; set; }

    public LeaveCalculationType CalculationType { get; set; } = LeaveCalculationType.WorkingDays;

    // For file upload
    public string? AttachmentBase64 { get; set; }
    public string? AttachmentFileName { get; set; }
}
