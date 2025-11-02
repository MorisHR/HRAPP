using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

public class RejectLeaveRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string RejectionReason { get; set; } = string.Empty;
}
