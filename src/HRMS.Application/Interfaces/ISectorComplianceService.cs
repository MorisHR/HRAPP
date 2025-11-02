using HRMS.Application.DTOs.SectorDtos;
using System.Text.Json;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Service for managing sector compliance rules and tenant customizations
/// </summary>
public interface ISectorComplianceService
{
    /// <summary>
    /// Get effective compliance rules for a tenant (combines sector defaults with tenant customizations)
    /// </summary>
    Task<Dictionary<string, Dictionary<string, object>>> GetEffectiveRulesForTenantAsync(string tenantSchemaName);

    /// <summary>
    /// Get effective rule for a specific category
    /// </summary>
    Task<Dictionary<string, object>?> GetEffectiveRuleByCategoryAsync(string tenantSchemaName, string ruleCategory);

    /// <summary>
    /// Validate if a custom rule configuration is legally compliant (cannot be worse than sector minimum)
    /// </summary>
    Task<(bool IsValid, List<string> Errors)> ValidateCustomRuleAsync(int sectorId, string ruleCategory, Dictionary<string, object> customConfig);

    /// <summary>
    /// Apply custom compliance rule for tenant (within legal limits)
    /// </summary>
    Task ApplyCustomRuleAsync(string tenantSchemaName, int sectorComplianceRuleId, Dictionary<string, object> customConfig, string justification, Guid approvedByUserId);

    /// <summary>
    /// Reset tenant custom rule to sector default
    /// </summary>
    Task ResetToSectorDefaultAsync(string tenantSchemaName, int sectorComplianceRuleId);

    /// <summary>
    /// Generate compliance report for tenant showing all effective rules
    /// </summary>
    Task<SectorComplianceReportDto> GenerateComplianceReportAsync(string tenantSchemaName);

    /// <summary>
    /// Notify tenants when a sector rule changes (for affected tenants)
    /// </summary>
    Task NotifyTenantsOfRuleChangeAsync(int sectorId, string ruleCategory);

    /// <summary>
    /// Get tenant's sector configuration
    /// </summary>
    Task<TenantSectorConfigDto?> GetTenantSectorConfigAsync(string tenantSchemaName);

    /// <summary>
    /// Get all custom compliance rules for a tenant
    /// </summary>
    Task<List<CustomComplianceRuleDto>> GetTenantCustomRulesAsync(string tenantSchemaName);
}
