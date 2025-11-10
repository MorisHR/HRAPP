using HRMS.Core.Entities.Master;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Audit correlation service for advanced activity analysis
/// FORTUNE 500 PATTERN: Splunk correlation searches, Elasticsearch aggregations
/// </summary>
public interface IAuditCorrelationService
{
    /// <summary>
    /// Correlates user activity patterns across time ranges
    /// </summary>
    Task<Dictionary<string, object>> CorrelateUserActivityAsync(
        Guid userId,
        TimeSpan timeRange,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds comprehensive activity timeline for a user
    /// </summary>
    Task<List<ActivityTimelineEntry>> BuildActivityTimelineAsync(
        Guid userId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects patterns across multiple users (insider threat detection)
    /// </summary>
    Task<List<CorrelatedPattern>> DetectPatternsAcrossUsersAsync(
        Guid? tenantId = null,
        int daysBack = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds related audit logs for an incident investigation
    /// </summary>
    Task<List<AuditLog>> FindRelatedAuditLogsAsync(
        Guid auditLogId,
        int correlationDepth = 2,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Activity timeline entry
/// </summary>
public class ActivityTimelineEntry
{
    public DateTime Timestamp { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Location { get; set; }
    public bool Success { get; set; }
    public Guid AuditLogId { get; set; }
}

/// <summary>
/// Correlated pattern detection result
/// </summary>
public class CorrelatedPattern
{
    public string PatternType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Guid> InvolvedUserIds { get; set; } = new();
    public List<Guid> RelatedAuditLogIds { get; set; } = new();
    public int Severity { get; set; }
    public DateTime DetectedAt { get; set; }
}
