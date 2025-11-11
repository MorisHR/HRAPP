using HRMS.Core.Entities.Tenant;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Interface for Fortune 500-grade Device API Key management service
/// Handles secure API key generation, validation, rotation, and lifecycle management
///
/// SECURITY PRINCIPLES:
/// - Constant-time comparison to prevent timing attacks
/// - Secure random generation using cryptographic RNG
/// - Hash-only storage (never store plaintext)
/// - Comprehensive audit logging
/// - Rate limiting integration
/// </summary>
public interface IDeviceApiKeyService
{
    // ==========================================
    // API KEY GENERATION & MANAGEMENT
    // ==========================================

    /// <summary>
    /// Generate a new API key for a device
    /// Returns both the entity (with hashed key) and the plaintext key (shown ONLY once)
    ///
    /// SECURITY:
    /// - Generates cryptographically secure 64-character base64url string
    /// - Stores SHA-256 hash in database
    /// - Returns plaintext ONLY at creation time
    /// - Logs creation in audit trail
    ///
    /// USAGE:
    /// var (apiKey, plaintextKey) = await GenerateApiKeyAsync(deviceId, "Production Key", DateTime.UtcNow.AddYears(1));
    /// // Display plaintextKey to user ONCE, then discard
    /// </summary>
    /// <param name="deviceId">ID of the device this key authenticates</param>
    /// <param name="description">Human-readable description</param>
    /// <param name="expiresAt">When this key should expire (null = never)</param>
    /// <param name="allowedIpAddresses">JSON array of allowed IPs (null = any IP)</param>
    /// <param name="rateLimitPerMinute">Max requests per minute (default 60)</param>
    /// <returns>Tuple of (DeviceApiKey entity, plaintext key)</returns>
    Task<(DeviceApiKey ApiKey, string PlaintextKey)> GenerateApiKeyAsync(
        Guid deviceId,
        string description,
        DateTime? expiresAt = null,
        string? allowedIpAddresses = null,
        int rateLimitPerMinute = 60);

    /// <summary>
    /// Validate an API key and check all security controls
    ///
    /// VALIDATION CHECKS:
    /// 1. Key exists in database (constant-time hash comparison)
    /// 2. Key is active (IsActive = true)
    /// 3. Key has not expired
    /// 4. IP address is allowed (if configured)
    /// 5. Rate limit not exceeded
    ///
    /// SIDE EFFECTS:
    /// - Updates LastUsedAt timestamp
    /// - Increments UsageCount
    /// - Logs authentication attempt (success/failure)
    /// - Enforces rate limiting
    /// </summary>
    /// <param name="plaintextApiKey">The API key to validate (from request header)</param>
    /// <param name="ipAddress">Client IP address (for IP whitelisting)</param>
    /// <returns>Validation result with device ID if successful</returns>
    Task<DeviceApiKeyValidationResult> ValidateApiKeyAsync(string plaintextApiKey, string ipAddress);

    // ==========================================
    // KEY LIFECYCLE MANAGEMENT
    // ==========================================

    /// <summary>
    /// Revoke an API key (mark as inactive)
    /// Immediate effect - key cannot be used after revocation
    /// Does NOT delete from database (maintains audit trail)
    ///
    /// USE CASES:
    /// - Key compromise suspected
    /// - Device decommissioned
    /// - Routine key rotation
    /// </summary>
    /// <param name="apiKeyId">ID of the API key to revoke</param>
    /// <returns>True if revoked successfully</returns>
    Task<bool> RevokeApiKeyAsync(Guid apiKeyId);

    /// <summary>
    /// Rotate an API key (revoke old, generate new)
    /// Atomic operation - ensures continuous access during rotation
    ///
    /// PROCESS:
    /// 1. Generate new API key with same settings
    /// 2. Mark old key as inactive
    /// 3. Return new key (plaintext shown ONLY once)
    ///
    /// BEST PRACTICE:
    /// - Rotate keys every 90 days
    /// - Rotate immediately if compromise suspected
    /// </summary>
    /// <param name="oldApiKeyId">ID of the API key to rotate</param>
    /// <returns>Tuple of (new DeviceApiKey entity, new plaintext key)</returns>
    Task<(DeviceApiKey NewApiKey, string NewPlaintextKey)> RotateApiKeyAsync(Guid oldApiKeyId);

    // ==========================================
    // QUERY & REPORTING
    // ==========================================

    /// <summary>
    /// Get all API keys for a specific device
    /// Useful for key management UI
    /// </summary>
    /// <param name="deviceId">ID of the device</param>
    /// <returns>List of API keys (hashed only, never plaintext)</returns>
    Task<List<DeviceApiKey>> GetDeviceApiKeysAsync(Guid deviceId);

    /// <summary>
    /// Get a specific API key by ID
    /// </summary>
    /// <param name="apiKeyId">ID of the API key</param>
    /// <returns>API key entity (hashed only)</returns>
    Task<DeviceApiKey?> GetApiKeyByIdAsync(Guid apiKeyId);

    /// <summary>
    /// Get all API keys for a tenant
    /// Used for tenant-wide key management
    /// </summary>
    /// <param name="tenantId">ID of the tenant</param>
    /// <returns>List of all API keys in tenant</returns>
    Task<List<DeviceApiKey>> GetTenantApiKeysAsync(Guid tenantId);

    /// <summary>
    /// Find expiring API keys (within specified days)
    /// Used for proactive rotation alerts
    /// </summary>
    /// <param name="daysUntilExpiration">Number of days threshold (default 30)</param>
    /// <returns>List of keys expiring soon</returns>
    Task<List<DeviceApiKey>> GetExpiringApiKeysAsync(int daysUntilExpiration = 30);

    /// <summary>
    /// Find stale API keys (not used in specified days)
    /// Used for security cleanup
    /// </summary>
    /// <param name="daysUnused">Number of days threshold (default 90)</param>
    /// <returns>List of unused keys</returns>
    Task<List<DeviceApiKey>> GetStaleApiKeysAsync(int daysUnused = 90);
}

/// <summary>
/// Result of API key validation
/// Contains all information needed for authentication decision
/// </summary>
public class DeviceApiKeyValidationResult
{
    /// <summary>
    /// Whether the API key is valid and authentication succeeded
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The device ID associated with this API key (if valid)
    /// </summary>
    public Guid? DeviceId { get; set; }

    /// <summary>
    /// The tenant ID associated with this API key (if valid)
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// The API key entity (if found)
    /// </summary>
    public DeviceApiKey? ApiKey { get; set; }

    /// <summary>
    /// Reason for validation failure (if invalid)
    /// Examples: "Key not found", "Key expired", "IP not allowed", "Rate limit exceeded"
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Whether rate limit was exceeded
    /// </summary>
    public bool RateLimitExceeded { get; set; }

    /// <summary>
    /// Seconds to wait before retrying (if rate limited)
    /// </summary>
    public int RetryAfterSeconds { get; set; }
}
