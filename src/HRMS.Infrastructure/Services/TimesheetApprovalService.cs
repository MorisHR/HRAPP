using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for managing timesheet approval workflow
/// Implements status transitions and authorization checks
/// </summary>
public class TimesheetApprovalService : ITimesheetApprovalService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<TimesheetApprovalService> _logger;

    public TimesheetApprovalService(
        TenantDbContext context,
        ILogger<TimesheetApprovalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Timesheet> SubmitTimesheetAsync(Guid timesheetId, Guid employeeId)
    {
        _logger.LogInformation(
            "Employee {EmployeeId} submitting timesheet {TimesheetId}",
            employeeId, timesheetId);

        var timesheet = await _context.Timesheets
            .Include(t => t.Entries)
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == timesheetId && !t.IsDeleted);

        if (timesheet == null)
        {
            throw new InvalidOperationException($"Timesheet {timesheetId} not found");
        }

        // Verify ownership
        if (timesheet.EmployeeId != employeeId)
        {
            throw new UnauthorizedAccessException(
                "Employee can only submit their own timesheets");
        }

        // Validate submission
        if (!timesheet.CanSubmit())
        {
            throw new InvalidOperationException(
                $"Timesheet cannot be submitted. Status: {timesheet.Status}, " +
                $"Entries: {timesheet.Entries.Count}, Locked: {timesheet.IsLocked}");
        }

        // Submit
        timesheet.Submit(employeeId);
        timesheet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Timesheet {TimesheetId} submitted successfully by employee {EmployeeId}",
            timesheetId, employeeId);

        return timesheet;
    }

    public async Task<Timesheet> ApproveTimesheetAsync(
        Guid timesheetId,
        Guid managerId,
        string? approvalNotes = null)
    {
        _logger.LogInformation(
            "Manager {ManagerId} approving timesheet {TimesheetId}",
            managerId, timesheetId);

        var timesheet = await _context.Timesheets
            .Include(t => t.Entries)
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == timesheetId && !t.IsDeleted);

        if (timesheet == null)
        {
            throw new InvalidOperationException($"Timesheet {timesheetId} not found");
        }

        // Check authorization
        if (!await CanApproveTimesheetAsync(timesheetId, managerId))
        {
            throw new UnauthorizedAccessException(
                "User is not authorized to approve this timesheet");
        }

        // Validate approval
        if (!timesheet.CanApprove())
        {
            throw new InvalidOperationException(
                $"Timesheet cannot be approved. Status: {timesheet.Status}");
        }

        // Get manager name
        var manager = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == managerId);

        var managerName = manager?.FullName ?? "Unknown";

        // Approve
        timesheet.Approve(managerId, managerName);
        timesheet.UpdatedAt = DateTime.UtcNow;

        // Add approval note as comment if provided
        if (!string.IsNullOrWhiteSpace(approvalNotes))
        {
            var comment = new TimesheetComment
            {
                Id = Guid.NewGuid(),
                TimesheetId = timesheetId,
                UserId = managerId,
                UserName = managerName,
                Comment = $"Approved: {approvalNotes}",
                CommentedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.TimesheetComments.Add(comment);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Timesheet {TimesheetId} approved by manager {ManagerId} ({ManagerName})",
            timesheetId, managerId, managerName);

        return timesheet;
    }

    public async Task<Timesheet> RejectTimesheetAsync(
        Guid timesheetId,
        Guid managerId,
        string rejectionReason)
    {
        _logger.LogInformation(
            "Manager {ManagerId} rejecting timesheet {TimesheetId}",
            managerId, timesheetId);

        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new ArgumentException("Rejection reason is required");
        }

        var timesheet = await _context.Timesheets
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == timesheetId && !t.IsDeleted);

        if (timesheet == null)
        {
            throw new InvalidOperationException($"Timesheet {timesheetId} not found");
        }

        // Check authorization
        if (!await CanApproveTimesheetAsync(timesheetId, managerId))
        {
            throw new UnauthorizedAccessException(
                "User is not authorized to reject this timesheet");
        }

        // Validate rejection
        if (!timesheet.CanReject())
        {
            throw new InvalidOperationException(
                $"Timesheet cannot be rejected. Status: {timesheet.Status}");
        }

        // Reject
        timesheet.Reject(managerId, rejectionReason);
        timesheet.UpdatedAt = DateTime.UtcNow;

        // Add rejection as comment
        var manager = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == managerId);

        var comment = new TimesheetComment
        {
            Id = Guid.NewGuid(),
            TimesheetId = timesheetId,
            UserId = managerId,
            UserName = manager?.FullName ?? "Unknown",
            Comment = $"Rejected: {rejectionReason}",
            CommentedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.TimesheetComments.Add(comment);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Timesheet {TimesheetId} rejected by manager {ManagerId}. Reason: {Reason}",
            timesheetId, managerId, rejectionReason);

        return timesheet;
    }

    public async Task<int> BulkApproveTimesheetsAsync(List<Guid> timesheetIds, Guid managerId)
    {
        _logger.LogInformation(
            "Manager {ManagerId} bulk approving {Count} timesheets",
            managerId, timesheetIds.Count);

        int approvedCount = 0;

        foreach (var timesheetId in timesheetIds)
        {
            try
            {
                await ApproveTimesheetAsync(timesheetId, managerId);
                approvedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to approve timesheet {TimesheetId} in bulk operation",
                    timesheetId);
            }
        }

        _logger.LogInformation(
            "Bulk approval completed. Approved {Approved} out of {Total}",
            approvedCount, timesheetIds.Count);

        return approvedCount;
    }

    public async Task<Timesheet> LockTimesheetAsync(Guid timesheetId, Guid lockedBy)
    {
        _logger.LogInformation(
            "Locking timesheet {TimesheetId} for payroll processing",
            timesheetId);

        var timesheet = await _context.Timesheets
            .FirstOrDefaultAsync(t => t.Id == timesheetId && !t.IsDeleted);

        if (timesheet == null)
        {
            throw new InvalidOperationException($"Timesheet {timesheetId} not found");
        }

        if (timesheet.Status != TimesheetStatus.Approved)
        {
            throw new InvalidOperationException(
                "Only approved timesheets can be locked");
        }

        timesheet.Status = TimesheetStatus.Locked;
        timesheet.IsLocked = true;
        timesheet.LockedAt = DateTime.UtcNow;
        timesheet.LockedBy = lockedBy;
        timesheet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Timesheet {TimesheetId} locked successfully", timesheetId);

        return timesheet;
    }

    public async Task<Timesheet> ReopenTimesheetAsync(Guid timesheetId, Guid employeeId)
    {
        _logger.LogInformation(
            "Reopening timesheet {TimesheetId} for employee {EmployeeId}",
            timesheetId, employeeId);

        var timesheet = await _context.Timesheets
            .FirstOrDefaultAsync(t => t.Id == timesheetId && !t.IsDeleted);

        if (timesheet == null)
        {
            throw new InvalidOperationException($"Timesheet {timesheetId} not found");
        }

        // Verify ownership
        if (timesheet.EmployeeId != employeeId)
        {
            throw new UnauthorizedAccessException(
                "Employee can only reopen their own timesheets");
        }

        // Can only reopen rejected timesheets
        if (timesheet.Status != TimesheetStatus.Rejected)
        {
            throw new InvalidOperationException(
                "Only rejected timesheets can be reopened");
        }

        timesheet.Reopen();
        timesheet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Timesheet {TimesheetId} reopened for editing",
            timesheetId);

        return timesheet;
    }

    public async Task<bool> CanApproveTimesheetAsync(Guid timesheetId, Guid userId)
    {
        var timesheet = await _context.Timesheets
            .Include(t => t.Employee)
            .ThenInclude(e => e!.Manager)
            .FirstOrDefaultAsync(t => t.Id == timesheetId && !t.IsDeleted);

        if (timesheet == null)
        {
            return false;
        }

        // Check if user is the employee's direct manager
        if (timesheet.Employee?.ManagerId == userId)
        {
            return true;
        }

        // Check if user is department head
        if (timesheet.Employee?.Department?.DepartmentHeadId == userId)
        {
            return true;
        }

        // Check if user is HR admin (this would typically check roles)
        // For now, we'll allow any manager to approve
        var user = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == userId && !e.IsDeleted);

        // If user has "Manager" or "Admin" in job title, allow approval
        if (user?.JobTitle?.Contains("Manager", StringComparison.OrdinalIgnoreCase) == true ||
            user?.JobTitle?.Contains("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            user?.JobTitle?.Contains("HR", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return false;
    }

    public async Task<List<Timesheet>> GetPendingApprovalsForManagerAsync(Guid managerId)
    {
        _logger.LogInformation(
            "Getting pending approvals for manager {ManagerId}",
            managerId);

        // Get all employees reporting to this manager
        var reportingEmployeeIds = await _context.Employees
            .Where(e => e.ManagerId == managerId && !e.IsDeleted)
            .Select(e => e.Id)
            .ToListAsync();

        // Get pending timesheets for those employees
        var pendingTimesheets = await _context.Timesheets
            .Include(t => t.Employee)
            .Include(t => t.Entries)
            .Where(t =>
                reportingEmployeeIds.Contains(t.EmployeeId) &&
                t.Status == TimesheetStatus.Submitted &&
                !t.IsDeleted)
            .OrderBy(t => t.SubmittedAt)
            .ToListAsync();

        _logger.LogInformation(
            "Found {Count} pending timesheets for manager {ManagerId}",
            pendingTimesheets.Count, managerId);

        return pendingTimesheets;
    }
}
