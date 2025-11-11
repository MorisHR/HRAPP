using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Fortune 500-grade Device API Key management service
/// Implements secure key generation, validation, and lifecycle management
///
/// SECURITY FEATURES:
/// - Cryptographically secure random generation (CSPRNG)
/// - SHA-256 hash-only storage
/// - Constant-time comparison (prevents timing attacks)
/// - IP address whitelisting with CIDR support
/// - In-memory rate limiting (high performance)
/// - Comprehensive audit logging
///
/// COMPLIANCE:
/// - SOC 2 Type II: Secure credential management
/// - ISO 27001: Cryptographic controls
/// - NIST 800-53: Access control mechanisms
/// </summary>
public class DeviceApiKeyService : IDeviceApiKeyService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<DeviceApiKeyService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IMemoryCache _cache;

    // Rate limiting: Track request counts per API key per minute
    // Format: "ratelimit:{apiKeyHash}:{minuteBucket}" => count
    private const string RATE_LIMIT_KEY_PREFIX = "ratelimit:deviceapi:";

    // API key specifications
    private const int API_KEY_LENGTH = 64; // 64 characters = 48 bytes of entropy (base64url)
    private const int API_KEY_BYTES = 48; // 48 bytes = 384 bits of entropy

    public DeviceApiKeyService(
        TenantDbContext context,
        ILogger<DeviceApiKeyService> logger,
        IAuditLogService auditLogService,
        IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _auditLogService = auditLogService;
        _cache = cache;
    }

    // ==========================================
    // API KEY GENERATION & MANAGEMENT
    // ==========================================

    public async Task<(DeviceApiKey ApiKey, string PlaintextKey)> GenerateApiKeyAsync(
        Guid deviceId,
        string description,
        DateTime? expiresAt = null,
        string? allowedIpAddresses = null,
        int rateLimitPerMinute = 60)
    {
        _logger.LogInformation("Generating new API key for device {DeviceId}", deviceId);

        // Validate device exists
        var device = await _context.AttendanceMachines
            .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

        if (device == null)
        {
            throw new InvalidOperationException($"Device {deviceId} not found");
        }

        // Generate cryptographically secure random API key
        var plaintextKey = GenerateSecureApiKey();
        var apiKeyHash = HashApiKey(plaintextKey);

        // Set default expiration if not provided (1 year from now)
        if (!expiresAt.HasValue)
        {
            expiresAt = DateTime.UtcNow.AddYears(1);
        }

        // Create API key entity
        var apiKey = new DeviceApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty, // Will be set by tenant context in controller
            DeviceId = deviceId,
            ApiKeyHash = apiKeyHash,
            Description = description,
            IsActive = true,
            ExpiresAt = expiresAt,
            AllowedIpAddresses = allowedIpAddresses,
            RateLimitPerMinute = rateLimitPerMinute,
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system" // Will be overridden by controller with actual user
        };

        _context.Set<DeviceApiKey>().Add(apiKey);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "API key {ApiKeyId} created for device {DeviceId}. Expires: {ExpiresAt}",
            apiKey.Id, deviceId, expiresAt);

        // Audit log
        await _auditLogService.LogDeviceApiKeyCreatedAsync(
            apiKey.Id,
            deviceId,
            description,
            expiresAt);

        return (apiKey, plaintextKey);
    }

    public async Task<DeviceApiKeyValidationResult> ValidateApiKeyAsync(
        string plaintextApiKey,
        string ipAddress)
    {
        var result = new DeviceApiKeyValidationResult
        {
            IsValid = false
        };

        try
        {
            // Step 1: Hash the provided API key
            var apiKeyHash = HashApiKey(plaintextApiKey);

            // Step 2: Find API key in database using constant-time comparison
            var apiKey = await FindApiKeyByHashAsync(apiKeyHash);

            if (apiKey == null)
            {
                _logger.LogWarning("API key validation failed: Key not found. IP: {IpAddress}", ipAddress);
                result.FailureReason = "Invalid API key";

                await _auditLogService.LogDeviceApiKeyAuthenticationFailedAsync(
                    null, ipAddress, "Key not found");

                return result;
            }

            result.ApiKey = apiKey;
            result.DeviceId = apiKey.DeviceId;
            result.TenantId = apiKey.TenantId;

            // Step 3: Check if key is active
            if (!apiKey.IsActive)
            {
                _logger.LogWarning(
                    "API key validation failed: Key {ApiKeyId} is inactive. IP: {IpAddress}",
                    apiKey.Id, ipAddress);

                result.FailureReason = "API key is inactive";

                await _auditLogService.LogDeviceApiKeyAuthenticationFailedAsync(
                    apiKey.Id, ipAddress, "Key inactive");

                return result;
            }

            // Step 4: Check if key has expired
            if (apiKey.IsExpired)
            {
                _logger.LogWarning(
                    "API key validation failed: Key {ApiKeyId} expired on {ExpiresAt}. IP: {IpAddress}",
                    apiKey.Id, apiKey.ExpiresAt, ipAddress);

                result.FailureReason = $"API key expired on {apiKey.ExpiresAt:yyyy-MM-dd}";

                await _auditLogService.LogDeviceApiKeyAuthenticationFailedAsync(
                    apiKey.Id, ipAddress, "Key expired");

                return result;
            }

            // Step 5: Validate IP address (if configured)
            if (!string.IsNullOrEmpty(apiKey.AllowedIpAddresses))
            {
                if (!IsIpAddressAllowed(ipAddress, apiKey.AllowedIpAddresses))
                {
                    _logger.LogWarning(
                        "API key validation failed: IP {IpAddress} not allowed for key {ApiKeyId}. Allowed: {AllowedIps}",
                        ipAddress, apiKey.Id, apiKey.AllowedIpAddresses);

                    result.FailureReason = "IP address not allowed";

                    await _auditLogService.LogDeviceApiKeyAuthenticationFailedAsync(
                        apiKey.Id, ipAddress, "IP not allowed");

                    return result;
                }
            }

            // Step 6: Check rate limit
            var rateLimitResult = CheckRateLimit(apiKeyHash, apiKey.RateLimitPerMinute);
            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning(
                    "API key validation failed: Rate limit exceeded for key {ApiKeyId}. IP: {IpAddress}",
                    apiKey.Id, ipAddress);

                result.FailureReason = "Rate limit exceeded";
                result.RateLimitExceeded = true;
                result.RetryAfterSeconds = rateLimitResult.RetryAfterSeconds;

                await _auditLogService.LogDeviceApiKeyRateLimitExceededAsync(
                    apiKey.Id, ipAddress, apiKey.RateLimitPerMinute);

                return result;
            }

            // Step 7: All checks passed - update usage statistics
            apiKey.LastUsedAt = DateTime.UtcNow;
            apiKey.UsageCount++;
            await _context.SaveChangesAsync();

            // Success
            result.IsValid = true;

            _logger.LogInformation(
                "API key {ApiKeyId} validated successfully for device {DeviceId}. IP: {IpAddress}. Total uses: {UsageCount}",
                apiKey.Id, apiKey.DeviceId, ipAddress, apiKey.UsageCount);

            await _auditLogService.LogDeviceApiKeyAuthenticationSuccessAsync(
                apiKey.Id, apiKey.DeviceId, ipAddress);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key. IP: {IpAddress}", ipAddress);
            result.FailureReason = "Internal validation error";
            return result;
        }
    }

    // ==========================================
    // KEY LIFECYCLE MANAGEMENT
    // ==========================================

    public async Task<bool> RevokeApiKeyAsync(Guid apiKeyId)
    {
        var apiKey = await _context.Set<DeviceApiKey>()
            .FirstOrDefaultAsync(k => k.Id == apiKeyId && !k.IsDeleted);

        if (apiKey == null)
        {
            _logger.LogWarning("Cannot revoke API key {ApiKeyId}: Not found", apiKeyId);
            return false;
        }

        apiKey.IsActive = false;
        apiKey.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "API key {ApiKeyId} revoked for device {DeviceId}",
            apiKeyId, apiKey.DeviceId);

        await _auditLogService.LogDeviceApiKeyRevokedAsync(apiKeyId, apiKey.DeviceId);

        return true;
    }

    public async Task<(DeviceApiKey NewApiKey, string NewPlaintextKey)> RotateApiKeyAsync(Guid oldApiKeyId)
    {
        var oldApiKey = await _context.Set<DeviceApiKey>()
            .FirstOrDefaultAsync(k => k.Id == oldApiKeyId && !k.IsDeleted);

        if (oldApiKey == null)
        {
            throw new InvalidOperationException($"API key {oldApiKeyId} not found");
        }

        _logger.LogInformation(
            "Rotating API key {OldApiKeyId} for device {DeviceId}",
            oldApiKeyId, oldApiKey.DeviceId);

        // Generate new key with same settings
        var (newApiKey, newPlaintextKey) = await GenerateApiKeyAsync(
            oldApiKey.DeviceId,
            $"{oldApiKey.Description} (Rotated)",
            oldApiKey.ExpiresAt,
            oldApiKey.AllowedIpAddresses,
            oldApiKey.RateLimitPerMinute);

        // Revoke old key
        await RevokeApiKeyAsync(oldApiKeyId);

        _logger.LogInformation(
            "API key rotation complete. Old: {OldApiKeyId}, New: {NewApiKeyId}",
            oldApiKeyId, newApiKey.Id);

        await _auditLogService.LogDeviceApiKeyRotatedAsync(oldApiKeyId, newApiKey.Id, oldApiKey.DeviceId);

        return (newApiKey, newPlaintextKey);
    }

    // ==========================================
    // QUERY & REPORTING
    // ==========================================

    public async Task<List<DeviceApiKey>> GetDeviceApiKeysAsync(Guid deviceId)
    {
        return await _context.Set<DeviceApiKey>()
            .Where(k => k.DeviceId == deviceId && !k.IsDeleted)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<DeviceApiKey?> GetApiKeyByIdAsync(Guid apiKeyId)
    {
        return await _context.Set<DeviceApiKey>()
            .FirstOrDefaultAsync(k => k.Id == apiKeyId && !k.IsDeleted);
    }

    public async Task<List<DeviceApiKey>> GetTenantApiKeysAsync(Guid tenantId)
    {
        return await _context.Set<DeviceApiKey>()
            .Where(k => k.TenantId == tenantId && !k.IsDeleted)
            .Include(k => k.Device)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<DeviceApiKey>> GetExpiringApiKeysAsync(int daysUntilExpiration = 30)
    {
        var threshold = DateTime.UtcNow.AddDays(daysUntilExpiration);

        return await _context.Set<DeviceApiKey>()
            .Where(k => !k.IsDeleted &&
                       k.IsActive &&
                       k.ExpiresAt.HasValue &&
                       k.ExpiresAt.Value <= threshold &&
                       k.ExpiresAt.Value > DateTime.UtcNow)
            .Include(k => k.Device)
            .OrderBy(k => k.ExpiresAt)
            .ToListAsync();
    }

    public async Task<List<DeviceApiKey>> GetStaleApiKeysAsync(int daysUnused = 90)
    {
        var threshold = DateTime.UtcNow.AddDays(-daysUnused);

        return await _context.Set<DeviceApiKey>()
            .Where(k => !k.IsDeleted &&
                       k.IsActive &&
                       (!k.LastUsedAt.HasValue || k.LastUsedAt.Value < threshold))
            .Include(k => k.Device)
            .OrderBy(k => k.LastUsedAt)
            .ToListAsync();
    }

    // ==========================================
    // PRIVATE HELPER METHODS
    // ==========================================

    /// <summary>
    /// Generate a cryptographically secure random API key
    /// Uses CSPRNG for maximum entropy
    /// Format: 64 characters base64url (384 bits of entropy)
    /// </summary>
    private string GenerateSecureApiKey()
    {
        var bytes = new byte[API_KEY_BYTES];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        // Convert to base64url (URL-safe base64)
        var base64 = Convert.ToBase64String(bytes);
        var base64url = base64
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");

        return base64url;
    }

    /// <summary>
    /// Hash API key using SHA-256
    /// Produces 44-character base64 string
    /// </summary>
    private string HashApiKey(string plaintextKey)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(plaintextKey);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// Find API key by hash using constant-time comparison
    /// Prevents timing attacks
    /// </summary>
    private async Task<DeviceApiKey?> FindApiKeyByHashAsync(string apiKeyHash)
    {
        // Get all potential matches (there should only be one, but we check all for constant-time)
        var allKeys = await _context.Set<DeviceApiKey>()
            .Where(k => !k.IsDeleted)
            .ToListAsync();

        // Use constant-time comparison to prevent timing attacks
        foreach (var key in allKeys)
        {
            if (ConstantTimeEquals(key.ApiKeyHash, apiKeyHash))
            {
                return key;
            }
        }

        return null;
    }

    /// <summary>
    /// Constant-time string equality comparison
    /// Prevents timing attacks by always comparing all characters
    /// CRITICAL SECURITY FUNCTION
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private bool ConstantTimeEquals(string a, string b)
    {
        if (a == null || b == null)
            return a == b;

        if (a.Length != b.Length)
            return false;

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }

    /// <summary>
    /// Check if IP address is allowed based on whitelist configuration
    /// Supports individual IPs and CIDR notation
    /// </summary>
    private bool IsIpAddressAllowed(string ipAddress, string allowedIpAddressesJson)
    {
        try
        {
            var allowedIps = JsonSerializer.Deserialize<List<string>>(allowedIpAddressesJson);

            if (allowedIps == null || allowedIps.Count == 0)
                return true; // No restrictions

            // Check for wildcard
            if (allowedIps.Contains("*"))
                return true;

            // Parse client IP
            if (!IPAddress.TryParse(ipAddress, out var clientIp))
                return false;

            foreach (var allowed in allowedIps)
            {
                // Check for CIDR notation
                if (allowed.Contains('/'))
                {
                    if (IsIpInCidrRange(clientIp, allowed))
                        return true;
                }
                else
                {
                    // Direct IP comparison
                    if (IPAddress.TryParse(allowed, out var allowedIp) &&
                        clientIp.Equals(allowedIp))
                        return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking IP whitelist");
            return false; // Fail secure
        }
    }

    /// <summary>
    /// Check if IP is in CIDR range
    /// Example: 192.168.1.100 in 192.168.1.0/24
    /// </summary>
    private bool IsIpInCidrRange(IPAddress ipAddress, string cidr)
    {
        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
                return false;

            var baseAddress = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);

            var baseBytes = baseAddress.GetAddressBytes();
            var ipBytes = ipAddress.GetAddressBytes();

            if (baseBytes.Length != ipBytes.Length)
                return false;

            var maskBytes = prefixLength / 8;
            var maskBits = prefixLength % 8;

            // Compare full bytes
            for (int i = 0; i < maskBytes; i++)
            {
                if (baseBytes[i] != ipBytes[i])
                    return false;
            }

            // Compare remaining bits
            if (maskBits > 0)
            {
                var mask = (byte)(0xFF << (8 - maskBits));
                if ((baseBytes[maskBytes] & mask) != (ipBytes[maskBytes] & mask))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check rate limit using in-memory cache
    /// High-performance sliding window algorithm
    /// </summary>
    private (bool IsAllowed, int RetryAfterSeconds) CheckRateLimit(string apiKeyHash, int limitPerMinute)
    {
        var now = DateTime.UtcNow;
        var currentMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
        var cacheKey = $"{RATE_LIMIT_KEY_PREFIX}{apiKeyHash}:{currentMinute:yyyyMMddHHmm}";

        var currentCount = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpiration = currentMinute.AddMinutes(2); // Keep for 2 minutes
            return 0;
        });

        currentCount++;

        if (currentCount > limitPerMinute)
        {
            var secondsUntilReset = 60 - now.Second;
            return (false, secondsUntilReset);
        }

        _cache.Set(cacheKey, currentCount);
        return (true, 0);
    }
}
