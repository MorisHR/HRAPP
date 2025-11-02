using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

/// <summary>
/// Response DTO for tenant information
/// </summary>
public class TenantDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public SubscriptionPlan SubscriptionPlan { get; set; }
    public string SubscriptionPlanDisplay { get; set; } = string.Empty;

    // Resource Limits and Usage
    public int MaxUsers { get; set; }
    public int CurrentUserCount { get; set; }
    public long MaxStorageBytes { get; set; }
    public long CurrentStorageBytes { get; set; }
    public int MaxApiCallsPerHour { get; set; }

    // Dates
    public DateTime CreatedAt { get; set; }
    public DateTime SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public DateTime? TrialEndDate { get; set; }

    // Suspension/Deletion Info
    public string? SuspensionReason { get; set; }
    public DateTime? SuspensionDate { get; set; }
    public DateTime? SoftDeleteDate { get; set; }
    public string? DeletionReason { get; set; }
    public int? DaysUntilHardDelete { get; set; }

    // Admin User
    public string AdminUserName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
}
