namespace HRMS.Application.DTOs;

/// <summary>
/// Request DTO for activating a tenant account
/// </summary>
public class ActivateTenantRequest
{
    /// <summary>
    /// Activation token sent via email
    /// </summary>
    public string ActivationToken { get; set; } = string.Empty;
}
