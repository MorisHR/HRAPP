using System;
using System.Collections.Generic;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 500: Comprehensive system health status
/// Designed for high-concurrency health checks from load balancers and monitoring systems
///
/// PERFORMANCE: Optimized for 1000+ requests/sec with Redis caching (30s TTL)
/// PATTERN: Netflix Hystrix, AWS CloudWatch, Datadog Health API
/// COMPLIANCE: Supports SOC 2 Type II continuous monitoring requirements
/// </summary>
public class SystemHealthDto
{
    /// <summary>
    /// Overall system health status
    /// Values: "Healthy", "Degraded", "Unhealthy"
    /// </summary>
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// Human-readable message describing system state
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when health check was performed
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Total system uptime in hours
    /// </summary>
    public decimal UptimeHours { get; set; }

    /// <summary>
    /// Database health and connection pool status
    /// </summary>
    public ComponentHealthDto Database { get; set; } = new();

    /// <summary>
    /// Redis cache health and availability
    /// </summary>
    public ComponentHealthDto Cache { get; set; } = new();

    /// <summary>
    /// Background job processor (Hangfire) health
    /// </summary>
    public ComponentHealthDto BackgroundJobs { get; set; } = new();

    /// <summary>
    /// API gateway and routing health
    /// </summary>
    public ComponentHealthDto ApiGateway { get; set; } = new();

    /// <summary>
    /// Current API performance metrics
    /// </summary>
    public PerformanceMetrics Performance { get; set; } = new();

    /// <summary>
    /// Current system resource utilization
    /// </summary>
    public ResourceUtilization Resources { get; set; } = new();

    /// <summary>
    /// Multi-tenant statistics
    /// </summary>
    public TenantStatistics Tenants { get; set; } = new();

    /// <summary>
    /// Active concurrent users across all tenants
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// Total active sessions
    /// </summary>
    public int ActiveSessions { get; set; }

    /// <summary>
    /// Number of active critical/high alerts
    /// </summary>
    public int ActiveAlerts { get; set; }

    /// <summary>
    /// List of any warnings or degraded services
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Application version
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Environment (Development, Staging, Production)
    /// </summary>
    public string Environment { get; set; } = string.Empty;
}

/// <summary>
/// Health status of individual system component
/// </summary>
public class ComponentHealthDto
{
    /// <summary>
    /// Component health status: "Healthy", "Degraded", "Unhealthy", "Unknown"
    /// </summary>
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// Response time in milliseconds (for health check probe)
    /// </summary>
    public decimal ResponseTimeMs { get; set; }

    /// <summary>
    /// Additional details about component health
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Last successful health check timestamp
    /// </summary>
    public DateTime? LastHealthyAt { get; set; }

    /// <summary>
    /// Component-specific metrics
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Current API performance metrics
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Average response time (last 5 minutes)
    /// </summary>
    public decimal AvgResponseTimeMs { get; set; }

    /// <summary>
    /// P95 response time (last 5 minutes)
    /// </summary>
    public decimal P95ResponseTimeMs { get; set; }

    /// <summary>
    /// P99 response time (last 5 minutes)
    /// </summary>
    public decimal P99ResponseTimeMs { get; set; }

    /// <summary>
    /// Requests per second (last 5 minutes)
    /// </summary>
    public decimal RequestsPerSecond { get; set; }

    /// <summary>
    /// Error rate percentage (last 5 minutes)
    /// </summary>
    public decimal ErrorRatePercent { get; set; }

    /// <summary>
    /// Success rate percentage (last 5 minutes)
    /// </summary>
    public decimal SuccessRatePercent { get; set; }
}

/// <summary>
/// System resource utilization
/// </summary>
public class ResourceUtilization
{
    /// <summary>
    /// CPU usage percentage (0-100)
    /// </summary>
    public decimal CpuPercent { get; set; }

    /// <summary>
    /// Memory usage percentage (0-100)
    /// </summary>
    public decimal MemoryPercent { get; set; }

    /// <summary>
    /// Disk usage percentage (0-100)
    /// </summary>
    public decimal DiskPercent { get; set; }

    /// <summary>
    /// Database connection pool utilization (0-100)
    /// </summary>
    public decimal ConnectionPoolPercent { get; set; }

    /// <summary>
    /// Active database connections
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Maximum database connections allowed
    /// </summary>
    public int MaxConnections { get; set; }
}

/// <summary>
/// Multi-tenant platform statistics
/// </summary>
public class TenantStatistics
{
    /// <summary>
    /// Total number of active tenants
    /// </summary>
    public int TotalActive { get; set; }

    /// <summary>
    /// Number of tenants with active users (last 5 minutes)
    /// </summary>
    public int ActiveTenants { get; set; }

    /// <summary>
    /// Number of suspended tenants
    /// </summary>
    public int Suspended { get; set; }

    /// <summary>
    /// Number of tenants in trial period
    /// </summary>
    public int Trial { get; set; }

    /// <summary>
    /// Total number of employees across all tenants
    /// </summary>
    public int TotalEmployees { get; set; }
}
