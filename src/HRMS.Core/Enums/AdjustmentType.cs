namespace HRMS.Core.Enums;

/// <summary>
/// Type of timesheet adjustment
/// </summary>
public enum AdjustmentType
{
    /// <summary>
    /// Adjust clock in time
    /// </summary>
    ClockIn = 1,

    /// <summary>
    /// Adjust clock out time
    /// </summary>
    ClockOut = 2,

    /// <summary>
    /// Adjust hours worked
    /// </summary>
    Hours = 3,

    /// <summary>
    /// Adjust break duration
    /// </summary>
    Break = 4,

    /// <summary>
    /// Manual entry (no attendance record)
    /// </summary>
    ManualEntry = 5,

    /// <summary>
    /// Correction due to system error
    /// </summary>
    SystemError = 6,

    /// <summary>
    /// Other adjustment
    /// </summary>
    Other = 7
}
