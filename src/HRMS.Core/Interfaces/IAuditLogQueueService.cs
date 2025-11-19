using HRMS.Core.Entities.Master;

namespace HRMS.Core.Interfaces;

/// <summary>
/// Service for queuing audit logs for async processing
/// Prevents ThreadPool exhaustion from fire-and-forget Task.Run
/// </summary>
public interface IAuditLogQueueService
{
    /// <summary>
    /// Queue an audit log for background processing
    /// </summary>
    /// <param name="auditLog">The audit log to queue</param>
    /// <returns>True if queued successfully, false otherwise</returns>
    ValueTask<bool> QueueAuditLogAsync(AuditLog auditLog);
}
