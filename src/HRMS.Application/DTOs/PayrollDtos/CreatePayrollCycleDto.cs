using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// DTO for creating a new payroll cycle
/// </summary>
public class CreatePayrollCycleDto
{
    /// <summary>
    /// Month of the payroll cycle (1-12)
    /// </summary>
    [Required]
    [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
    public int Month { get; set; }

    /// <summary>
    /// Year of the payroll cycle
    /// </summary>
    [Required]
    [Range(2020, 2100, ErrorMessage = "Please enter a valid year")]
    public int Year { get; set; }

    /// <summary>
    /// Planned payment date
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Optional notes for this payroll cycle
    /// </summary>
    public string? Notes { get; set; }
}
