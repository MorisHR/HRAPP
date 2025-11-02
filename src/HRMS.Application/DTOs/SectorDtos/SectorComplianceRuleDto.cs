using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace HRMS.Application.DTOs.SectorDtos;

/// <summary>
/// Sector compliance rule details
/// </summary>
public class SectorComplianceRuleDto
{
    public int Id { get; set; }

    public int SectorId { get; set; }
    public string SectorName { get; set; } = string.Empty;
    public string SectorCode { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string RuleCategory { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string RuleName { get; set; } = string.Empty;

    [Required]
    public Dictionary<string, object> RuleConfig { get; set; } = new();

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    [StringLength(500)]
    public string? LegalReference { get; set; }

    public bool IsActive { get; set; }
    public bool IsCurrent { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
