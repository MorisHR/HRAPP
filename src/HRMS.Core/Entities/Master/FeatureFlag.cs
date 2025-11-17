namespace HRMS.Core.Entities.Master;

/// <summary>
/// Feature Flag entity - stored in Master schema
/// Enables per-tenant feature control with gradual rollout capability
/// FORTUNE 500 PATTERN: Canary deployment, emergency rollback, A/B testing
/// </summary>
public class FeatureFlag : BaseEntity
{
    /// <summary>
    /// Tenant ID - NULL indicates global default for all tenants
    /// NON-NULL indicates tenant-specific override
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Module name: "auth", "dashboard", "employees", "payroll", "attendance", "leaves", etc.
    /// Used for feature-level granular control
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Whether the feature is enabled (true) or disabled (false)
    /// Defaults to FALSE for safety (opt-in rollout)
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Rollout percentage (0-100)
    /// 0 = completely disabled, 100 = fully enabled
    /// Allows canary deployment (e.g., enable for 10% of users, then 50%, then 100%)
    /// </summary>
    public int RolloutPercentage { get; set; }

    /// <summary>
    /// Feature description for documentation
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Tags for categorization (comma-separated)
    /// Examples: "experimental", "beta", "stable", "deprecated"
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Minimum required tier for this feature
    /// NULL = available for all tiers, otherwise specifies minimum tier
    /// </summary>
    public string? MinimumTier { get; set; }

    /// <summary>
    /// Emergency rollback flag
    /// Set to TRUE by emergency rollback endpoint to quickly disable feature
    /// </summary>
    public bool IsEmergencyDisabled { get; set; }

    /// <summary>
    /// Emergency rollback reason
    /// Audit trail for why feature was emergency disabled
    /// </summary>
    public string? EmergencyDisabledReason { get; set; }

    /// <summary>
    /// Emergency disabled timestamp
    /// When the emergency rollback was triggered
    /// </summary>
    public DateTime? EmergencyDisabledAt { get; set; }

    /// <summary>
    /// Who triggered the emergency rollback
    /// SuperAdmin email for audit trail
    /// </summary>
    public string? EmergencyDisabledBy { get; set; }

    /// <summary>
    /// Navigation property to Tenant
    /// NULL for global defaults
    /// </summary>
    public virtual Tenant? Tenant { get; set; }
}
