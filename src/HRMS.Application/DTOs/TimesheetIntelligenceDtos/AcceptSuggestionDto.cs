namespace HRMS.Application.DTOs.TimesheetIntelligenceDtos;

/// <summary>
/// Accept/Reject/Modify a project allocation suggestion
/// </summary>
public class AcceptSuggestionDto
{
    public Guid SuggestionId { get; set; }
    public string Action { get; set; } = "Accept"; // Accept, Reject, Modify
    public decimal? ModifiedHours { get; set; }
    public string? TaskDescription { get; set; }
    public string? RejectionReason { get; set; }
}

/// <summary>
/// Batch accept/reject suggestions
/// </summary>
public class BatchAcceptSuggestionsDto
{
    public List<Guid> SuggestionIds { get; set; } = new();
    public string Action { get; set; } = "Accept"; // Accept, Reject
    public string? RejectionReason { get; set; }
}
