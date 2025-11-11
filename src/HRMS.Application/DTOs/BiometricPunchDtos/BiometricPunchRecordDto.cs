namespace HRMS.Application.DTOs.BiometricPunchDtos;

/// <summary>
/// DTO for biometric punch record responses
/// Used when returning punch record data to the API clients
/// </summary>
public class BiometricPunchRecordDto
{
    /// <summary>
    /// Unique identifier for the punch record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Employee ID (if matched)
    /// </summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>
    /// Employee full name (for display)
    /// </summary>
    public string? EmployeeName { get; set; }

    /// <summary>
    /// Employee code (for display)
    /// </summary>
    public string? EmployeeCode { get; set; }

    /// <summary>
    /// Device ID that captured this punch
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Device name (for display)
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Device serial number
    /// </summary>
    public string DeviceSerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// User ID from the biometric device
    /// </summary>
    public string DeviceUserId { get; set; } = string.Empty;

    /// <summary>
    /// When the punch occurred
    /// </summary>
    public DateTime PunchTime { get; set; }

    /// <summary>
    /// Type of punch: "CheckIn", "CheckOut", "Break", "BreakEnd"
    /// </summary>
    public string PunchType { get; set; } = string.Empty;

    /// <summary>
    /// Verification method: "Fingerprint", "Face", "Card", "PIN", "Palm"
    /// </summary>
    public string VerificationMethod { get; set; } = string.Empty;

    /// <summary>
    /// Verification quality score (0-100)
    /// </summary>
    public int VerificationQuality { get; set; }

    /// <summary>
    /// Location latitude (if available)
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Location longitude (if available)
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Processing status: "Pending", "Processed", "Failed", "Duplicate", "Ignored"
    /// </summary>
    public string ProcessingStatus { get; set; } = string.Empty;

    /// <summary>
    /// When the punch was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ProcessingError { get; set; }

    /// <summary>
    /// ID of the attendance record created from this punch
    /// </summary>
    public Guid? AttendanceId { get; set; }

    /// <summary>
    /// When the record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Photo path (if available)
    /// </summary>
    public string? PhotoPath { get; set; }

    /// <summary>
    /// Location name (derived from device)
    /// </summary>
    public string? LocationName { get; set; }
}
