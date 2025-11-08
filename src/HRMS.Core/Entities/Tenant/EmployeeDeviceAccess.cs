using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Tracks explicit device access grants for employees beyond their primary location
/// Used for field staff, contractors, multi-location workers
/// </summary>
public class EmployeeDeviceAccess : BaseEntity
{
    // Employee and Device
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public Guid DeviceId { get; set; }
    public AttendanceMachine Device { get; set; } = null!;

    // Access Details
    public string AccessType { get; set; } = "Secondary";  // "Primary", "Secondary", "Temporary"
    public string? AccessReason { get; set; }  // "Field technician - visits all sites"

    // Validity Period
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }  // NULL = permanent

    // Time Restrictions (JSON for flexibility)
    public string? AllowedDaysJson { get; set; }  // ["Monday", "Tuesday", "Wednesday"]
    public TimeSpan? AllowedTimeStart { get; set; }  // 08:00:00
    public TimeSpan? AllowedTimeEnd { get; set; }    // 17:00:00

    // Status
    public bool IsActive { get; set; } = true;

    // Approval
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
}
