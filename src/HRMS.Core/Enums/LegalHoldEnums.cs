namespace HRMS.Core.Enums;

/// <summary>
/// Legal hold status
/// FORTUNE 500 PATTERN: E-Discovery platforms, litigation hold systems
/// </summary>
public enum LegalHoldStatus
{
    /// <summary>Legal hold is active and enforced</summary>
    ACTIVE,

    /// <summary>Legal hold has been released</summary>
    RELEASED,

    /// <summary>Legal hold has expired</summary>
    EXPIRED,

    /// <summary>Legal hold is pending activation</summary>
    PENDING,

    /// <summary>Legal hold has been cancelled</summary>
    CANCELLED
}

/// <summary>
/// E-Discovery export format
/// </summary>
public enum EDiscoveryFormat
{
    /// <summary>EMLX email format for legal review</summary>
    EMLX,

    /// <summary>PDF format with chain of custody</summary>
    PDF,

    /// <summary>JSON format for programmatic access</summary>
    JSON,

    /// <summary>CSV format for spreadsheet import</summary>
    CSV,

    /// <summary>Native format (original data format)</summary>
    NATIVE
}
