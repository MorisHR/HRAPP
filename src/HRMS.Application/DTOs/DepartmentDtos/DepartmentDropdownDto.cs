namespace HRMS.Application.DTOs.DepartmentDtos;

/// <summary>
/// Simplified DTO for department dropdowns
/// </summary>
public class DepartmentDropdownDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
