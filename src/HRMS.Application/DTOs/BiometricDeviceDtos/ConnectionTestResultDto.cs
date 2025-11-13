namespace HRMS.Application.DTOs.BiometricDeviceDtos;

/// <summary>
/// Result of a biometric device connection test
/// </summary>
public class ConnectionTestResultDto
{
    /// <summary>
    /// Indicates if the connection was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable message about the connection test result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Time taken to establish connection (milliseconds)
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Device information retrieved during connection (if available)
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Firmware version detected (if available)
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Number of records available on device (if retrievable)
    /// </summary>
    public int? RecordsAvailable { get; set; }

    /// <summary>
    /// Detailed error message if connection failed
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Technical diagnostic information for troubleshooting
    /// </summary>
    public string? Diagnostics { get; set; }

    /// <summary>
    /// Timestamp of the connection test
    /// </summary>
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}
