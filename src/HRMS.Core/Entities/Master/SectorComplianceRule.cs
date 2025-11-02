using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Sector-specific compliance rules based on Mauritius Labour Laws
/// Stored in Master Schema, referenced by tenants
/// </summary>
public class SectorComplianceRule : IntIdBaseEntity
{
    public int SectorId { get; set; }

    /// <summary>
    /// OVERTIME, MINIMUM_WAGE, WORKING_HOURS, BREAKS, ALLOWANCES, LEAVE, GRATUITY
    /// </summary>
    public string RuleCategory { get; set; } = string.Empty;

    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Flexible JSON configuration for different rule types
    /// Examples:
    /// - Overtime: { "weekday_overtime_rate": 1.5, "sunday_rate": 2.0, ... }
    /// - Minimum Wage: { "monthly_minimum_wage_mur": 17110, ... }
    /// - Working Hours: { "standard_weekly_hours": 45, ... }
    /// </summary>
    public string RuleConfig { get; set; } = "{}";

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public string? LegalReference { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual IndustrySector? Sector { get; set; }
}
