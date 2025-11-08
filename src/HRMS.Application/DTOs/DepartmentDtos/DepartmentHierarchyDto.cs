namespace HRMS.Application.DTOs.DepartmentDtos;

/// <summary>
/// DTO for department hierarchy tree structure
/// </summary>
public class DepartmentHierarchyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? ParentDepartmentId { get; set; }
    public string? DepartmentHeadName { get; set; }
    public int EmployeeCount { get; set; }
    public bool IsActive { get; set; }
    public List<DepartmentHierarchyDto> Children { get; set; } = new();
}
