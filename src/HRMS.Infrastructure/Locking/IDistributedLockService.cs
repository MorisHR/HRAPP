namespace HRMS.Infrastructure.Locking;

/// <summary>
/// Distributed locking service for preventing concurrent operations
/// Uses distributed cache (Redis/Memory) for cross-server locking
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Acquire a distributed lock
    /// </summary>
    /// <param name="key">Lock key (e.g., "timesheet-generation")</param>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="timeout">Lock timeout duration</param>
    /// <returns>Disposable lock handle, or null if lock cannot be acquired</returns>
    Task<IDisposable?> AcquireLockAsync(string key, Guid tenantId, TimeSpan timeout);
}
