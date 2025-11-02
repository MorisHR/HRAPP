using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

public class CancelLeaveRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string CancellationReason { get; set; } = string.Empty;
}
