using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Daily attendance record for employees
/// Links to sector compliance rules for overtime calculation
/// </summary>
public class Attendance : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }

    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }

    public decimal WorkingHours { get; set; }
    public decimal OvertimeHours { get; set; }

    public AttendanceStatus Status { get; set; }

    public int? LateArrivalMinutes { get; set; }
    public int? EarlyDepartureMinutes { get; set; }

    public string? Remarks { get; set; }

    public bool IsRegularized { get; set; }
    public Guid? RegularizedBy { get; set; }
    public DateTime? RegularizedAt { get; set; }

    public Guid? ShiftId { get; set; }
    public Guid? AttendanceMachineId { get; set; }

    // ==========================================
    // MULTI-DEVICE / MULTI-LOCATION TRACKING
    // ==========================================

    /// <summary>
    /// Which biometric device was used for this attendance
    /// CRITICAL for multi-device tracking and authorization validation
    /// </summary>
    public Guid? DeviceId { get; set; }
    public AttendanceMachine? Device { get; set; }

    /// <summary>
    /// Physical location where attendance was recorded (derived from device)
    /// CRITICAL for location-based reporting and fraud detection
    /// </summary>
    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    /// <summary>
    /// How was this attendance recorded?
    /// "Biometric", "Manual", "Web", "Mobile", "API"
    /// </summary>
    public string PunchSource { get; set; } = "Biometric";

    /// <summary>
    /// Verification method used: "Fingerprint", "Face", "Card", "PIN", "Manual"
    /// </summary>
    public string? VerificationMethod { get; set; }

    /// <summary>
    /// User ID from the biometric device (may differ from employee code)
    /// </summary>
    public string? DeviceUserId { get; set; }

    /// <summary>
    /// Was the employee authorized to use this device/location?
    /// FALSE = Anomaly detected (wrong location, unauthorized access)
    /// </summary>
    public bool IsAuthorized { get; set; } = true;

    /// <summary>
    /// Note explaining authorization status or anomaly
    /// </summary>
    public string? AuthorizationNote { get; set; }

    // Sector-specific calculations
    public decimal? OvertimeRate { get; set; } // Multiplier from sector rules (1.5x, 2x, 3x)
    public bool IsSunday { get; set; }
    public bool IsPublicHoliday { get; set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public virtual Employee? Employee { get; set; }
    public ICollection<AttendanceAnomaly> Anomalies { get; set; } = new List<AttendanceAnomaly>();
}
