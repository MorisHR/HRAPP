namespace HRMS.Application.DTOs;

public class EmployeeDraftDto
{
    public Guid Id { get; set; }
    public string FormDataJson { get; set; } = "{}";
    public string DraftName { get; set; } = string.Empty;
    public int CompletionPercentage { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? LastEditedBy { get; set; }
    public string? LastEditedByName { get; set; }
    public DateTime LastEditedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int DaysUntilExpiry { get; set; }
    public bool IsExpired { get; set; }
}

public class SaveEmployeeDraftRequest
{
    public Guid? Id { get; set; }
    public string FormDataJson { get; set; } = "{}";
    public string DraftName { get; set; } = string.Empty;
    public int CompletionPercentage { get; set; }
}
