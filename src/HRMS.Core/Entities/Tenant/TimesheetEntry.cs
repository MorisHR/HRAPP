using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Daily timesheet entry - represents one day of work
/// Generated from attendance records
/// Can be manually adjusted if needed
/// </summary>
public class TimesheetEntry : BaseEntity
{
    public Guid TimesheetId { get; set; }
    public DateTime Date { get; set; }

    // Link to source attendance record
    public Guid? AttendanceId { get; set; }

    // Clock times (from attendance or manual entry)
    public DateTime? ClockInTime { get; set; }
    public DateTime? ClockOutTime { get; set; }

    // Break duration in minutes
    public int BreakDuration { get; set; } = 30; // Default 30 minutes

    // Actual hours worked
    public decimal ActualHours { get; set; }

    // Hours breakdown
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal HolidayHours { get; set; }
    public decimal SickLeaveHours { get; set; }
    public decimal AnnualLeaveHours { get; set; }

    // Day status flags
    public bool IsAbsent { get; set; }
    public bool IsHoliday { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsOnLeave { get; set; }

    // Day type categorization
    public DayType DayType { get; set; }

    // Notes for this specific day
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Timesheet? Timesheet { get; set; }
    public virtual Attendance? Attendance { get; set; }
    public virtual ICollection<TimesheetAdjustment> Adjustments { get; set; } = new List<TimesheetAdjustment>();

    /// <summary>
    /// Calculate hours worked based on clock in/out times
    /// </summary>
    public void CalculateHours(decimal dailyHours = 8, decimal weeklyHoursThreshold = 40)
    {
        if (IsAbsent)
        {
            ActualHours = 0;
            RegularHours = 0;
            OvertimeHours = 0;
            return;
        }

        if (IsOnLeave)
        {
            // Leave hours are set separately
            ActualHours = SickLeaveHours + AnnualLeaveHours;
            RegularHours = 0;
            OvertimeHours = 0;
            return;
        }

        if (ClockInTime.HasValue && ClockOutTime.HasValue)
        {
            var totalMinutes = (ClockOutTime.Value - ClockInTime.Value).TotalMinutes;
            var workMinutes = totalMinutes - BreakDuration;
            ActualHours = (decimal)(workMinutes / 60.0);

            // Split into regular and overtime
            if (IsHoliday || IsWeekend)
            {
                // All hours on holiday/weekend are holiday hours
                HolidayHours = ActualHours;
                RegularHours = 0;
                OvertimeHours = 0;
            }
            else
            {
                // First 8 hours are regular, rest is overtime
                if (ActualHours <= dailyHours)
                {
                    RegularHours = ActualHours;
                    OvertimeHours = 0;
                }
                else
                {
                    RegularHours = dailyHours;
                    OvertimeHours = ActualHours - dailyHours;
                }
            }
        }
        else
        {
            // No clock times - mark as absent unless on leave or holiday
            if (!IsHoliday && !IsWeekend)
            {
                IsAbsent = true;
            }
            ActualHours = 0;
            RegularHours = 0;
            OvertimeHours = 0;
        }
    }

    /// <summary>
    /// Apply Mauritius overtime rules based on industry sector
    /// Manufacturing/Shops/Hotels: >45 hrs/week = OT
    /// Others: >40 hrs/week = OT
    /// </summary>
    public void ApplyOvertimeRules(decimal weeklyRegularHours, decimal weeklyThreshold)
    {
        // If weekly hours exceed threshold, move excess to overtime
        if (weeklyRegularHours > weeklyThreshold)
        {
            var excessHours = weeklyRegularHours - weeklyThreshold;

            // If this day's regular hours can cover the excess
            if (RegularHours >= excessHours)
            {
                OvertimeHours += excessHours;
                RegularHours -= excessHours;
            }
            else
            {
                // All regular hours become overtime
                OvertimeHours += RegularHours;
                RegularHours = 0;
            }
        }
    }
}
