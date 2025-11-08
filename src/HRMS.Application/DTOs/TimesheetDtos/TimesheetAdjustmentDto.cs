using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.TimesheetDtos;

public class TimesheetAdjustmentDto
{
    public Guid Id { get; set; }
    public Guid TimesheetEntryId { get; set; }

    public AdjustmentType AdjustmentType { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Reason { get; set; } = string.Empty;

    public Guid AdjustedBy { get; set; }
    public string? AdjustedByName { get; set; }
    public DateTime AdjustedAt { get; set; }

    public AdjustmentStatus Status { get; set; }

    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? RejectionReason { get; set; }
}
