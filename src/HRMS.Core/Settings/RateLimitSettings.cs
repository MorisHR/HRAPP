namespace HRMS.Core.Settings;

/// <summary>
/// Rate limiting configuration for Fortune 500-grade DDoS protection
/// PATTERN: Cloudflare, AWS WAF, Azure Front Door
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Enable rate limiting globally
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Use Redis for distributed rate limiting (production)
    /// Falls back to in-memory cache if Redis unavailable
    /// </summary>
    public bool UseRedis { get; set; } = false;

    /// <summary>
    /// Redis connection string
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Rate limit algorithm: SlidingWindow, FixedWindow, TokenBucket
    /// SlidingWindow = Most accurate, slightly more expensive
    /// FixedWindow = Fastest, potential burst at window boundaries
    /// TokenBucket = Best for bursty traffic
    /// </summary>
    public string Algorithm { get; set; } = "SlidingWindow";

    /// <summary>
    /// General API rate limit (requests per minute)
    /// Cloudflare free tier: 10,000 req/min
    /// </summary>
    public int GeneralLimit { get; set; } = 100;

    /// <summary>
    /// SuperAdmin endpoint rate limit (requests per minute)
    /// STRICTER than general API (high-value targets)
    /// </summary>
    public int SuperAdminLimit { get; set; } = 30;

    /// <summary>
    /// Authentication endpoint rate limit (requests per minute)
    /// VERY STRICT to prevent brute force attacks
    /// </summary>
    public int AuthenticationLimit { get; set; } = 5;

    /// <summary>
    /// Time window in seconds
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// IP whitelist for unlimited access (monitoring tools, health checks)
    /// </summary>
    public List<string> WhitelistedIPs { get; set; } = new()
    {
        "127.0.0.1",
        "::1"
    };

    /// <summary>
    /// HTTP status code to return when rate limit exceeded
    /// 429 = Too Many Requests (standard)
    /// 503 = Service Unavailable (alternative)
    /// </summary>
    public int StatusCode { get; set; } = 429;

    /// <summary>
    /// Enable automatic IP blacklisting after repeated violations
    /// CRITICAL: Prevents persistent attackers
    /// </summary>
    public bool EnableAutoBlacklist { get; set; } = true;

    /// <summary>
    /// Number of rate limit violations before auto-blacklist
    /// </summary>
    public int BlacklistThreshold { get; set; } = 10;

    /// <summary>
    /// Blacklist duration in minutes
    /// </summary>
    public int BlacklistDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Log rate limit violations for security monitoring
    /// </summary>
    public bool LogViolations { get; set; } = true;

    /// <summary>
    /// Send security alerts for persistent violators
    /// </summary>
    public bool AlertOnViolations { get; set; } = true;
}
