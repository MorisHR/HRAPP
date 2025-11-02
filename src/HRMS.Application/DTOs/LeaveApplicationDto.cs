using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class LeaveApplicationDto
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;

    // Employee details
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    // Leave details
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;

    // Status
    public LeaveStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime AppliedDate { get; set; }

    // Approval details
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedByName { get; set; }
    public string? ApproverComments { get; set; }

    public DateTime? RejectedDate { get; set; }
    public string? RejectedByName { get; set; }
    public string? RejectionReason { get; set; }

    public string? ContactNumber { get; set; }
    public string? AttachmentPath { get; set; }

    public List<LeaveApprovalDto> Approvals { get; set; } = new();
}
