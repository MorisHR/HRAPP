using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

/// <summary>
/// Lightweight employee information for list views
/// </summary>
public class EmployeeListDto
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public EmployeeType EmployeeType { get; set; }
    public bool IsExpatriate { get; set; }
    public string? CountryOfOrigin { get; set; }
    public DateTime JoiningDate { get; set; }
    public int YearsOfService { get; set; }
    public bool IsActive { get; set; }

    // Document expiry warnings for list view
    public bool HasExpiredDocuments { get; set; }
    public bool HasDocumentsExpiringSoon { get; set; }
    public DocumentExpiryStatus? PassportExpiryStatus { get; set; }
    public DocumentExpiryStatus? VisaExpiryStatus { get; set; }
}
