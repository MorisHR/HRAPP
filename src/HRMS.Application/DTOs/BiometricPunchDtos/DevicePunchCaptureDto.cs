using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.BiometricPunchDtos;

/// <summary>
/// DTO for capturing biometric punch data from devices
/// Used when devices submit punch events to the API
/// </summary>
public class DevicePunchCaptureDto
{
    /// <summary>
    /// Serial number of the device submitting the punch
    /// Used to identify and authenticate the device
    /// </summary>
    [Required(ErrorMessage = "Device serial number is required")]
    [MaxLength(100, ErrorMessage = "Device serial number cannot exceed 100 characters")]
    public string DeviceSerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// User ID from the biometric device
    /// This is the ID stored in the device, not the employee ID
    /// </summary>
    [Required(ErrorMessage = "Device user ID is required")]
    [MaxLength(100, ErrorMessage = "Device user ID cannot exceed 100 characters")]
    public string DeviceUserId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the punch occurred on the device
    /// </summary>
    [Required(ErrorMessage = "Punch time is required")]
    public DateTime PunchTime { get; set; }

    /// <summary>
    /// Type of punch: "CheckIn", "CheckOut", "Break", "BreakEnd"
    /// </summary>
    [Required(ErrorMessage = "Punch type is required")]
    [MaxLength(50, ErrorMessage = "Punch type cannot exceed 50 characters")]
    public string PunchType { get; set; } = string.Empty;

    /// <summary>
    /// Verification method used: "Fingerprint", "Face", "Card", "PIN", "Palm"
    /// </summary>
    [Required(ErrorMessage = "Verification method is required")]
    [MaxLength(50, ErrorMessage = "Verification method cannot exceed 50 characters")]
    public string VerificationMethod { get; set; } = string.Empty;

    /// <summary>
    /// Quality score of the biometric verification (0-100)
    /// Higher values indicate better match confidence
    /// </summary>
    [Required(ErrorMessage = "Verification quality is required")]
    [Range(0, 100, ErrorMessage = "Verification quality must be between 0 and 100")]
    public int VerificationQuality { get; set; }

    /// <summary>
    /// GPS latitude of punch location (optional)
    /// </summary>
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public decimal? Latitude { get; set; }

    /// <summary>
    /// GPS longitude of punch location (optional)
    /// </summary>
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Base64-encoded photo from device (optional)
    /// For face recognition devices or photo capture
    /// </summary>
    public string? PhotoBase64 { get; set; }

    /// <summary>
    /// Raw JSON data from device (optional)
    /// Used for debugging and audit purposes
    /// </summary>
    public string? RawData { get; set; }
}
