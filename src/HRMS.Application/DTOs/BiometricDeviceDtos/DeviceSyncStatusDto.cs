namespace HRMS.Application.DTOs.BiometricDeviceDtos;

/// <summary>
/// Device sync status information for monitoring dashboard
/// </summary>
public class DeviceSyncStatusDto
{
    public Guid DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string? LocationName { get; set; }

    // Sync Status
    public bool SyncEnabled { get; set; }
    public int SyncIntervalMinutes { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public string? LastSyncStatus { get; set; }
    public int LastSyncRecordCount { get; set; }
    public int? MinutesSinceLastSync { get; set; }

    // Device Health
    public string DeviceStatus { get; set; } = "Active";
    public bool IsOnline { get; set; }
    public bool IsOfflineAlertTriggered { get; set; }

    // Latest Sync Log
    public Guid? LatestSyncLogId { get; set; }
    public DateTime? LatestSyncStartTime { get; set; }
    public DateTime? LatestSyncEndTime { get; set; }
    public int? LatestSyncDurationSeconds { get; set; }
    public string? LatestSyncError { get; set; }

    // Statistics
    public int TotalSyncCount { get; set; }
    public int SuccessfulSyncCount { get; set; }
    public int FailedSyncCount { get; set; }
    public decimal SyncSuccessRate { get; set; }
}
