namespace HRMS.Application.DTOs.BiometricDeviceDtos;

/// <summary>
/// Result of a manual device sync trigger
/// </summary>
public class ManualSyncResultDto
{
    /// <summary>
    /// Indicates if the sync job was successfully queued
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable message about the sync trigger result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Hangfire job ID for tracking the sync operation
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Device ID being synced
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Device name for reference
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the sync job was queued
    /// </summary>
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Estimated time for sync to complete (seconds)
    /// </summary>
    public int? EstimatedDurationSeconds { get; set; }

    /// <summary>
    /// Error message if sync could not be triggered
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Indicates if a sync is already in progress for this device
    /// </summary>
    public bool SyncAlreadyInProgress { get; set; }
}
