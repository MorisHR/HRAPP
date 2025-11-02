namespace HRMS.Application.DTOs.SectorDtos;

/// <summary>
/// Compliance report for tenant showing effective rules
/// </summary>
public class SectorComplianceReportDto
{
    public int SectorId { get; set; }
    public string SectorName { get; set; } = string.Empty;
    public string SectorCode { get; set; } = string.Empty;

    public DateTime ReportGeneratedAt { get; set; }

    // Effective rules by category
    public Dictionary<string, EffectiveRuleDto> EffectiveRules { get; set; } = new();

    public ComplianceSummaryDto Summary { get; set; } = new();
}

/// <summary>
/// Effective rule details
/// </summary>
public class EffectiveRuleDto
{
    public string RuleCategory { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public bool IsCustomized { get; set; }
    public Dictionary<string, object> RuleConfig { get; set; } = new();
    public string Source { get; set; } = string.Empty; // "Sector Default" or "Custom"
    public string? Justification { get; set; }
    public DateTime EffectiveFrom { get; set; }
}

/// <summary>
/// Compliance summary statistics
/// </summary>
public class ComplianceSummaryDto
{
    public int TotalRules { get; set; }
    public int DefaultRules { get; set; }
    public int CustomizedRules { get; set; }
    public DateTime LastCustomizationDate { get; set; }

    // Validation warnings
    public List<string> Warnings { get; set; } = new();

    // Compliance status
    public bool IsFullyCompliant { get; set; }
    public List<string> NonComplianceReasons { get; set; } = new();
}
