using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Tenant entity - stored in Master schema
/// Represents a company/organization using the HRMS system
/// </summary>
public class Tenant : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }

    // Employee-based Tier (replaces SubscriptionPlan)
    public EmployeeTier EmployeeTier { get; set; }
    public decimal MonthlyPrice { get; set; }

    // Resource Limits (simplified for easier understanding)
    public int MaxUsers { get; set; }
    public int MaxStorageGB { get; set; }
    public int ApiCallsPerMonth { get; set; }

    // Suspension/Deletion tracking
    public string? SuspensionReason { get; set; }
    public DateTime? SuspensionDate { get; set; }
    public DateTime? SoftDeleteDate { get; set; }
    public string? DeletionReason { get; set; }
    public int GracePeriodDays { get; set; } = 30;

    // Subscription tracking
    public DateTime SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public DateTime? TrialEndDate { get; set; }

    // Admin user details (first tenant admin)
    public string AdminUserName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;

    // Usage tracking
    public int CurrentUserCount { get; set; }
    public int CurrentStorageGB { get; set; }

    // Industry Sector (references master.IndustrySectors)
    public int? SectorId { get; set; }
    public DateTime? SectorSelectedAt { get; set; }

    // Navigation
    public virtual IndustrySector? Sector { get; set; }

    // Computed property: Can be hard deleted?
    public bool CanBeHardDeleted()
    {
        if (Status != TenantStatus.SoftDeleted || !SoftDeleteDate.HasValue)
            return false;

        return DateTime.UtcNow >= SoftDeleteDate.Value.AddDays(GracePeriodDays);
    }

    // Computed property: Days remaining before hard delete
    public int? DaysUntilHardDelete()
    {
        if (Status != TenantStatus.SoftDeleted || !SoftDeleteDate.HasValue)
            return null;

        var hardDeleteDate = SoftDeleteDate.Value.AddDays(GracePeriodDays);
        var daysRemaining = (hardDeleteDate - DateTime.UtcNow).Days;

        return daysRemaining > 0 ? daysRemaining : 0;
    }
}
