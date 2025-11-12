using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

/// <summary>
/// Public employee information - visible to all authenticated users
/// SECURITY: Excludes sensitive data (salary, bank details, tax info, personal contact)
/// Used for: Employee directory, org chart, search results
/// </summary>
public class EmployeePublicDto
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    // Job Information
    public string JobTitle { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }

    // Employment Status
    public EmployeeType EmployeeType { get; set; }
    public bool IsExpatriate { get; set; }
    public DateTime JoiningDate { get; set; }
    public int YearsOfService { get; set; }
    public bool IsActive { get; set; }
}
