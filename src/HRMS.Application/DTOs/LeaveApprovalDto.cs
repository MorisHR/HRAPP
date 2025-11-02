using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class LeaveApprovalDto
{
    public Guid Id { get; set; }
    public int ApprovalLevel { get; set; }
    public string ApproverRole { get; set; } = string.Empty;
    public string? ApproverName { get; set; }
    public ApprovalStatus Status { get; set; }
    public DateTime? ActionDate { get; set; }
    public string? Comments { get; set; }
}
