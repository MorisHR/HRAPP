namespace HRMS.Application.DTOs.DeviceWebhookDtos;

/// <summary>
/// DTO for attendance data pushed from biometric devices
/// Compatible with ZKTeco and similar device webhook formats
/// </summary>
public class DevicePushAttendanceDto
{
    /// <summary>
    /// Device identifier (machine ID or serial number)
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// API key for device authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Batch of attendance records
    /// </summary>
    public List<AttendanceRecordDto> Records { get; set; } = new();

    /// <summary>
    /// Device timestamp when data was sent
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Individual attendance record from device
/// </summary>
public class AttendanceRecordDto
{
    /// <summary>
    /// Employee ID/Badge number from device
    /// </summary>
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// Punch timestamp from device
    /// </summary>
    public DateTime PunchTime { get; set; }

    /// <summary>
    /// Punch type: 0=Check In, 1=Check Out, 2=Break Start, 3=Break End, 4=Overtime In, 5=Overtime Out
    /// </summary>
    public int PunchType { get; set; }

    /// <summary>
    /// Verification method: 0=Password, 1=Fingerprint, 2=Card, 3=Face, 15=Palm
    /// </summary>
    public int VerifyMode { get; set; }

    /// <summary>
    /// Device-specific work code (optional)
    /// </summary>
    public string? WorkCode { get; set; }

    /// <summary>
    /// Device record ID (for deduplication)
    /// </summary>
    public string? DeviceRecordId { get; set; }
}

/// <summary>
/// Response DTO for webhook acknowledgment
/// </summary>
public class DeviceWebhookResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RecordsProcessed { get; set; }
    public int RecordsSkipped { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// DTO for device heartbeat/status updates
/// </summary>
public class DeviceHeartbeatDto
{
    /// <summary>
    /// Device identifier
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// API key for device authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Device status information
    /// </summary>
    public DeviceStatusInfo Status { get; set; } = new();

    /// <summary>
    /// Heartbeat timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }
}

public class DeviceStatusInfo
{
    public bool IsOnline { get; set; }
    public int UserCount { get; set; }
    public int RecordCount { get; set; }
    public int FingerCount { get; set; }
    public int FaceCount { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? DeviceModel { get; set; }
    public int FreeSpace { get; set; }
}
