namespace HRMS.Application.DTOs;

/// <summary>
/// Response DTO for successful tenant activation
/// </summary>
public class ActivateTenantResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TenantSubdomain { get; set; }
    public string? LoginUrl { get; set; }
    public string? AdminEmail { get; set; }
}
