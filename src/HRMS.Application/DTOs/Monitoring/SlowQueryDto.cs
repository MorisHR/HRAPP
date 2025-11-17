using System;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 50: Slow query analysis for database performance optimization
/// Identifies problematic queries requiring optimization or indexing
/// PERFORMANCE TARGET: Query execution time should be <50ms for 95% of queries
/// </summary>
public class SlowQueryDto
{
    /// <summary>
    /// Unique query identifier (hash of normalized query text)
    /// </summary>
    public string QueryId { get; set; } = string.Empty;

    /// <summary>
    /// Normalized SQL query text (placeholders for parameters)
    /// Example: "SELECT * FROM employees WHERE tenant_id = $1 AND department = $2"
    /// </summary>
    public string QueryText { get; set; } = string.Empty;

    /// <summary>
    /// Tenant subdomain (if query is tenant-scoped), NULL for cross-tenant queries
    /// </summary>
    public string? TenantSubdomain { get; set; }

    /// <summary>
    /// Database schema where query executes (tenant schema or master)
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// Total number of times this query has been executed
    /// </summary>
    public long ExecutionCount { get; set; }

    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public decimal AvgExecutionTimeMs { get; set; }

    /// <summary>
    /// Minimum execution time observed (ms)
    /// </summary>
    public decimal MinExecutionTimeMs { get; set; }

    /// <summary>
    /// Maximum execution time observed (ms)
    /// </summary>
    public decimal MaxExecutionTimeMs { get; set; }

    /// <summary>
    /// P95 execution time in milliseconds
    /// </summary>
    public decimal P95ExecutionTimeMs { get; set; }

    /// <summary>
    /// Total cumulative execution time for all executions (ms)
    /// Used to identify queries consuming most database time
    /// </summary>
    public decimal TotalExecutionTimeMs { get; set; }

    /// <summary>
    /// Average number of rows returned by this query
    /// </summary>
    public decimal? AvgRowsReturned { get; set; }

    /// <summary>
    /// Average number of rows scanned (indicates missing indexes if >> rows returned)
    /// </summary>
    public decimal? AvgRowsScanned { get; set; }

    /// <summary>
    /// Cache hit percentage for this query (0-100)
    /// Low values indicate query is not benefiting from caching
    /// </summary>
    public decimal? CacheHitRate { get; set; }

    /// <summary>
    /// Whether this query performs sequential scans (indicates missing indexes)
    /// </summary>
    public bool HasSequentialScan { get; set; }

    /// <summary>
    /// Number of tables joined in this query
    /// </summary>
    public int? JoinCount { get; set; }

    /// <summary>
    /// Query plan explanation (EXPLAIN output for optimization)
    /// </summary>
    public string? ExecutionPlan { get; set; }

    /// <summary>
    /// Optimization recommendations (e.g., "Add index on employees(department, hire_date)")
    /// </summary>
    public string? OptimizationSuggestion { get; set; }

    /// <summary>
    /// Severity: Critical (>1000ms), High (500-1000ms), Medium (200-500ms), Low (<200ms)
    /// </summary>
    public string Severity { get; set; } = "Low";

    /// <summary>
    /// When this query was first detected as slow
    /// </summary>
    public DateTime FirstDetected { get; set; }

    /// <summary>
    /// When this query was last executed
    /// </summary>
    public DateTime LastExecuted { get; set; }

    /// <summary>
    /// Whether this slow query has been reviewed by DBA/DevOps
    /// </summary>
    public bool IsReviewed { get; set; }

    /// <summary>
    /// Review notes and optimization actions taken
    /// </summary>
    public string? ReviewNotes { get; set; }
}
