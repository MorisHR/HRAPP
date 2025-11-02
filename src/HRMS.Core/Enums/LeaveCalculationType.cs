namespace HRMS.Core.Enums;

/// <summary>
/// How to calculate leave days
/// </summary>
public enum LeaveCalculationType
{
    WorkingDays = 1,          // Exclude weekends and holidays
    CalendarDays = 2,         // Include all days
    HalfDay = 3               // Half-day leave
}
