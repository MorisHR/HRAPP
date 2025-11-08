using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.TimesheetDtos;

public class CreateAdjustmentRequest
{
    public AdjustmentType AdjustmentType { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
