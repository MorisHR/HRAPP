using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.BiometricDeviceDtos;

/// <summary>
/// DTO for creating a new biometric device
/// </summary>
public class CreateBiometricDeviceDto
{
    [Required]
    [StringLength(50)]
    public string DeviceCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string MachineName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string MachineId { get; set; } = string.Empty;

    [StringLength(100)]
    public string DeviceType { get; set; } = "ZKTeco";

    [StringLength(100)]
    public string? Model { get; set; }

    // Location Assignment
    [Required]
    public Guid LocationId { get; set; }

    public Guid? DepartmentId { get; set; }

    // Network Configuration
    [StringLength(50)]
    public string? IpAddress { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; } = 4370;

    [StringLength(50)]
    public string? MacAddress { get; set; }

    // Device Identification
    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [StringLength(50)]
    public string? FirmwareVersion { get; set; }

    // Sync Configuration
    public bool SyncEnabled { get; set; } = true;

    [Range(5, 1440)]
    public int SyncIntervalMinutes { get; set; } = 15;

    // Connection Settings
    [StringLength(50)]
    public string ConnectionMethod { get; set; } = "TCP/IP";

    [Range(10, 300)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    // Device Configuration
    public string? DeviceConfigJson { get; set; }

    [StringLength(50)]
    public string DeviceStatus { get; set; } = "Active";

    public bool IsActive { get; set; } = true;

    // Alert Configuration
    public bool OfflineAlertEnabled { get; set; } = true;

    [Range(10, 1440)]
    public int OfflineThresholdMinutes { get; set; } = 60;

    // Legacy ZKTeco fields
    [StringLength(50)]
    public string? ZKTecoDeviceId { get; set; }
}
