namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Department entity - stored in Tenant schema
/// </summary>
public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public Department? ParentDepartment { get; set; }
    public Guid? DepartmentHeadId { get; set; }
    public Employee? DepartmentHead { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
}
