namespace HRMS.Application.DTOs.DepartmentDtos;

/// <summary>
/// Extended department DTO with full details and audit information
/// </summary>
public class DepartmentDetailDto : DepartmentDto
{
    /// <summary>
    /// List of sub-departments
    /// </summary>
    public List<DepartmentDto> SubDepartments { get; set; } = new();

    /// <summary>
    /// Full department hierarchy path (e.g., "Corporation > Region > Department")
    /// </summary>
    public string HierarchyPath { get; set; } = string.Empty;

    /// <summary>
    /// Department level in hierarchy (0 = root)
    /// </summary>
    public int HierarchyLevel { get; set; }

    /// <summary>
    /// Created by user full name
    /// </summary>
    public string CreatedByName { get; set; } = string.Empty;

    /// <summary>
    /// Updated by user full name
    /// </summary>
    public string? UpdatedByName { get; set; }

    /// <summary>
    /// When department was last deactivated
    /// </summary>
    public DateTime? DeactivatedAt { get; set; }

    /// <summary>
    /// Who deactivated the department
    /// </summary>
    public string? DeactivatedBy { get; set; }

    /// <summary>
    /// Number of active employees
    /// </summary>
    public int ActiveEmployeeCount { get; set; }

    /// <summary>
    /// Number of inactive employees
    /// </summary>
    public int InactiveEmployeeCount { get; set; }
}
