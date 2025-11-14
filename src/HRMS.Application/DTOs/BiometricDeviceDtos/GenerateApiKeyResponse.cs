namespace HRMS.Application.DTOs.BiometricDeviceDtos;

/// <summary>
/// Response containing a newly generated API key
/// CRITICAL: The PlaintextKey is shown ONLY ONCE during generation
/// It is never stored in the database and cannot be retrieved later
/// </summary>
public class GenerateApiKeyResponse
{
    /// <summary>
    /// Unique identifier for this API key
    /// </summary>
    public Guid ApiKeyId { get; set; }

    /// <summary>
    /// The plaintext API key
    /// CRITICAL: This is shown ONLY ONCE
    /// Store this securely - it cannot be retrieved later
    /// Format: 64-character base64url string (384 bits of entropy)
    /// </summary>
    public string PlaintextKey { get; set; } = string.Empty;

    /// <summary>
    /// Description of this API key
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When this API key expires
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this API key is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When this API key was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Maximum requests allowed per minute
    /// </summary>
    public int RateLimitPerMinute { get; set; }

    /// <summary>
    /// JSON array of allowed IP addresses (optional)
    /// </summary>
    public string? AllowedIpAddresses { get; set; }

    /// <summary>
    /// Important security warning message
    /// </summary>
    public string SecurityWarning { get; set; } = "IMPORTANT: Save this API key securely. It will not be shown again and cannot be retrieved later.";
}
