using HRMS.Core.Entities.Tenant;

namespace HRMS.Core.Interfaces;

/// <summary>
/// Service for managing timesheet approval workflow
/// </summary>
public interface ITimesheetApprovalService
{
    /// <summary>
    /// Employee submits timesheet for manager approval
    /// </summary>
    Task<Timesheet> SubmitTimesheetAsync(Guid timesheetId, Guid employeeId);

    /// <summary>
    /// Manager approves timesheet
    /// </summary>
    Task<Timesheet> ApproveTimesheetAsync(Guid timesheetId, Guid managerId, string? approvalNotes = null);

    /// <summary>
    /// Manager rejects timesheet with reason
    /// </summary>
    Task<Timesheet> RejectTimesheetAsync(Guid timesheetId, Guid managerId, string rejectionReason);

    /// <summary>
    /// Bulk approve multiple timesheets
    /// </summary>
    Task<int> BulkApproveTimesheetsAsync(List<Guid> timesheetIds, Guid managerId);

    /// <summary>
    /// Lock timesheet for payroll processing
    /// </summary>
    Task<Timesheet> LockTimesheetAsync(Guid timesheetId, Guid lockedBy);

    /// <summary>
    /// Reopen rejected timesheet for editing
    /// </summary>
    Task<Timesheet> ReopenTimesheetAsync(Guid timesheetId, Guid employeeId);

    /// <summary>
    /// Check if user can approve timesheet (is manager or HR admin)
    /// </summary>
    Task<bool> CanApproveTimesheetAsync(Guid timesheetId, Guid userId);

    /// <summary>
    /// Get all timesheets pending approval for a manager
    /// </summary>
    Task<List<Timesheet>> GetPendingApprovalsForManagerAsync(Guid managerId);
}
