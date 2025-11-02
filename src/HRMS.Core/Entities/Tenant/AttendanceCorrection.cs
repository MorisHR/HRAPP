using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Attendance correction requests (manager approval required)
/// </summary>
public class AttendanceCorrection : BaseEntity
{
    public Guid AttendanceId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid RequestedBy { get; set; }

    public DateTime? OriginalCheckIn { get; set; }
    public DateTime? OriginalCheckOut { get; set; }

    public DateTime? CorrectedCheckIn { get; set; }
    public DateTime? CorrectedCheckOut { get; set; }

    public string Reason { get; set; } = string.Empty;
    public AttendanceCorrectionStatus Status { get; set; }

    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation
    public virtual Attendance? Attendance { get; set; }
    public virtual Employee? Employee { get; set; }
}
