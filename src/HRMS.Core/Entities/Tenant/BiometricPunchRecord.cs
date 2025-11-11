using HRMS.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Raw biometric punch records from attendance devices
/// Stores all punch events before processing into attendance records
/// Provides complete audit trail with tamper-proof hash chain
/// </summary>
public class BiometricPunchRecord : BaseEntity
{
    // ==========================================
    // TENANT & DEVICE IDENTIFICATION
    // ==========================================

    /// <summary>
    /// Tenant ID for multi-tenancy isolation
    /// Set automatically by TenantInterceptor
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Reference to the biometric device that captured this punch
    /// CRITICAL for device tracking and location validation
    /// </summary>
    [Required]
    public Guid DeviceId { get; set; }

    /// <summary>
    /// User ID from the biometric device (not employee ID)
    /// Used to match device punch to employee records
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DeviceUserId { get; set; } = string.Empty;

    /// <summary>
    /// Serial number of the device (for validation and audit)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DeviceSerialNumber { get; set; } = string.Empty;

    // ==========================================
    // EMPLOYEE MAPPING
    // ==========================================

    /// <summary>
    /// Mapped employee ID (null if not yet matched)
    /// Populated during processing based on DeviceUserId
    /// </summary>
    public Guid? EmployeeId { get; set; }

    // ==========================================
    // PUNCH DETAILS
    // ==========================================

    /// <summary>
    /// Timestamp when the punch was recorded on the device
    /// CRITICAL for attendance calculation
    /// </summary>
    [Required]
    public DateTime PunchTime { get; set; }

    /// <summary>
    /// Type of punch: "CheckIn", "CheckOut", "Break", "BreakEnd"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string PunchType { get; set; } = string.Empty;

    /// <summary>
    /// How was identity verified: "Fingerprint", "Face", "Card", "PIN", "Palm"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string VerificationMethod { get; set; } = string.Empty;

    /// <summary>
    /// Quality score of biometric verification (0-100)
    /// Higher score indicates better match confidence
    /// </summary>
    public int VerificationQuality { get; set; }

    // ==========================================
    // LOCATION & PHOTO CAPTURE
    // ==========================================

    /// <summary>
    /// GPS latitude of punch location (if available)
    /// Precision: 10,8 = ±1mm accuracy
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// GPS longitude of punch location (if available)
    /// Precision: 11,8 = ±1mm accuracy
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Path to stored photo (for face recognition devices)
    /// </summary>
    [MaxLength(500)]
    public string? PhotoPath { get; set; }

    // ==========================================
    // RAW DATA & PROCESSING
    // ==========================================

    /// <summary>
    /// Raw JSON data from device (for audit and debugging)
    /// Stored as JSONB in PostgreSQL for efficient querying
    /// </summary>
    public string? RawData { get; set; }

    /// <summary>
    /// Processing status: "Pending", "Processed", "Failed", "Duplicate", "Ignored"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ProcessingStatus { get; set; } = "Pending";

    /// <summary>
    /// When was this punch processed into an attendance record
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    [MaxLength(2000)]
    public string? ProcessingError { get; set; }

    /// <summary>
    /// Reference to the attendance record created from this punch
    /// </summary>
    public Guid? AttendanceId { get; set; }

    // ==========================================
    // BLOCKCHAIN-STYLE AUDIT TRAIL
    // ==========================================

    /// <summary>
    /// Tamper-proof hash chain for audit integrity
    /// SHA256(DeviceId + DeviceUserId + PunchTime + PreviousHash)
    /// Ensures punch records cannot be modified without detection
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string HashChain { get; set; } = string.Empty;

    // ==========================================
    // NAVIGATION PROPERTIES
    // ==========================================

    /// <summary>
    /// Navigation to the biometric device
    /// </summary>
    public virtual AttendanceMachine? Device { get; set; }

    /// <summary>
    /// Navigation to the employee (if matched)
    /// </summary>
    public virtual Employee? Employee { get; set; }

    /// <summary>
    /// Navigation to the generated attendance record (if processed)
    /// </summary>
    public virtual Attendance? Attendance { get; set; }

    // Note: Tenant navigation property not needed as TenantId is managed by interceptor
}
