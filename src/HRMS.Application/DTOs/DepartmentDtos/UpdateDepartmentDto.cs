using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.DepartmentDtos;

/// <summary>
/// DTO for updating an existing department
/// </summary>
public class UpdateDepartmentDto
{
    [Required(ErrorMessage = "Department name is required")]
    [StringLength(100, ErrorMessage = "Department name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department code is required")]
    [StringLength(20, ErrorMessage = "Department code cannot exceed 20 characters")]
    public string Code { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public Guid? ParentDepartmentId { get; set; }

    public Guid? DepartmentHeadId { get; set; }

    [StringLength(50, ErrorMessage = "Cost center code cannot exceed 50 characters")]
    public string? CostCenterCode { get; set; }

    public bool IsActive { get; set; }
}
