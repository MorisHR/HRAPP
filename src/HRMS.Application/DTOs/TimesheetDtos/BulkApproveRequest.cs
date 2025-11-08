namespace HRMS.Application.DTOs.TimesheetDtos;

public class BulkApproveRequest
{
    public List<Guid> TimesheetIds { get; set; } = new();
    public string? Notes { get; set; }
}
