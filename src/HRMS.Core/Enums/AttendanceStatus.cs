namespace HRMS.Core.Enums;

/// <summary>
/// Attendance status for daily records
/// </summary>
public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    HalfDay = 4,
    OnLeave = 5,
    Weekend = 6,
    PublicHoliday = 7,
    Unpaid = 8,
    EarlyDeparture = 9
}
