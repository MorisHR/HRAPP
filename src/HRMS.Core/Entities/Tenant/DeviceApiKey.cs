using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Fortune 500-grade Device API Key entity
/// Provides secure authentication for biometric devices and external systems
///
/// SECURITY FEATURES:
/// - SHA-256 hashed API keys (never store plaintext)
/// - IP address whitelisting
/// - Rate limiting per device
/// - Automatic expiration
/// - Usage tracking and auditing
///
/// COMPLIANCE:
/// - SOC 2 Type II: Secure credential management
/// - ISO 27001: Access control mechanisms
/// - PCI DSS: Key lifecycle management
/// </summary>
public class DeviceApiKey : BaseEntity
{
    // ==========================================
    // IDENTITY & RELATIONSHIPS
    // ==========================================

    /// <summary>
    /// Tenant that owns this API key
    /// Multi-tenant isolation: Each tenant has separate API keys
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Device associated with this API key
    /// One device can have multiple API keys (for rotation, different purposes)
    /// </summary>
    public Guid DeviceId { get; set; }

    // ==========================================
    // AUTHENTICATION
    // ==========================================

    /// <summary>
    /// SHA-256 hashed API key
    /// CRITICAL: Never store plaintext keys in database
    /// Format: Base64 encoded SHA-256 hash (44 characters)
    /// </summary>
    public string ApiKeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of this API key
    /// Examples: "Production Device Key", "Development Testing", "Backup Key"
    /// </summary>
    public string Description { get; set; } = string.Empty;

    // ==========================================
    // STATUS & LIFECYCLE
    // ==========================================

    /// <summary>
    /// Whether this API key is currently active
    /// Inactive keys are immediately rejected during authentication
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this API key expires (nullable = never expires)
    /// Best practice: Set to 1 year from creation
    /// Auto-rejected after expiration
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Last time this API key was successfully used
    /// Updated on every successful authentication
    /// Used for detecting stale/unused keys
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Total number of times this API key has been used
    /// Incremented on every successful authentication
    /// Used for usage analytics and anomaly detection
    /// </summary>
    public int UsageCount { get; set; } = 0;

    // ==========================================
    // SECURITY CONTROLS
    // ==========================================

    /// <summary>
    /// JSON array of allowed IP addresses (CIDR notation supported)
    /// Examples: ["192.168.1.100"], ["10.0.0.0/24"], ["*"] for any IP
    /// Empty or null = no IP restriction
    /// Enforced during authentication
    /// </summary>
    public string? AllowedIpAddresses { get; set; }

    /// <summary>
    /// Maximum number of requests allowed per minute
    /// Default: 60 requests/minute (1 request/second)
    /// Prevents abuse and DDoS attacks
    /// Enforced in middleware
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 60;

    // ==========================================
    // METADATA
    // ==========================================

    /// <summary>
    /// Who created this API key
    /// Populated from HttpContext user claims
    /// </summary>
    public new string? CreatedBy { get; set; }

    /// <summary>
    /// Who last modified this API key
    /// Populated from HttpContext user claims
    /// </summary>
    public new string? UpdatedBy { get; set; }

    // ==========================================
    // NAVIGATION PROPERTIES
    // ==========================================

    /// <summary>
    /// The biometric device this key authenticates
    /// </summary>
    public AttendanceMachine? Device { get; set; }

    // ==========================================
    // COMPUTED PROPERTIES
    // ==========================================

    /// <summary>
    /// Check if this API key is currently valid
    /// Valid = Active AND not expired
    /// </summary>
    public bool IsValid => IsActive && !IsExpired;

    /// <summary>
    /// Check if this API key has expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Days until expiration (null if never expires)
    /// </summary>
    public int? DaysUntilExpiration => ExpiresAt.HasValue
        ? Math.Max(0, (int)(ExpiresAt.Value - DateTime.UtcNow).TotalDays)
        : null;

    /// <summary>
    /// Check if this key is expiring soon (within 30 days)
    /// </summary>
    public bool IsExpiringSoon => DaysUntilExpiration.HasValue && DaysUntilExpiration.Value <= 30;

    /// <summary>
    /// Check if this key appears to be unused (never used or not used in 90 days)
    /// </summary>
    public bool IsStale => !LastUsedAt.HasValue ||
                          (DateTime.UtcNow - LastUsedAt.Value).TotalDays > 90;
}
