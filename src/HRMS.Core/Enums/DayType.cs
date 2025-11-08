namespace HRMS.Core.Enums;

/// <summary>
/// Type of day in timesheet entry
/// </summary>
public enum DayType
{
    /// <summary>
    /// Regular working day
    /// </summary>
    Regular = 1,

    /// <summary>
    /// Public holiday
    /// </summary>
    Holiday = 2,

    /// <summary>
    /// Weekend (Saturday/Sunday)
    /// </summary>
    Weekend = 3,

    /// <summary>
    /// Annual leave
    /// </summary>
    AnnualLeave = 4,

    /// <summary>
    /// Sick leave
    /// </summary>
    SickLeave = 5,

    /// <summary>
    /// Casual leave
    /// </summary>
    CasualLeave = 6,

    /// <summary>
    /// Unpaid leave
    /// </summary>
    UnpaidLeave = 7,

    /// <summary>
    /// Absent without leave
    /// </summary>
    Absent = 8,

    /// <summary>
    /// Remote work day
    /// </summary>
    RemoteWork = 9
}
