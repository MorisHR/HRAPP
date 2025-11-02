using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

/// <summary>
/// Document expiry information for dashboard widgets and alerts
/// </summary>
public class DocumentExpiryInfoDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? CountryOfOrigin { get; set; }

    // Passport Info
    public string? PassportNumber { get; set; }
    public DateTime? PassportExpiryDate { get; set; }
    public int? DaysUntilPassportExpiry { get; set; }
    public DocumentExpiryStatus PassportExpiryStatus { get; set; }

    // Visa Info
    public VisaType? VisaType { get; set; }
    public string? VisaNumber { get; set; }
    public DateTime? VisaExpiryDate { get; set; }
    public int? DaysUntilVisaExpiry { get; set; }
    public DocumentExpiryStatus VisaExpiryStatus { get; set; }

    // Work Permit Info
    public string? WorkPermitNumber { get; set; }
    public DateTime? WorkPermitExpiryDate { get; set; }
    public int? DaysUntilWorkPermitExpiry { get; set; }

    // Overall status
    public bool RequiresUrgentAction { get; set; }
    public string? RecommendedAction { get; set; }
}
