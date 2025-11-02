using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Tenant's customized compliance rules (overrides sector defaults)
/// Stored in Tenant Schema
/// </summary>
public class TenantCustomComplianceRule : BaseEntity
{
    public int SectorComplianceRuleId { get; set; }  // References master.SectorComplianceRules.Id

    public bool IsUsingDefault { get; set; } = true;

    /// <summary>
    /// Tenant's customized values (only if IsUsingDefault = false)
    /// Same JSON structure as SectorComplianceRule.RuleConfig
    /// </summary>
    public string? CustomRuleConfig { get; set; }

    public string? Justification { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Cached info from master rule (for quick reference)
    public string? RuleCategory { get; set; }
    public string? RuleName { get; set; }
}
