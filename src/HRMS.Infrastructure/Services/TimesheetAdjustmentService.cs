using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for handling timesheet adjustments and corrections
/// Maintains audit trail of all changes
/// </summary>
public class TimesheetAdjustmentService : ITimesheetAdjustmentService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<TimesheetAdjustmentService> _logger;

    public TimesheetAdjustmentService(
        TenantDbContext context,
        ILogger<TimesheetAdjustmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TimesheetAdjustment> CreateAdjustmentAsync(
        Guid timesheetEntryId,
        AdjustmentType adjustmentType,
        string fieldName,
        string oldValue,
        string newValue,
        string reason,
        Guid adjustedBy,
        string adjustedByName)
    {
        _logger.LogInformation(
            "Creating adjustment for timesheet entry {EntryId}. Type: {Type}, Field: {Field}",
            timesheetEntryId, adjustmentType, fieldName);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Adjustment reason is required");
        }

        var entry = await _context.TimesheetEntries
            .Include(e => e.Timesheet)
            .FirstOrDefaultAsync(e => e.Id == timesheetEntryId && !e.IsDeleted);

        if (entry == null)
        {
            throw new InvalidOperationException($"Timesheet entry {timesheetEntryId} not found");
        }

        // Check if timesheet is locked
        if (entry.Timesheet?.IsLocked == true)
        {
            // For locked timesheets, adjustments require approval
            _logger.LogInformation(
                "Timesheet is locked. Adjustment will require approval.");
        }

        var adjustment = new TimesheetAdjustment
        {
            Id = Guid.NewGuid(),
            TimesheetEntryId = timesheetEntryId,
            AdjustmentType = adjustmentType,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            Reason = reason,
            AdjustedBy = adjustedBy,
            AdjustedByName = adjustedByName,
            AdjustedAt = DateTime.UtcNow,
            Status = entry.Timesheet?.IsLocked == true
                ? AdjustmentStatus.Pending
                : AdjustmentStatus.AutoApplied,
            CreatedAt = DateTime.UtcNow
        };

        _context.TimesheetAdjustments.Add(adjustment);

        // If timesheet is not locked (Draft status), apply adjustment immediately
        if (entry.Timesheet?.Status == TimesheetStatus.Draft)
        {
            await ApplyAdjustmentAsync(adjustment);
            adjustment.Status = AdjustmentStatus.AutoApplied;
            adjustment.ApprovedAt = DateTime.UtcNow;
            adjustment.ApprovedBy = adjustedBy;
            adjustment.ApprovedByName = adjustedByName;

            _logger.LogInformation(
                "Adjustment {AdjustmentId} auto-applied to draft timesheet",
                adjustment.Id);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Adjustment {AdjustmentId} created with status {Status}",
            adjustment.Id, adjustment.Status);

        return adjustment;
    }

    public async Task<TimesheetAdjustment> ApproveAdjustmentAsync(
        Guid adjustmentId,
        Guid approvedBy,
        string approvedByName)
    {
        _logger.LogInformation(
            "Approving adjustment {AdjustmentId}",
            adjustmentId);

        var adjustment = await _context.TimesheetAdjustments
            .Include(a => a.TimesheetEntry)
            .ThenInclude(e => e!.Timesheet)
            .FirstOrDefaultAsync(a => a.Id == adjustmentId && !a.IsDeleted);

        if (adjustment == null)
        {
            throw new InvalidOperationException($"Adjustment {adjustmentId} not found");
        }

        if (adjustment.Status != AdjustmentStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Can only approve pending adjustments. Current status: {adjustment.Status}");
        }

        // Apply the adjustment
        await ApplyAdjustmentAsync(adjustment);

        // Update adjustment status
        adjustment.Status = AdjustmentStatus.Approved;
        adjustment.ApprovedBy = approvedBy;
        adjustment.ApprovedByName = approvedByName;
        adjustment.ApprovedAt = DateTime.UtcNow;
        adjustment.UpdatedAt = DateTime.UtcNow;

        // Recalculate timesheet totals
        if (adjustment.TimesheetEntry?.Timesheet != null)
        {
            adjustment.TimesheetEntry.Timesheet.CalculateTotals();
            adjustment.TimesheetEntry.Timesheet.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Adjustment {AdjustmentId} approved and applied",
            adjustmentId);

        return adjustment;
    }

    public async Task<TimesheetAdjustment> RejectAdjustmentAsync(
        Guid adjustmentId,
        Guid rejectedBy,
        string rejectionReason)
    {
        _logger.LogInformation(
            "Rejecting adjustment {AdjustmentId}",
            adjustmentId);

        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new ArgumentException("Rejection reason is required");
        }

        var adjustment = await _context.TimesheetAdjustments
            .FirstOrDefaultAsync(a => a.Id == adjustmentId && !a.IsDeleted);

        if (adjustment == null)
        {
            throw new InvalidOperationException($"Adjustment {adjustmentId} not found");
        }

        if (adjustment.Status != AdjustmentStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Can only reject pending adjustments. Current status: {adjustment.Status}");
        }

        adjustment.Status = AdjustmentStatus.Rejected;
        adjustment.RejectionReason = rejectionReason;
        adjustment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Adjustment {AdjustmentId} rejected. Reason: {Reason}",
            adjustmentId, rejectionReason);

        return adjustment;
    }

    public async Task<List<TimesheetAdjustment>> GetPendingAdjustmentsForTimesheetAsync(
        Guid timesheetId)
    {
        return await _context.TimesheetAdjustments
            .Include(a => a.TimesheetEntry)
            .Where(a =>
                a.TimesheetEntry!.TimesheetId == timesheetId &&
                a.Status == AdjustmentStatus.Pending &&
                !a.IsDeleted)
            .OrderBy(a => a.AdjustedAt)
            .ToListAsync();
    }

    public async Task ApplyAdjustmentAsync(TimesheetAdjustment adjustment)
    {
        var entry = await _context.TimesheetEntries
            .Include(e => e.Timesheet)
            .FirstOrDefaultAsync(e => e.Id == adjustment.TimesheetEntryId);

        if (entry == null)
        {
            throw new InvalidOperationException(
                $"Timesheet entry {adjustment.TimesheetEntryId} not found");
        }

        _logger.LogInformation(
            "Applying adjustment {AdjustmentId} to entry {EntryId}. Field: {Field}",
            adjustment.Id, entry.Id, adjustment.FieldName);

        // Apply adjustment based on field type
        switch (adjustment.FieldName.ToLower())
        {
            case "clockintime":
                if (DateTime.TryParse(adjustment.NewValue, out var clockIn))
                {
                    entry.ClockInTime = clockIn;
                }
                break;

            case "clockouttime":
                if (DateTime.TryParse(adjustment.NewValue, out var clockOut))
                {
                    entry.ClockOutTime = clockOut;
                }
                break;

            case "breakduration":
                if (int.TryParse(adjustment.NewValue, out var breakMinutes))
                {
                    entry.BreakDuration = breakMinutes;
                }
                break;

            case "regularhours":
                if (decimal.TryParse(adjustment.NewValue, out var regularHours))
                {
                    entry.RegularHours = regularHours;
                }
                break;

            case "overtimehours":
                if (decimal.TryParse(adjustment.NewValue, out var overtimeHours))
                {
                    entry.OvertimeHours = overtimeHours;
                }
                break;

            case "notes":
                entry.Notes = adjustment.NewValue;
                break;

            default:
                _logger.LogWarning(
                    "Unknown field name {FieldName} in adjustment {AdjustmentId}",
                    adjustment.FieldName, adjustment.Id);
                break;
        }

        // Recalculate hours if clock times changed
        if (adjustment.FieldName.ToLower() is "clockintime" or "clockouttime" or "breakduration")
        {
            entry.CalculateHours();
            _logger.LogInformation(
                "Hours recalculated for entry {EntryId}. Regular: {Regular}, Overtime: {Overtime}",
                entry.Id, entry.RegularHours, entry.OvertimeHours);
        }

        entry.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Adjustment {AdjustmentId} applied successfully",
            adjustment.Id);
    }
}
