using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.DepartmentDtos;

/// <summary>
/// DTO for bulk status update operations
/// </summary>
public class BulkDepartmentStatusDto
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one department ID is required")]
    public List<Guid> DepartmentIds { get; set; } = new();

    [Required]
    public bool IsActive { get; set; }

    /// <summary>
    /// Optional reason for status change (for audit trail)
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}
