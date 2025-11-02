namespace HRMS.Core.Enums;

/// <summary>
/// Document expiry status for tracking and alerts
/// </summary>
public enum DocumentExpiryStatus
{
    /// <summary>
    /// Valid - More than 90 days until expiry
    /// </summary>
    Valid = 1,

    /// <summary>
    /// Expiring Soon - 30-90 days until expiry
    /// </summary>
    ExpiringSoon = 2,

    /// <summary>
    /// Expiring Very Soon - 15-30 days until expiry
    /// </summary>
    ExpiringVerySoon = 3,

    /// <summary>
    /// Critical - Less than 15 days until expiry
    /// </summary>
    Critical = 4,

    /// <summary>
    /// Expired - Document has expired
    /// </summary>
    Expired = 5,

    /// <summary>
    /// Not Applicable - Document not required for this employee type
    /// </summary>
    NotApplicable = 99
}
