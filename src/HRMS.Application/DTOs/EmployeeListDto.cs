using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

/// <summary>
/// Lightweight employee information for list views
/// PRODUCTION FIX: Added frontend-compatible fields for Angular UI
/// </summary>
public class EmployeeListDto
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;

    // Frontend compatibility: separate name fields
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty; // Backwards compatibility

    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty; // Frontend needs this

    // Frontend compatibility: dual naming for department
    public string Department { get; set; } = string.Empty; // Frontend expects "department"
    public string DepartmentName { get; set; } = string.Empty; // Backwards compatibility

    // Frontend compatibility: designation field
    public string Designation { get; set; } = string.Empty; // Frontend expects "designation"
    public string JobTitle { get; set; } = string.Empty; // Backwards compatibility

    // Frontend compatibility: status field (Active, OnLeave, Suspended, Terminated)
    public string Status { get; set; } = string.Empty; // Maps from EmploymentStatus

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
