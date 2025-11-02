namespace HRMS.Core.Settings;

/// <summary>
/// Rate limiting configuration to prevent abuse and DoS attacks
/// </summary>
public class RateLimitingSettings
{
    /// <summary>
    /// Enable endpoint-based rate limiting
    /// </summary>
    public bool EnableEndpointRateLimiting { get; set; } = true;

    /// <summary>
    /// Stack blocked requests (count them against the limit)
    /// </summary>
    public bool StackBlockedRequests { get; set; }

    /// <summary>
    /// Header containing real IP address (for load balancer/proxy scenarios)
    /// </summary>
    public string RealIpHeader { get; set; } = "X-Real-IP";

    /// <summary>
    /// Header containing client ID
    /// </summary>
    public string ClientIdHeader { get; set; } = "X-ClientId";

    /// <summary>
    /// HTTP status code to return when rate limit is exceeded
    /// </summary>
    public int HttpStatusCode { get; set; } = 429;

    /// <summary>
    /// General rate limiting rules
    /// </summary>
    public List<RateLimitRule> GeneralRules { get; set; } = new();
}

/// <summary>
/// Individual rate limit rule
/// </summary>
public class RateLimitRule
{
    /// <summary>
    /// Endpoint pattern (e.g., "*", "*/auth/login")
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Time period (e.g., "1s", "1m", "1h", "1d")
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of requests allowed in the period
    /// </summary>
    public int Limit { get; set; }
}
