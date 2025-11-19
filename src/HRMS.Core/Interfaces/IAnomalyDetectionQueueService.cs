using HRMS.Core.Entities.Master;

namespace HRMS.Core.Interfaces;

/// <summary>
/// Service for queuing anomaly detection checks for async processing
/// Prevents ThreadPool exhaustion from fire-and-forget Task.Run
/// Ensures anomaly detection doesn't block audit logging pipeline
/// </summary>
public interface IAnomalyDetectionQueueService
{
    /// <summary>
    /// Queue an anomaly detection check for background processing
    /// </summary>
    /// <param name="auditLog">The audit log to analyze for anomalies</param>
    /// <returns>True if queued successfully, false otherwise</returns>
    ValueTask<bool> QueueAnomalyDetectionCheckAsync(AuditLog auditLog);
}
