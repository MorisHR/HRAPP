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

    // Sector-specific calculations
    public decimal? OvertimeRate { get; set; } // Multiplier from sector rules (1.5x, 2x, 3x)
    public bool IsSunday { get; set; }
    public bool IsPublicHoliday { get; set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public virtual Employee? Employee { get; set; }
}
