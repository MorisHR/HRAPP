namespace HRMS.Core.Enums;

/// <summary>
/// Timesheet period type
/// </summary>
public enum PeriodType
{
    /// <summary>
    /// Weekly timesheet (7 days)
    /// </summary>
    Weekly = 1,

    /// <summary>
    /// Bi-weekly timesheet (14 days)
    /// </summary>
    Biweekly = 2,

    /// <summary>
    /// Monthly timesheet (calendar month)
    /// </summary>
    Monthly = 3,

    /// <summary>
    /// Semi-monthly (twice per month)
    /// </summary>
    SemiMonthly = 4
}
