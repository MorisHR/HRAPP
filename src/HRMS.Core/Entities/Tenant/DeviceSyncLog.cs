using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Tracks device synchronization operations and results
/// Critical for monitoring device health and troubleshooting sync issues
/// </summary>
public class DeviceSyncLog : BaseEntity
{
    // Device Reference
    public Guid DeviceId { get; set; }
    public AttendanceMachine Device { get; set; } = null!;

    // Sync Timing
    public DateTime SyncStartTime { get; set; }
    public DateTime? SyncEndTime { get; set; }
    public int? SyncDurationSeconds { get; set; }
    public string SyncStatus { get; set; } = "InProgress";  // "InProgress", "Success", "Failed", "Partial"

    // Sync Results
    public int RecordsFetched { get; set; } = 0;
    public int RecordsProcessed { get; set; } = 0;
    public int RecordsInserted { get; set; } = 0;
    public int RecordsUpdated { get; set; } = 0;
    public int RecordsSkipped { get; set; } = 0;
    public int RecordsErrored { get; set; } = 0;

    // Sync Details
    public string SyncMethod { get; set; } = "Auto";  // "Auto", "Manual", "Scheduled"
    public DateTime? DateRangeFrom { get; set; }
    public DateTime? DateRangeTo { get; set; }

    // Error Information
    public string? ErrorMessage { get; set; }
    public string? ErrorDetailsJson { get; set; }  // JSON with stack trace, device response, etc.

    // Audit
    public Guid? InitiatedBy { get; set; }
}
