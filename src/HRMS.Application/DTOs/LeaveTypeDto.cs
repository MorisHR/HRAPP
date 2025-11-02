using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class LeaveTypeDto
{
    public Guid Id { get; set; }
    public LeaveTypeEnum TypeCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultEntitlement { get; set; }
    public bool IsPaid { get; set; }
    public bool CanCarryForward { get; set; }
    public int MaxCarryForwardDays { get; set; }
    public bool RequiresDocumentation { get; set; }
    public bool IsActive { get; set; }
}
