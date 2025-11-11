namespace HRMS.Application.DTOs.SignalRDtos;

/// <summary>
/// DTO for real-time device status updates via SignalR
/// Used to notify clients when biometric devices go online/offline
/// </summary>
public class DeviceStatusDto
{
    /// <summary>
    /// Device unique identifier
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Device name for display
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Device serial number
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Device status: "Online", "Offline", "Maintenance", "Error"
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Location name where device is installed
    /// </summary>
    public string? LocationName { get; set; }

    /// <summary>
    /// When the status changed
    /// </summary>
    public DateTime StatusChangedAt { get; set; }

    /// <summary>
    /// Last successful sync time
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// Number of records synced in last sync
    /// </summary>
    public int LastSyncRecordCount { get; set; }

    /// <summary>
    /// Last sync status: "Success", "Failed", "Partial"
    /// </summary>
    public string? LastSyncStatus { get; set; }

    /// <summary>
    /// Error message if device is in error state
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// IP address of the device
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Firmware version of the device
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Is the device currently active
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for anomaly detection alerts via SignalR
/// Used to notify clients of suspicious attendance patterns or security issues
/// </summary>
public class AnomalyAlertDto
{
    /// <summary>
    /// Unique identifier for this anomaly
    /// </summary>
    public Guid AnomalyId { get; set; }

    /// <summary>
    /// Type of anomaly: "DuplicatePunch", "ImpossibleTravel", "UnusualPattern", "DeviceManipulation"
    /// </summary>
    public string AnomalyType { get; set; } = string.Empty;

    /// <summary>
    /// Severity: "Low", "Medium", "High", "Critical"
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Employee ID involved (if applicable)
    /// </summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>
    /// Employee name for display
    /// </summary>
    public string? EmployeeName { get; set; }

    /// <summary>
    /// Device ID involved (if applicable)
    /// </summary>
    public Guid? DeviceId { get; set; }

    /// <summary>
    /// Device name for display
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Description of the anomaly
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Detailed explanation
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// When the anomaly was detected
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// Related punch record ID (if applicable)
    /// </summary>
    public Guid? PunchRecordId { get; set; }

    /// <summary>
    /// Risk score (0-100)
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Recommended action
    /// </summary>
    public string? RecommendedAction { get; set; }
}
