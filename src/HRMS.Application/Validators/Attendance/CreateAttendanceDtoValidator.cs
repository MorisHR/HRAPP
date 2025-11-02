using FluentValidation;
using HRMS.Application.DTOs.AttendanceDtos;

namespace HRMS.Application.Validators.Attendance;

/// <summary>
/// Production-grade validator for attendance creation
/// Validates attendance date, check-in/check-out times, and business rules
/// Prevents data integrity issues and ensures accurate time tracking
/// </summary>
public class CreateAttendanceDtoValidator : AbstractValidator<CreateAttendanceDto>
{
    public CreateAttendanceDtoValidator()
    {
        // Employee ID Validation
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee ID is required")
            .WithName("Employee");

        // Date Validation
        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Attendance date is required")
            .Must(BeValidAttendanceDate)
            .WithMessage("Attendance date must be within the last 90 days and not in the future")
            .WithName("Date");

        // Check-in Time Validation
        RuleFor(x => x.CheckInTime)
            .Must((dto, checkIn) => BeValidCheckInTime(dto.Date, checkIn))
            .When(x => x.CheckInTime.HasValue)
            .WithMessage("Check-in time must be on the same day as the attendance date")
            .WithName("Check-In Time");

        // Check-out Time Validation
        RuleFor(x => x.CheckOutTime)
            .Must((dto, checkOut) => BeValidCheckOutTime(dto.Date, checkOut))
            .When(x => x.CheckOutTime.HasValue)
            .WithMessage("Check-out time must be on the same day as attendance date or the following day")
            .WithName("Check-Out Time");

        // Cross-field validation: Check-out must be after check-in
        RuleFor(x => x)
            .Must(x => x.CheckOutTime > x.CheckInTime)
            .When(x => x.CheckInTime.HasValue && x.CheckOutTime.HasValue)
            .WithMessage("Check-out time must be after check-in time");

        // Cross-field validation: Working hours must be reasonable (max 24 hours)
        RuleFor(x => x)
            .Must(x => BeReasonableWorkingHours(x.CheckInTime, x.CheckOutTime))
            .When(x => x.CheckInTime.HasValue && x.CheckOutTime.HasValue)
            .WithMessage("Working hours cannot exceed 24 hours. Please verify check-in and check-out times.");

        // Cross-field validation: Minimum working duration (1 minute)
        RuleFor(x => x)
            .Must(x => BeMinimumWorkingDuration(x.CheckInTime, x.CheckOutTime))
            .When(x => x.CheckInTime.HasValue && x.CheckOutTime.HasValue)
            .WithMessage("Working duration must be at least 1 minute");

        // Either check-in or check-out must be provided
        RuleFor(x => x)
            .Must(x => x.CheckInTime.HasValue || x.CheckOutTime.HasValue)
            .WithMessage("At least one of check-in or check-out time must be provided");

        // Remarks Validation (optional, but reasonable length)
        RuleFor(x => x.Remarks)
            .MaximumLength(500)
            .WithMessage("Remarks cannot exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Remarks))
            .WithName("Remarks");
    }

    /// <summary>
    /// Validates that attendance date is within allowed range (past 90 days, not future)
    /// </summary>
    private bool BeValidAttendanceDate(DateTime date)
    {
        var today = DateTime.UtcNow.Date;
        var ninetyDaysAgo = today.AddDays(-90);

        var attendanceDate = date.Date;

        // Must not be in the future
        if (attendanceDate > today)
            return false;

        // Must not be too old (configurable based on organization policy)
        if (attendanceDate < ninetyDaysAgo)
            return false;

        return true;
    }

    /// <summary>
    /// Validates that check-in time is on the same day as attendance date
    /// </summary>
    private bool BeValidCheckInTime(DateTime attendanceDate, DateTime? checkInTime)
    {
        if (!checkInTime.HasValue)
            return true;

        // Check-in must be on the same date as attendance date
        return checkInTime.Value.Date == attendanceDate.Date;
    }

    /// <summary>
    /// Validates that check-out time is reasonable (same day or next day for night shifts)
    /// </summary>
    private bool BeValidCheckOutTime(DateTime attendanceDate, DateTime? checkOutTime)
    {
        if (!checkOutTime.HasValue)
            return true;

        var checkOutDate = checkOutTime.Value.Date;
        var attendanceDateOnly = attendanceDate.Date;

        // Check-out can be on the same day or the following day (for night shifts)
        return checkOutDate == attendanceDateOnly || checkOutDate == attendanceDateOnly.AddDays(1);
    }

    /// <summary>
    /// Validates that working hours are reasonable (max 24 hours)
    /// </summary>
    private bool BeReasonableWorkingHours(DateTime? checkInTime, DateTime? checkOutTime)
    {
        if (!checkInTime.HasValue || !checkOutTime.HasValue)
            return true;

        var duration = checkOutTime.Value - checkInTime.Value;

        // Working hours must not exceed 24 hours
        return duration.TotalHours <= 24;
    }

    /// <summary>
    /// Validates minimum working duration (1 minute)
    /// </summary>
    private bool BeMinimumWorkingDuration(DateTime? checkInTime, DateTime? checkOutTime)
    {
        if (!checkInTime.HasValue || !checkOutTime.HasValue)
            return true;

        var duration = checkOutTime.Value - checkInTime.Value;

        // Must be at least 1 minute
        return duration.TotalMinutes >= 1;
    }
}
