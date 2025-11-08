using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Timesheet aggregates attendance data for payroll processing
/// Weekly/Monthly summary of employee work hours
/// Links attendance records to payroll calculation
/// </summary>
public class Timesheet : BaseEntity
{
    public Guid EmployeeId { get; set; }

    // Period Configuration
    public PeriodType PeriodType { get; set; } // Weekly, Biweekly, Monthly
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Hours Summary
    public decimal TotalRegularHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal TotalHolidayHours { get; set; }
    public decimal TotalSickLeaveHours { get; set; }
    public decimal TotalAnnualLeaveHours { get; set; }
    public decimal TotalAbsentHours { get; set; }

    // Computed property for payroll
    public decimal TotalPayableHours => TotalRegularHours + TotalOvertimeHours + TotalHolidayHours +
                                         TotalSickLeaveHours + TotalAnnualLeaveHours;

    // Workflow Status
    public TimesheetStatus Status { get; set; }

    // Submission tracking
    public DateTime? SubmittedAt { get; set; }
    public Guid? SubmittedBy { get; set; }

    // Approval tracking
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }

    // Rejection tracking
    public DateTime? RejectedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }

    // Locking for payroll processing
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public Guid? LockedBy { get; set; }

    // Notes
    public string? Notes { get; set; }

    // Tenant isolation
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
    public virtual ICollection<TimesheetComment> Comments { get; set; } = new List<TimesheetComment>();

    // Business logic methods
    public bool CanEdit()
    {
        return Status == TimesheetStatus.Draft && !IsLocked;
    }

    public bool CanSubmit()
    {
        return Status == TimesheetStatus.Draft && Entries.Any() && !IsLocked;
    }

    public bool CanApprove()
    {
        return Status == TimesheetStatus.Submitted && !IsLocked;
    }

    public bool CanReject()
    {
        return Status == TimesheetStatus.Submitted && !IsLocked;
    }

    public void CalculateTotals()
    {
        TotalRegularHours = Entries.Sum(e => e.RegularHours);
        TotalOvertimeHours = Entries.Sum(e => e.OvertimeHours);
        TotalHolidayHours = Entries.Sum(e => e.HolidayHours);
        TotalSickLeaveHours = Entries.Sum(e => e.SickLeaveHours);
        TotalAnnualLeaveHours = Entries.Sum(e => e.AnnualLeaveHours);
        TotalAbsentHours = Entries.Where(e => e.IsAbsent).Sum(e => 8); // Assuming 8 hour workday
    }

    public void Submit(Guid submittedBy)
    {
        if (!CanSubmit())
            throw new InvalidOperationException("Timesheet cannot be submitted in current state");

        Status = TimesheetStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
        SubmittedBy = submittedBy;
    }

    public void Approve(Guid approvedBy, string approverName)
    {
        if (!CanApprove())
            throw new InvalidOperationException("Timesheet cannot be approved in current state");

        Status = TimesheetStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = approvedBy;
        ApprovedByName = approverName;

        // Lock timesheet after approval
        IsLocked = true;
        LockedAt = DateTime.UtcNow;
        LockedBy = approvedBy;
    }

    public void Reject(Guid rejectedBy, string rejectionReason)
    {
        if (!CanReject())
            throw new InvalidOperationException("Timesheet cannot be rejected in current state");

        Status = TimesheetStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
        RejectedBy = rejectedBy;
        RejectionReason = rejectionReason;
    }

    public void Reopen()
    {
        if (Status != TimesheetStatus.Rejected)
            throw new InvalidOperationException("Only rejected timesheets can be reopened");

        Status = TimesheetStatus.Draft;
        RejectedAt = null;
        RejectedBy = null;
        RejectionReason = null;
    }
}
