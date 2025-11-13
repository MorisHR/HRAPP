namespace HRMS.Application.DTOs;

/// <summary>
/// Response for tenant employee login
/// Includes access token and refresh token for secure token rotation
/// </summary>
public class TenantLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public EmployeeDto Employee { get; set; } = null!;
    public Guid TenantId { get; set; }
    public string Subdomain { get; set; } = string.Empty;
}
