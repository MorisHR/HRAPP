using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.BiometricDeviceDtos;

/// <summary>
/// Request to generate a new API key for a biometric device
/// </summary>
public class GenerateApiKeyRequest
{
    /// <summary>
    /// Human-readable description of this API key
    /// Required for identifying the purpose of this key
    /// Examples: "Production Key", "Testing Key", "Backup Key"
    /// </summary>
    [Required(ErrorMessage = "Description is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Description must be between 3 and 200 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When this API key should expire (optional)
    /// If not provided, defaults to 1 year from creation
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// JSON array of allowed IP addresses (optional)
    /// Examples: ["192.168.1.100"], ["10.0.0.0/24"], ["*"] for any IP
    /// If not provided, any IP is allowed
    /// </summary>
    [StringLength(500, ErrorMessage = "Allowed IP addresses must not exceed 500 characters")]
    public string? AllowedIpAddresses { get; set; }

    /// <summary>
    /// Maximum requests allowed per minute (default: 60)
    /// Prevents abuse and DDoS attacks
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Rate limit must be between 1 and 1000 requests per minute")]
    public int RateLimitPerMinute { get; set; } = 60;
}
