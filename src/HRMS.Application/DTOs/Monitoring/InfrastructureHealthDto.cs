using System;
using System.Collections.Generic;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 50: Infrastructure layer health metrics
/// Monitors database performance, connection pooling, and system resources
/// PATTERN: Read-only monitoring with zero application impact
/// </summary>
public class InfrastructureHealthDto
{
    /// <summary>
    /// PostgreSQL database version
    /// </summary>
    public string DatabaseVersion { get; set; } = string.Empty;

    /// <summary>
    /// Current database uptime in hours
    /// </summary>
    public decimal UptimeHours { get; set; }

    /// <summary>
    /// Current cache hit rate percentage (target: >95%)
    /// </summary>
    public decimal CacheHitRate { get; set; }

    /// <summary>
    /// Cache hit rate status: Excellent (>98%), Good (95-98%), Warning (<95%)
    /// </summary>
    public string CacheHitRateStatus { get; set; } = "Unknown";

    /// <summary>
    /// Total number of connections (active + idle)
    /// </summary>
    public int TotalConnections { get; set; }

    /// <summary>
    /// Number of active connections executing queries
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Number of idle connections in the pool
    /// </summary>
    public int IdleConnections { get; set; }

    /// <summary>
    /// Maximum allowed connections (PostgreSQL limit)
    /// </summary>
    public int MaxConnections { get; set; }

    /// <summary>
    /// Connection pool utilization percentage
    /// </summary>
    public decimal ConnectionUtilization { get; set; }

    /// <summary>
    /// Total database size in bytes
    /// </summary>
    public long DatabaseSizeBytes { get; set; }

    /// <summary>
    /// Human-readable database size (e.g., "2.5 GB")
    /// </summary>
    public string DatabaseSizeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Disk I/O read operations per second
    /// </summary>
    public decimal DiskReadsPerSecond { get; set; }

    /// <summary>
    /// Disk I/O write operations per second
    /// </summary>
    public decimal DiskWritesPerSecond { get; set; }

    /// <summary>
    /// Average query execution time in milliseconds
    /// </summary>
    public decimal AvgQueryTimeMs { get; set; }

    /// <summary>
    /// Number of queries executed in last collection interval
    /// </summary>
    public long QueriesExecuted { get; set; }

    /// <summary>
    /// Number of deadlocks detected (should be 0)
    /// </summary>
    public int Deadlocks { get; set; }

    /// <summary>
    /// List of top 5 slowest queries
    /// </summary>
    public List<SlowQueryDto> TopSlowQueries { get; set; } = new();

    /// <summary>
    /// Last health check timestamp
    /// </summary>
    public DateTime LastChecked { get; set; }

    // System Resource Metrics (Fortune 500 Standard)

    /// <summary>
    /// Database server CPU usage percentage (0-100)
    /// </summary>
    public decimal CpuUsagePercent { get; set; }

    /// <summary>
    /// Database server memory usage percentage (0-100)
    /// </summary>
    public decimal MemoryUsagePercent { get; set; }

    /// <summary>
    /// Database disk usage percentage (0-100)
    /// </summary>
    public decimal DiskUsagePercent { get; set; }

    /// <summary>
    /// Average network latency to database in milliseconds
    /// </summary>
    public decimal NetworkLatencyMs { get; set; }

    /// <summary>
    /// Number of active database connections (currently executing queries)
    /// </summary>
    public int DbConnections { get; set; }
}
