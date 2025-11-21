namespace HRMS.Core.Models;

/// <summary>
/// Service tier classification for intelligent health monitoring
/// PATTERN: Google SRE - Service Level Objectives (SLOs)
/// REFERENCE: "Site Reliability Engineering" (Google, 2016)
/// </summary>
public enum ServiceHealthTier
{
    /// <summary>
    /// CRITICAL: System cannot function without this service
    /// Examples: Database, API Gateway, Authentication
    /// SLO: 99.95% uptime minimum
    /// Impact: If down, entire system is DOWN
    /// </summary>
    Critical = 1,

    /// <summary>
    /// IMPORTANT: Reduced functionality if degraded
    /// Examples: Storage, Email, Background Jobs
    /// SLO: 99.9% uptime target
    /// Impact: Graceful degradation possible
    /// </summary>
    Important = 2,

    /// <summary>
    /// OPTIONAL: Has fallback mechanism, degradation acceptable
    /// Examples: Redis Cache (→ in-memory), CDN (→ origin), Analytics
    /// SLO: 95% uptime acceptable
    /// Impact: Minimal - fallback handles it
    /// </summary>
    Optional = 3,

    /// <summary>
    /// INFORMATIONAL: Monitoring/metrics only, no user impact
    /// Examples: Prometheus, Grafana, Log aggregation
    /// SLO: Best effort
    /// Impact: None on user-facing functionality
    /// </summary>
    Informational = 4
}

/// <summary>
/// Service health configuration with intelligent weighting
/// PATTERN: Netflix Chaos Engineering + AWS Well-Architected Framework
/// </summary>
public class ServiceHealthConfig
{
    public string ServiceName { get; set; } = string.Empty;
    public ServiceHealthTier Tier { get; set; }

    /// <summary>
    /// Weight in overall health score (0.0 to 1.0)
    /// CRITICAL services: 0.4-0.5 each
    /// IMPORTANT services: 0.1-0.2 each
    /// OPTIONAL services: 0.01-0.05 each
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// Does this service have a working fallback mechanism?
    /// If true, degradation has minimal impact
    /// </summary>
    public bool HasFallback { get; set; }

    /// <summary>
    /// Target uptime percentage (SLO)
    /// CRITICAL: 99.95%+
    /// IMPORTANT: 99.9%+
    /// OPTIONAL: 95%+
    /// </summary>
    public double TargetUptimeSLO { get; set; }

    /// <summary>
    /// Impact multiplier when service is degraded
    /// 1.0 = Full impact (no fallback)
    /// 0.1 = Minimal impact (has fallback)
    /// 0.0 = No impact (informational only)
    /// </summary>
    public double ImpactMultiplier => HasFallback ? 0.1 : 1.0;
}

/// <summary>
/// Intelligent health calculation result
/// PATTERN: Datadog Multi-Dimensional Metrics
/// </summary>
public class IntelligentHealthScore
{
    /// <summary>Overall system status</summary>
    public SystemStatus Status { get; set; }

    /// <summary>
    /// Weighted health score (0-100)
    /// Accounts for service tiers and fallback mechanisms
    /// </summary>
    public double WeightedHealthScore { get; set; }

    /// <summary>
    /// Simple average uptime (for comparison)
    /// Shows what naive calculation would give
    /// </summary>
    public double SimpleAverageUptime { get; set; }

    /// <summary>Critical services health (99.95%+ SLO)</summary>
    public double CriticalServicesHealth { get; set; }

    /// <summary>Important services health (99.9%+ SLO)</summary>
    public double ImportantServicesHealth { get; set; }

    /// <summary>Services below their SLO target</summary>
    public List<string> ServicesBelowSLO { get; set; } = new();

    /// <summary>Services with active fallbacks</summary>
    public List<string> ServicesUsingFallback { get; set; } = new();

    /// <summary>Recommended alert level</summary>
    public AlertLevel RecommendedAlertLevel { get; set; }

    /// <summary>Human-readable explanation</summary>
    public string HealthExplanation { get; set; } = string.Empty;
}

public enum SystemStatus
{
    /// <summary>All critical services healthy, most important services healthy</summary>
    Healthy,

    /// <summary>All critical services healthy, but some important services degraded</summary>
    Degraded,

    /// <summary>One or more critical services unhealthy</summary>
    Down
}

public enum AlertLevel
{
    /// <summary>No alert needed - informational only</summary>
    Info,

    /// <summary>Service degraded but has fallback - monitor</summary>
    Warning,

    /// <summary>Important service down or critical service degraded - investigate</summary>
    Critical,

    /// <summary>Critical service down - immediate action required</summary>
    Emergency
}
