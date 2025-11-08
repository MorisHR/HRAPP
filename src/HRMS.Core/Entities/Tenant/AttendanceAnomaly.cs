using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Tracks attendance anomalies and violations
/// Examples: Unauthorized location access, impossible travel, double punches, etc.
/// </summary>
public class AttendanceAnomaly : BaseEntity
{
    // Employee Reference
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // Related Punch (if applicable)
    public Guid? AttendanceId { get; set; }
    public Attendance? Attendance { get; set; }

    // Anomaly Details
    public string AnomalyType { get; set; } = string.Empty;  // "UnauthorizedLocation", "ImpossibleTravel", "DoublePunch"
    public string AnomalySeverity { get; set; } = "Warning";  // "Critical", "Warning", "Info"
    public DateTime AnomalyDate { get; set; }
    public DateTime AnomalyTime { get; set; }

    // Description
    public string? AnomalyDescription { get; set; }
    public string? AnomalyDetailsJson { get; set; }  // Additional context as JSON

    // Related Entities
    public Guid? DeviceId { get; set; }
    public AttendanceMachine? Device { get; set; }

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    public Guid? ExpectedLocationId { get; set; }  // Where they should have been
    public Location? ExpectedLocation { get; set; }

    // Resolution
    public string ResolutionStatus { get; set; } = "Pending";  // "Pending", "Approved", "Rejected", "Fixed", "Ignored"
    public DateTime? ResolutionDate { get; set; }
    public string? ResolutionNote { get; set; }
    public Guid? ResolvedBy { get; set; }

    // Notifications
    public bool NotificationSent { get; set; } = false;
    public DateTime? NotificationSentAt { get; set; }
    public string? NotificationRecipientsJson { get; set; }  // [{"type": "Manager", "user_id": "..."}]

    // Auto-Resolution
    public bool AutoResolved { get; set; } = false;
    public string? AutoResolutionRule { get; set; }
}
