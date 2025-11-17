using System;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 50: Real-time dashboard metrics aggregated from all monitoring layers
/// Provides high-level system health overview for SuperAdmin dashboard
/// COMPLIANCE: ISO 27001, SOX (monitoring and alerting requirements)
/// </summary>
public class DashboardMetricsDto
{
    /// <summary>
    /// Overall system health status
    /// Values: Healthy, Degraded, Critical, Unknown
    /// </summary>
    public string SystemStatus { get; set; } = "Unknown";

    /// <summary>
    /// Database cache hit rate percentage (target: >95%)
    /// Critical performance indicator for query optimization
    /// </summary>
    public decimal CacheHitRate { get; set; }

    /// <summary>
    /// Current active database connections
    /// Monitor for connection leaks and pool exhaustion
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Connection pool utilization percentage (alert threshold: 80%)
    /// </summary>
    public decimal ConnectionPoolUtilization { get; set; }

    /// <summary>
    /// API P95 response time in milliseconds (SLA target: <200ms)
    /// </summary>
    public decimal ApiResponseTimeP95 { get; set; }

    /// <summary>
    /// API P99 response time in milliseconds (SLA target: <500ms)
    /// </summary>
    public decimal ApiResponseTimeP99 { get; set; }

    /// <summary>
    /// API error rate percentage (target: <0.1%)
    /// </summary>
    public decimal ApiErrorRate { get; set; }

    /// <summary>
    /// Total active tenant count (tenants with activity in last 24h)
    /// </summary>
    public int ActiveTenants { get; set; }

    /// <summary>
    /// Total registered tenants in the system
    /// </summary>
    public int TotalTenants { get; set; }

    /// <summary>
    /// Average schema switch time in milliseconds (target: <10ms)
    /// Critical multi-tenant performance metric
    /// </summary>
    public decimal AvgSchemaSwitchTime { get; set; }

    /// <summary>
    /// Count of active critical alerts requiring immediate attention
    /// </summary>
    public int CriticalAlerts { get; set; }

    /// <summary>
    /// Count of active warning alerts for monitoring
    /// </summary>
    public int WarningAlerts { get; set; }

    /// <summary>
    /// Failed authentication attempts in last hour (threshold: 100)
    /// Security monitoring for potential attacks
    /// </summary>
    public int FailedAuthAttemptsLastHour { get; set; }

    /// <summary>
    /// IDOR prevention triggers in last hour (should be 0)
    /// Security metric for access control violations
    /// </summary>
    public int IdorPreventionTriggersLastHour { get; set; }

    /// <summary>
    /// Timestamp of the most recent metric snapshot
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Timestamp of the next scheduled metric collection
    /// </summary>
    public DateTime NextUpdate { get; set; }
}
