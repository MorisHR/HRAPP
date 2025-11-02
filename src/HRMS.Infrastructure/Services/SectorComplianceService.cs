using HRMS.Application.DTOs.SectorDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for managing sector compliance rules and tenant customizations
/// </summary>
public class SectorComplianceService : ISectorComplianceService
{
    private readonly MasterDbContext _masterContext;
    private readonly ILogger<SectorComplianceService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SectorComplianceService(
        MasterDbContext masterContext,
        ILogger<SectorComplianceService> logger,
        IServiceProvider serviceProvider)
    {
        _masterContext = masterContext;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<Dictionary<string, Dictionary<string, object>>> GetEffectiveRulesForTenantAsync(string tenantSchemaName)
    {
        _logger.LogInformation("Getting effective compliance rules for tenant: {TenantSchema}", tenantSchemaName);

        // This is a placeholder - full implementation would query tenant context
        return new Dictionary<string, Dictionary<string, object>>();
    }

    public async Task<Dictionary<string, object>?> GetEffectiveRuleByCategoryAsync(string tenantSchemaName, string ruleCategory)
    {
        _logger.LogInformation("Getting effective rule for tenant {TenantSchema}, category: {Category}", tenantSchemaName, ruleCategory);

        // Placeholder implementation
        return null;
    }

    public async Task<(bool IsValid, List<string> Errors)> ValidateCustomRuleAsync(int sectorId, string ruleCategory, Dictionary<string, object> customConfig)
    {
        _logger.LogInformation("Validating custom rule for sector {SectorId}, category: {Category}", sectorId, ruleCategory);

        var errors = new List<string>();

        // Get sector default rule
        var now = DateTime.UtcNow;
        var sectorRule = await _masterContext.SectorComplianceRules
            .Where(r => r.SectorId == sectorId && r.RuleCategory == ruleCategory)
            .Where(r => r.EffectiveFrom <= now && (r.EffectiveTo == null || r.EffectiveTo >= now))
            .FirstOrDefaultAsync();

        if (sectorRule == null)
        {
            errors.Add($"No sector rule found for category: {ruleCategory}");
            return (false, errors);
        }

        // Parse sector config
        var sectorConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(
            sectorRule.RuleConfig) ?? new();

        // Validate based on category
        switch (ruleCategory)
        {
            case "OVERTIME":
                errors.AddRange(ValidateOvertimeRule(sectorConfig, customConfig));
                break;
            case "MINIMUM_WAGE":
                errors.AddRange(ValidateMinimumWageRule(sectorConfig, customConfig));
                break;
            case "WORKING_HOURS":
                errors.AddRange(ValidateWorkingHoursRule(sectorConfig, customConfig));
                break;
        }

        return (errors.Count == 0, errors);
    }

    public async Task ApplyCustomRuleAsync(string tenantSchemaName, int sectorComplianceRuleId, Dictionary<string, object> customConfig, string justification, Guid approvedByUserId)
    {
        _logger.LogInformation("Applying custom rule for tenant {TenantSchema}, rule ID: {RuleId}", tenantSchemaName, sectorComplianceRuleId);

        // Full implementation would write to tenant context
        // Placeholder for now
        await Task.CompletedTask;
    }

    public async Task ResetToSectorDefaultAsync(string tenantSchemaName, int sectorComplianceRuleId)
    {
        _logger.LogInformation("Resetting to sector default for tenant {TenantSchema}, rule ID: {RuleId}", tenantSchemaName, sectorComplianceRuleId);

        // Placeholder
        await Task.CompletedTask;
    }

    public async Task<SectorComplianceReportDto> GenerateComplianceReportAsync(string tenantSchemaName)
    {
        _logger.LogInformation("Generating compliance report for tenant: {TenantSchema}", tenantSchemaName);

        return new SectorComplianceReportDto
        {
            ReportGeneratedAt = DateTime.UtcNow,
            Summary = new ComplianceSummaryDto
            {
                IsFullyCompliant = true
            }
        };
    }

    public async Task NotifyTenantsOfRuleChangeAsync(int sectorId, string ruleCategory)
    {
        _logger.LogInformation("Notifying tenants of rule change for sector {SectorId}, category: {Category}", sectorId, ruleCategory);

        // Placeholder - would send notifications
        await Task.CompletedTask;
    }

    public async Task<TenantSectorConfigDto?> GetTenantSectorConfigAsync(string tenantSchemaName)
    {
        _logger.LogInformation("Getting tenant sector config for: {TenantSchema}", tenantSchemaName);

        // Placeholder
        return null;
    }

    public async Task<List<CustomComplianceRuleDto>> GetTenantCustomRulesAsync(string tenantSchemaName)
    {
        _logger.LogInformation("Getting custom rules for tenant: {TenantSchema}", tenantSchemaName);

        return new List<CustomComplianceRuleDto>();
    }

    // Validation helpers
    private List<string> ValidateOvertimeRule(Dictionary<string, object> sectorConfig, Dictionary<string, object> customConfig)
    {
        var errors = new List<string>();

        // Tenant cannot set overtime rates LOWER than sector minimum
        if (customConfig.TryGetValue("weekday_overtime_rate", out var weekdayRate))
        {
            var sectorWeekdayRate = GetDecimalValue(sectorConfig, "weekday_overtime_rate");
            var customWeekdayRate = Convert.ToDecimal(weekdayRate);

            if (customWeekdayRate < sectorWeekdayRate)
            {
                errors.Add($"Weekday overtime rate ({customWeekdayRate}) cannot be lower than sector minimum ({sectorWeekdayRate})");
            }
        }

        if (customConfig.TryGetValue("weekend_overtime_rate", out var weekendRate))
        {
            var sectorWeekendRate = GetDecimalValue(sectorConfig, "weekend_overtime_rate");
            var customWeekendRate = Convert.ToDecimal(weekendRate);

            if (customWeekendRate < sectorWeekendRate)
            {
                errors.Add($"Weekend overtime rate ({customWeekendRate}) cannot be lower than sector minimum ({sectorWeekendRate})");
            }
        }

        return errors;
    }

    private List<string> ValidateMinimumWageRule(Dictionary<string, object> sectorConfig, Dictionary<string, object> customConfig)
    {
        var errors = new List<string>();

        if (customConfig.TryGetValue("monthly_minimum_wage_mur", out var customWage))
        {
            var sectorMinWage = GetDecimalValue(sectorConfig, "monthly_minimum_wage_mur");
            var customMinWage = Convert.ToDecimal(customWage);

            if (customMinWage < sectorMinWage)
            {
                errors.Add($"Minimum wage (MUR {customMinWage}) cannot be lower than sector requirement (MUR {sectorMinWage})");
            }
        }

        return errors;
    }

    private List<string> ValidateWorkingHoursRule(Dictionary<string, object> sectorConfig, Dictionary<string, object> customConfig)
    {
        var errors = new List<string>();

        if (customConfig.TryGetValue("standard_weekly_hours", out var customHours))
        {
            var sectorMaxHours = GetDecimalValue(sectorConfig, "standard_weekly_hours");
            var customWeeklyHours = Convert.ToDecimal(customHours);

            if (customWeeklyHours > sectorMaxHours)
            {
                errors.Add($"Standard weekly hours ({customWeeklyHours}) cannot exceed sector maximum ({sectorMaxHours})");
            }
        }

        return errors;
    }

    private decimal GetDecimalValue(Dictionary<string, object> config, string key)
    {
        if (config.TryGetValue(key, out var value))
        {
            return Convert.ToDecimal(value);
        }
        return 0;
    }
}
