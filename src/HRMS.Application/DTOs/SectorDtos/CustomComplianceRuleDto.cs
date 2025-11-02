using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.SectorDtos;

/// <summary>
/// Tenant's custom compliance rule configuration
/// </summary>
public class CustomComplianceRuleDto
{
    public Guid Id { get; set; }

    public int SectorComplianceRuleId { get; set; }
    public string RuleCategory { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;

    public bool IsUsingDefault { get; set; }

    // Original sector rule config
    public Dictionary<string, object>? SectorRuleConfig { get; set; }

    // Custom rule config (if overridden)
    public Dictionary<string, object>? CustomRuleConfig { get; set; }

    [StringLength(1000)]
    public string? Justification { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
