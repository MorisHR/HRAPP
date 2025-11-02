using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// DTO for updating an existing salary component
/// </summary>
public class UpdateSalaryComponentDto
{
    /// <summary>
    /// Updated amount
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Amount must be positive")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Updated effective end date
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Updated active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Updated description
    /// </summary>
    public string? Description { get; set; }
}
