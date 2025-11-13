namespace HRMS.Application.DTOs.BiometricDeviceDtos;

/// <summary>
/// Device API Key information (never contains plaintext key)
/// Used for displaying API key details in management UI
/// </summary>
public class DeviceApiKeyDto
{
    /// <summary>
    /// Unique identifier for this API key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Device this API key authenticates
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Human-readable description of this API key
    /// Examples: "Production Key", "Testing Key", "Backup Key"
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this API key is currently active
    /// Inactive keys are immediately rejected during authentication
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When this API key expires (null = never expires)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Last time this API key was successfully used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Total number of times this API key has been used
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// JSON array of allowed IP addresses
    /// </summary>
    public string? AllowedIpAddresses { get; set; }

    /// <summary>
    /// Maximum requests allowed per minute
    /// </summary>
    public int RateLimitPerMinute { get; set; }

    /// <summary>
    /// When this API key was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Who created this API key
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// When this API key was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Who last updated this API key
    /// </summary>
    public string? UpdatedBy { get; set; }

    // Computed properties

    /// <summary>
    /// Whether this API key is valid (active and not expired)
    /// </summary>
    public bool IsValid => IsActive && !IsExpired;

    /// <summary>
    /// Whether this API key has expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Days until expiration (null if never expires)
    /// </summary>
    public int? DaysUntilExpiration => ExpiresAt.HasValue
        ? Math.Max(0, (int)(ExpiresAt.Value - DateTime.UtcNow).TotalDays)
        : null;

    /// <summary>
    /// Whether this key is expiring soon (within 30 days)
    /// </summary>
    public bool IsExpiringSoon => DaysUntilExpiration.HasValue && DaysUntilExpiration.Value <= 30;
}
