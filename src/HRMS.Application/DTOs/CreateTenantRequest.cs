using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

/// <summary>
/// Request DTO for creating a new tenant
/// </summary>
public class CreateTenantRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public SubscriptionPlan SubscriptionPlan { get; set; }

    // Resource Limits
    public int MaxUsers { get; set; } = 50;
    public long MaxStorageBytes { get; set; } = 10737418240; // 10 GB default
    public int MaxApiCallsPerHour { get; set; } = 10000;

    // Admin User Details
    public string AdminUserName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}
