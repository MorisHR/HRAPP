using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Biometric attendance devices (ZKTeco, fingerprint, face recognition machines)
/// Tracks physical devices at locations for multi-device attendance management
/// </summary>
public class AttendanceMachine : BaseEntity
{
    // Device Identity
    public string DeviceCode { get; set; } = string.Empty;  // Unique code like "FAC-DEV1"
    public string MachineName { get; set; } = string.Empty;  // Display name like "Factory Main Entrance"
    public string MachineId { get; set; } = string.Empty;   // Legacy field, kept for compatibility

    // Device Type & Model
    public string DeviceType { get; set; } = "ZKTeco";  // "ZKTeco", "Fingerprint", "Face Recognition", "Card Reader"
    public string? Model { get; set; }  // "ZKTeco K40", "ZKTeco F18"

    // Location Assignment (CRITICAL for multi-location support)
    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    public Guid? DepartmentId { get; set; }
    public string? LegacyLocation { get; set; }  // Legacy string field, kept for compatibility

    // Network Configuration
    public string? IpAddress { get; set; }
    public int Port { get; set; } = 4370;  // ZKTeco default port
    public string? MacAddress { get; set; }

    // Device Identification
    public string? SerialNumber { get; set; }
    public string? FirmwareVersion { get; set; }

    // Sync Configuration
    public bool SyncEnabled { get; set; } = true;
    public int SyncIntervalMinutes { get; set; } = 15;  // Auto-sync every 15 minutes
    public DateTime? LastSyncTime { get; set; }
    public string? LastSyncStatus { get; set; }  // "Success", "Failed", "Partial"
    public int LastSyncRecordCount { get; set; } = 0;

    // Connection Settings
    public string ConnectionMethod { get; set; } = "TCP/IP";  // "TCP/IP", "USB", "Serial"
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    // API/SDK Configuration (JSON for flexibility)
    public string? DeviceConfigJson { get; set; }  // {"username": "admin", "sdk_version": "6.5"}

    // Device Status
    public string DeviceStatus { get; set; } = "Active";  // "Active", "Offline", "Maintenance", "Disabled"
    public bool IsActive { get; set; } = true;

    // Alert Configuration
    public bool OfflineAlertEnabled { get; set; } = true;
    public int OfflineThresholdMinutes { get; set; } = 60;  // Alert if no sync for 60 mins

    // Legacy ZKTeco fields
    public string? ZKTecoDeviceId { get; set; }
    public DateTime? LastSyncAt { get; set; }  // Alias for LastSyncTime

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation Properties
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<EmployeeDeviceAccess> EmployeeDeviceAccesses { get; set; } = new List<EmployeeDeviceAccess>();
    public ICollection<DeviceSyncLog> SyncLogs { get; set; } = new List<DeviceSyncLog>();
}
