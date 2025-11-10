using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Anomaly detection service for identifying suspicious patterns and security threats
/// FORTUNE 500 PATTERN: AWS GuardDuty, Splunk Security, Azure Sentinel
/// </summary>
public interface IAnomalyDetectionService
{
    // ============================================
    // ANOMALY DETECTION
    // ============================================

    /// <summary>
    /// Detects anomalies from an audit log entry
    /// </summary>
    /// <param name="auditLog">Audit log to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of detected anomalies</returns>
    Task<List<DetectedAnomaly>> DetectAnomaliesAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs batch anomaly detection on recent audit logs
    /// </summary>
    /// <param name="lookbackMinutes">Number of minutes to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of anomalies detected</returns>
    Task<int> RunBatchDetectionAsync(int lookbackMinutes = 60, CancellationToken cancellationToken = default);

    // ============================================
    // ANOMALY RETRIEVAL
    // ============================================

    /// <summary>
    /// Gets anomalies with filtering and pagination
    /// </summary>
    /// <param name="tenantId">Optional tenant filter</param>
    /// <param name="anomalyType">Optional anomaly type filter</param>
    /// <param name="riskLevel">Optional risk level filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of anomalies and total count</returns>
    Task<(List<DetectedAnomaly> anomalies, int totalCount)> GetAnomaliesAsync(
        Guid? tenantId = null,
        AnomalyType? anomalyType = null,
        AnomalyRiskLevel? riskLevel = null,
        AnomalyStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific anomaly by ID
    /// </summary>
    /// <param name="anomalyId">Anomaly ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Anomaly or null if not found</returns>
    Task<DetectedAnomaly?> GetAnomalyByIdAsync(Guid anomalyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets anomalies for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="daysBack">Number of days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of anomalies</returns>
    Task<List<DetectedAnomaly>> GetUserAnomaliesAsync(Guid userId, int daysBack = 30, CancellationToken cancellationToken = default);

    // ============================================
    // ANOMALY MANAGEMENT
    // ============================================

    /// <summary>
    /// Updates the status of an anomaly
    /// </summary>
    /// <param name="anomalyId">Anomaly ID</param>
    /// <param name="status">New status</param>
    /// <param name="investigatedBy">User who is updating the status</param>
    /// <param name="investigationNotes">Investigation notes</param>
    /// <param name="resolution">Resolution details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated anomaly</returns>
    Task<DetectedAnomaly> UpdateAnomalyStatusAsync(
        Guid anomalyId,
        AnomalyStatus status,
        Guid investigatedBy,
        string? investigationNotes = null,
        string? resolution = null,
        CancellationToken cancellationToken = default);

    // ============================================
    // STATISTICS & ANALYTICS
    // ============================================

    /// <summary>
    /// Gets anomaly statistics for a tenant
    /// </summary>
    /// <param name="tenantId">Optional tenant ID (null for all tenants)</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Statistics object</returns>
    Task<AnomalyStatistics> GetAnomalyStatisticsAsync(
        Guid? tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top users with most anomalies
    /// </summary>
    /// <param name="tenantId">Optional tenant ID</param>
    /// <param name="daysBack">Number of days to analyze</param>
    /// <param name="topN">Number of top users to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user IDs with anomaly counts</returns>
    Task<List<(Guid userId, string? userEmail, int anomalyCount)>> GetTopUsersWithAnomaliesAsync(
        Guid? tenantId,
        int daysBack = 30,
        int topN = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Anomaly statistics DTO
/// </summary>
public class AnomalyStatistics
{
    public int TotalAnomalies { get; set; }
    public int NewAnomalies { get; set; }
    public int InvestigatingAnomalies { get; set; }
    public int ConfirmedThreats { get; set; }
    public int FalsePositives { get; set; }
    public int ResolvedAnomalies { get; set; }
    public Dictionary<AnomalyType, int> AnomaliesByType { get; set; } = new();
    public Dictionary<AnomalyRiskLevel, int> AnomaliesByRiskLevel { get; set; } = new();
    public double AverageRiskScore { get; set; }
    public int CriticalAnomalies { get; set; }
    public int HighRiskAnomalies { get; set; }
}
