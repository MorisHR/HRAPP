using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// DTO for approving or rejecting a payroll cycle
/// </summary>
public class ApprovePayrollDto
{
    /// <summary>
    /// True to approve, False to reject
    /// </summary>
    [Required]
    public bool IsApproved { get; set; }

    /// <summary>
    /// Planned payment date (required if approving)
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Approval/rejection notes
    /// </summary>
    public string? Notes { get; set; }
}
