namespace HRMS.Application.Interfaces;

/// <summary>
/// High-performance distributed rate limiting service
/// FORTUNE 500 PATTERN: Cloudflare Rate Limiting, AWS WAF, Azure Front Door
///
/// PERFORMANCE TARGET: Sub-millisecond overhead (&lt;1ms per request)
/// SECURITY: DDoS protection, brute force prevention, auto-blacklisting
/// SCALABILITY: Redis-backed for multi-instance deployments
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Check if a request is allowed based on rate limit
    /// PERFORMANCE: Optimized for &lt;1ms execution time
    /// </summary>
    /// <param name="key">Unique identifier (IP address, user ID, etc.)</param>
    /// <param name="limit">Maximum requests allowed</param>
    /// <param name="window">Time window for rate limit</param>
    /// <returns>RateLimitResult containing decision and metadata</returns>
    Task<RateLimitResult> CheckRateLimitAsync(string key, int limit, TimeSpan window);

    /// <summary>
    /// Check rate limit with endpoint-specific configuration
    /// Automatically applies correct limits based on endpoint type
    /// </summary>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="endpoint">Endpoint category (general, superadmin, authentication)</param>
    /// <param name="userId">Optional user ID for authenticated requests</param>
    /// <returns>RateLimitResult with enforcement decision</returns>
    Task<RateLimitResult> CheckEndpointRateLimitAsync(string ipAddress, string endpoint, Guid? userId = null);

    /// <summary>
    /// Blacklist an IP address for specified duration
    /// CRITICAL: Blocks persistent attackers
    /// </summary>
    /// <param name="ipAddress">IP address to blacklist</param>
    /// <param name="duration">Blacklist duration</param>
    /// <param name="reason">Reason for blacklisting</param>
    /// <returns>Success status</returns>
    Task<bool> BlacklistIpAsync(string ipAddress, TimeSpan duration, string reason);

    /// <summary>
    /// Check if an IP address is currently blacklisted
    /// PERFORMANCE: Cached lookup for speed
    /// </summary>
    /// <param name="ipAddress">IP address to check</param>
    /// <returns>True if blacklisted, false otherwise</returns>
    Task<bool> IsBlacklistedAsync(string ipAddress);

    /// <summary>
    /// Remove an IP from blacklist (manual override)
    /// Requires SECURITY_SETTINGS_WRITE permission
    /// </summary>
    /// <param name="ipAddress">IP address to unblock</param>
    /// <returns>Success status</returns>
    Task<bool> UnblacklistIpAsync(string ipAddress);

    /// <summary>
    /// Record a rate limit violation for monitoring
    /// Auto-blacklists after threshold exceeded
    /// </summary>
    /// <param name="ipAddress">IP address violating rate limit</param>
    /// <param name="endpoint">Endpoint being accessed</param>
    /// <returns>True if auto-blacklisted</returns>
    Task<bool> RecordViolationAsync(string ipAddress, string endpoint);

    /// <summary>
    /// Get current rate limit status for a key
    /// Useful for debugging and monitoring
    /// </summary>
    /// <param name="key">Rate limit key</param>
    /// <returns>Current request count and reset time</returns>
    Task<RateLimitStatus> GetRateLimitStatusAsync(string key);

    /// <summary>
    /// Reset rate limit for a specific key (admin override)
    /// </summary>
    /// <param name="key">Rate limit key to reset</param>
    /// <returns>Success status</returns>
    Task<bool> ResetRateLimitAsync(string key);
}

/// <summary>
/// Rate limit check result
/// </summary>
public class RateLimitResult
{
    /// <summary>Is the request allowed?</summary>
    public bool IsAllowed { get; set; }

    /// <summary>Current request count in window</summary>
    public int CurrentCount { get; set; }

    /// <summary>Maximum allowed requests</summary>
    public int Limit { get; set; }

    /// <summary>When the rate limit resets (UTC)</summary>
    public DateTime ResetsAt { get; set; }

    /// <summary>Remaining requests in current window</summary>
    public int Remaining => Math.Max(0, Limit - CurrentCount);

    /// <summary>Seconds until rate limit resets</summary>
    public int RetryAfterSeconds { get; set; }

    /// <summary>Reason for denial (if IsAllowed = false)</summary>
    public string? DenialReason { get; set; }

    /// <summary>Is the IP blacklisted?</summary>
    public bool IsBlacklisted { get; set; }
}

/// <summary>
/// Current rate limit status
/// </summary>
public class RateLimitStatus
{
    /// <summary>Rate limit key</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Current request count</summary>
    public int CurrentCount { get; set; }

    /// <summary>Configured limit</summary>
    public int Limit { get; set; }

    /// <summary>Window start time (UTC)</summary>
    public DateTime WindowStart { get; set; }

    /// <summary>Window end time (UTC)</summary>
    public DateTime WindowEnd { get; set; }

    /// <summary>Is the key blacklisted?</summary>
    public bool IsBlacklisted { get; set; }

    /// <summary>Recent violations count</summary>
    public int ViolationCount { get; set; }
}
