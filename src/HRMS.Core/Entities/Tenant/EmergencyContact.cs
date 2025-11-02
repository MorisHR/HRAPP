namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Emergency contact information for employees
/// Supports both local (Mauritius) and home country contacts for expatriates
/// Stored in tenant schema
/// </summary>
public class EmergencyContact : BaseEntity
{
    /// <summary>
    /// Foreign key to Employee
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Full name of emergency contact
    /// </summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// Phone number with country code
    /// Example: +230 5xxx xxxx (Mauritius) or +91 98xxx xxxxx (India)
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Alternative phone number (optional)
    /// </summary>
    public string? AlternatePhoneNumber { get; set; }

    /// <summary>
    /// Email address (optional)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Relationship to employee
    /// Examples: Spouse, Parent, Sibling, Friend, etc.
    /// </summary>
    public string Relationship { get; set; } = string.Empty;

    /// <summary>
    /// Contact type: Local (in Mauritius) or HomeCountry
    /// </summary>
    public string ContactType { get; set; } = "Local"; // Local, HomeCountry

    /// <summary>
    /// Address of the emergency contact (optional)
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Country where this contact is located
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Is this the primary emergency contact?
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Navigation property
    /// </summary>
    public virtual Employee? Employee { get; set; }
}
