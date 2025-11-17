namespace HRMS.Core.Entities.Master;

/// <summary>
/// PRODUCTION-GRADE: Activation email resend audit log
/// FORTUNE 500 PATTERN: Netflix/Stripe audit logging + rate limiting enforcement
/// PERFORMANCE: Indexed on TenantId, RequestedAt for fast lookups
/// SECURITY: Immutable logs for GDPR compliance and security monitoring
/// RATE LIMITING: Enables "max 3 requests per hour per tenant" enforcement
/// MULTI-TENANT: Tracks all activation email resend attempts across subdomains
/// </summary>
public class ActivationResendLog : BaseEntity
{
    /// <summary>
    /// Foreign key to Tenant
    /// CRITICAL: Enables rate limiting per tenant (not just per IP)
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// When the resend request was made (UTC)
    /// PERFORMANCE: Indexed for sliding window rate limit queries
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address that requested resend
    /// SECURITY: Detect brute-force attacks and unauthorized access attempts
    /// FORMAT: IPv4 (15 chars max) or IPv6 (45 chars max)
    /// </summary>
    public string? RequestedFromIp { get; set; }

    /// <summary>
    /// Email address used in the resend request
    /// SECURITY: Must match tenant contact email (validation before resend)
    /// AUDIT: Track who requested activation resend
    /// </summary>
    public string? RequestedByEmail { get; set; }

    /// <summary>
    /// New activation token generated (first 8 chars only - not full token!)
    /// SECURITY: Never store full tokens in logs (PCI-DSS pattern)
    /// FORMAT: "a1b2c3d4..." (truncated for audit trail)
    /// </summary>
    public string? TokenGenerated { get; set; }

    /// <summary>
    /// When the new token expires (UTC)
    /// COMPLIANCE: Track token lifecycle for security audits
    /// DEFAULT: 24 hours from RequestedAt
    /// </summary>
    public DateTime TokenExpiry { get; set; }

    /// <summary>
    /// Was the resend successful?
    /// MONITORING: Track success rate for alerting
    /// FALSE = rate limited, email send failed, or validation failed
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Failure reason if resend was blocked or failed
    /// COMMON VALUES:
    /// - "Rate limit exceeded (3 per hour)"
    /// - "Email send failed: [SMTP error]"
    /// - "Invalid email address"
    /// - "Tenant already activated"
    /// - "Tenant not found"
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// User agent string (browser/device info)
    /// FRAUD DETECTION: Identify bot traffic vs legitimate users
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Device information (parsed from user agent)
    /// ANALYTICS: Understand activation UX across devices
    /// FORMAT: "Mobile - iOS Safari" or "Desktop - Chrome"
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Geolocation (city, country)
    /// FRAUD DETECTION: Flag suspicious location patterns
    /// FORMAT: "Port Louis, Mauritius" or "Unknown"
    /// </summary>
    public string? Geolocation { get; set; }

    /// <summary>
    /// Email delivery success status
    /// MONITORING: Track email delivery rate
    /// </summary>
    public bool EmailDelivered { get; set; } = false;

    /// <summary>
    /// Email send error (if delivery failed)
    /// DEBUGGING: SMTP errors, bounce reasons, etc.
    /// </summary>
    public string? EmailSendError { get; set; }

    /// <summary>
    /// Number of resend attempts from this IP in the last hour
    /// REAL-TIME TRACKING: Used for rate limit enforcement
    /// UPDATED: Before each request (sliding window)
    /// </summary>
    public int ResendCountLastHour { get; set; }

    /// <summary>
    /// Was this request blocked by rate limiting?
    /// ANALYTICS: Track how often rate limits trigger
    /// </summary>
    public bool WasRateLimited { get; set; } = false;

    // Navigation property
    public virtual Tenant? Tenant { get; set; }
}
