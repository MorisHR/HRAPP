using HRMS.Core.Entities.Master;

namespace HRMS.Core.Interfaces;

/// <summary>
/// Service for queuing security alerts for async processing
/// Prevents ThreadPool exhaustion from fire-and-forget Task.Run
/// Ensures security alerts are reliably processed even during high load
/// </summary>
public interface ISecurityAlertQueueService
{
    /// <summary>
    /// Queue a security alert check for background processing
    /// </summary>
    /// <param name="auditLog">The audit log to analyze for security threats</param>
    /// <returns>True if queued successfully, false otherwise</returns>
    ValueTask<bool> QueueSecurityAlertCheckAsync(AuditLog auditLog);
}
