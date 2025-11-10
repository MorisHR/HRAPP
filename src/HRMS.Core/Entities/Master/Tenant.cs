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

    /// <summary>
    /// PRODUCTION-GRADE: Yearly subscription price in Mauritian Rupees (MUR)
    /// FORTUNE 500 PATTERN: Annual billing reduces churn (Salesforce, HubSpot, Zendesk)
    /// PRECISION: decimal(18,2) for accurate financial calculations
    /// </summary>
    public decimal YearlyPriceMUR { get; set; }

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

    /// <summary>
    /// PRODUCTION-GRADE: Last subscription notification sent
    /// FORTUNE 500 PATTERN: Prevents duplicate emails (Stripe, Chargebee pattern)
    /// </summary>
    public DateTime? LastNotificationSent { get; set; }

    /// <summary>
    /// Type of last notification sent
    /// AUDIT: Track notification progression (30d -> 15d -> 7d -> expiry)
    /// </summary>
    public SubscriptionNotificationType? LastNotificationType { get; set; }

    /// <summary>
    /// Grace period start date (when subscription expired)
    /// FORTUNE 500: 14-day grace period before suspension
    /// </summary>
    public DateTime? GracePeriodStartDate { get; set; }

    // Admin user details (first tenant admin)
    public string AdminUserName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;

    // Tenant activation fields
    public string? ActivationToken { get; set; }
    public DateTime? ActivationTokenExpiry { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? ActivatedBy { get; set; }

    // Tenant type
    public bool IsGovernmentEntity { get; set; } = false;

    // Usage tracking
    public int CurrentUserCount { get; set; }
    public int CurrentStorageGB { get; set; }

    // Industry Sector (references master.IndustrySectors)
    public int? SectorId { get; set; }
    public DateTime? SectorSelectedAt { get; set; }

    // Navigation properties
    public virtual IndustrySector? Sector { get; set; }

    /// <summary>
    /// PRODUCTION-GRADE: Yearly subscription payment history
    /// FORTUNE 500 PATTERN: Complete financial audit trail
    /// </summary>
    public virtual ICollection<SubscriptionPayment> SubscriptionPayments { get; set; } = new List<SubscriptionPayment>();

    /// <summary>
    /// PRODUCTION-GRADE: Subscription notification audit log
    /// FORTUNE 500 PATTERN: Email deduplication and compliance tracking
    /// </summary>
    public virtual ICollection<SubscriptionNotificationLog> SubscriptionNotificationLogs { get; set; } = new List<SubscriptionNotificationLog>();

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

    /// <summary>
    /// PRODUCTION-GRADE: Days until subscription expiry
    /// FORTUNE 500 PATTERN: Proactive renewal notifications
    /// </summary>
    public int? DaysUntilSubscriptionExpiry()
    {
        if (!SubscriptionEndDate.HasValue)
            return null;

        return (SubscriptionEndDate.Value - DateTime.UtcNow).Days;
    }

    /// <summary>
    /// PRODUCTION-GRADE: Is subscription expired?
    /// PERFORMANCE: Computed property for quick filtering
    /// </summary>
    public bool IsSubscriptionExpired()
    {
        if (!SubscriptionEndDate.HasValue)
            return false;

        return DateTime.UtcNow > SubscriptionEndDate.Value;
    }

    /// <summary>
    /// PRODUCTION-GRADE: Is subscription expiring within 7 days?
    /// FORTUNE 500 PATTERN: Early warning system triggers TenantStatus.ExpiringSoon
    /// </summary>
    public bool IsInExpiryWarningPeriod()
    {
        var daysRemaining = DaysUntilSubscriptionExpiry();
        return daysRemaining.HasValue && daysRemaining.Value <= 7 && daysRemaining.Value > 0;
    }

    /// <summary>
    /// PRODUCTION-GRADE: Days since subscription expired (for grace period tracking)
    /// FORTUNE 500 PATTERN: 14-day grace period before suspension
    /// </summary>
    public int? DaysSinceExpiry()
    {
        if (!IsSubscriptionExpired())
            return null;

        return (DateTime.UtcNow - SubscriptionEndDate!.Value).Days;
    }

    /// <summary>
    /// PRODUCTION-GRADE: Is tenant in grace period?
    /// FORTUNE 500 PATTERN: 0-14 days after expiry before suspension
    /// </summary>
    public bool IsInGracePeriod()
    {
        var daysSince = DaysSinceExpiry();
        return daysSince.HasValue && daysSince.Value <= 14;
    }
}
