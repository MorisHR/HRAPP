namespace HRMS.DeviceSync.Models;

/// <summary>
/// Configuration for a single biometric device
/// </summary>
public class DeviceConfiguration
{
    /// <summary>
    /// Unique device code (matches HRMS database)
    /// </summary>
    public string DeviceCode { get; set; } = string.Empty;

    /// <summary>
    /// Device IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Device communication port (default: 4370 for ZKTeco)
    /// </summary>
    public int Port { get; set; } = 4370;

    /// <summary>
    /// Device communication password (if set on device)
    /// </summary>
    public int CommPassword { get; set; } = 0;

    /// <summary>
    /// Device type (ZKTeco, Anviz, etc.)
    /// </summary>
    public string DeviceType { get; set; } = "ZKTeco";

    /// <summary>
    /// Enabled for syncing
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Sync service configuration
/// </summary>
public class SyncServiceConfiguration
{
    /// <summary>
    /// API base URL (e.g., https://api.morishr.com)
    /// </summary>
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Sync interval in minutes
    /// </summary>
    public int SyncIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum records to fetch per sync
    /// </summary>
    public int MaxRecordsPerSync { get; set; } = 1000;

    /// <summary>
    /// List of devices to sync
    /// </summary>
    public List<DeviceConfiguration> Devices { get; set; } = new();
}

/// <summary>
/// Attendance record from device
/// </summary>
public class DeviceAttendanceRecord
{
    public string EnrollNumber { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime PunchTime { get; set; }
    public int VerifyMode { get; set; } // 0=Password, 1=Fingerprint, 15=Face
    public int InOutMode { get; set; } // 0=CheckIn, 1=CheckOut, 2=BreakOut, 3=BreakIn
    public int WorkCode { get; set; }
}
