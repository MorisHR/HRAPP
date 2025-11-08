namespace HRMS.Application.DTOs.BiometricDeviceDtos;

/// <summary>
/// Complete biometric device information for detail view
/// </summary>
public class BiometricDeviceDto
{
    public Guid Id { get; set; }

    // Device Identity
    public string DeviceCode { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;

    // Device Type & Model
    public string DeviceType { get; set; } = "ZKTeco";
    public string? Model { get; set; }

    // Location Assignment
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? LocationCode { get; set; }

    // Legacy fields
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? LegacyLocation { get; set; }

    // Network Configuration
    public string? IpAddress { get; set; }
    public int Port { get; set; } = 4370;
    public string? MacAddress { get; set; }

    // Device Identification
    public string? SerialNumber { get; set; }
    public string? FirmwareVersion { get; set; }

    // Sync Configuration
    public bool SyncEnabled { get; set; } = true;
    public int SyncIntervalMinutes { get; set; } = 15;
    public DateTime? LastSyncTime { get; set; }
    public string? LastSyncStatus { get; set; }
    public int LastSyncRecordCount { get; set; }

    // Connection Settings
    public string ConnectionMethod { get; set; } = "TCP/IP";
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    // Device Configuration
    public string? DeviceConfigJson { get; set; }

    // Device Status
    public string DeviceStatus { get; set; } = "Active";
    public bool IsActive { get; set; } = true;

    // Alert Configuration
    public bool OfflineAlertEnabled { get; set; } = true;
    public int OfflineThresholdMinutes { get; set; } = 60;

    // Legacy ZKTeco fields
    public string? ZKTecoDeviceId { get; set; }
    public DateTime? LastSyncAt { get; set; }

    // Statistics
    public int TotalAttendanceRecords { get; set; }
    public int AuthorizedEmployeeCount { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
