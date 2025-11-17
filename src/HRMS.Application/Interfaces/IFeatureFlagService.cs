using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Feature Flag Service Interface
/// Manages per-tenant feature flags with gradual rollout and emergency rollback
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Gets all feature flags for a specific tenant
    /// Returns tenant-specific overrides merged with global defaults
    /// </summary>
    /// <param name="tenantId">Tenant ID (required)</param>
    /// <returns>List of effective feature flags for the tenant</returns>
    Task<List<FeatureFlagDto>> GetFeatureFlagsForTenantAsync(Guid tenantId);

    /// <summary>
    /// Gets a specific feature flag for a tenant
    /// Returns tenant-specific override if exists, otherwise global default
    /// </summary>
    /// <param name="tenantId">Tenant ID (null = global default)</param>
    /// <param name="module">Module name</param>
    /// <returns>Feature flag DTO or null if not found</returns>
    Task<FeatureFlagDto?> GetFeatureFlagAsync(Guid? tenantId, string module);

    /// <summary>
    /// Checks if a feature is enabled for a tenant
    /// Respects rollout percentage and emergency disable flags
    /// Uses deterministic user-based rollout (same user always gets same result)
    /// </summary>
    /// <param name="tenantId">Tenant ID (null = global check)</param>
    /// <param name="module">Module name</param>
    /// <param name="userId">Optional user ID for rollout percentage calculation</param>
    /// <returns>True if feature is enabled for this tenant/user</returns>
    Task<bool> IsFeatureEnabledAsync(Guid? tenantId, string module, Guid? userId = null);

    /// <summary>
    /// Sets or updates a feature flag
    /// Creates new flag if doesn't exist, updates if exists
    /// Automatically logs audit trail
    /// </summary>
    /// <param name="request">Feature flag configuration</param>
    /// <param name="performedBy">SuperAdmin email who performed the action</param>
    /// <returns>Updated feature flag DTO</returns>
    Task<FeatureFlagDto> SetFeatureFlagAsync(SetFeatureFlagRequest request, string performedBy);

    /// <summary>
    /// Emergency rollback: Immediately disables a feature
    /// Sets IsEmergencyDisabled flag and logs audit trail
    /// Takes precedence over IsEnabled flag
    /// </summary>
    /// <param name="request">Emergency rollback request</param>
    /// <param name="performedBy">SuperAdmin email who triggered rollback</param>
    /// <returns>Updated feature flag DTO</returns>
    Task<FeatureFlagDto> EmergencyRollbackAsync(EmergencyRollbackRequest request, string performedBy);

    /// <summary>
    /// Re-enables a feature after emergency rollback
    /// Clears IsEmergencyDisabled flag
    /// </summary>
    /// <param name="tenantId">Tenant ID (null = global)</param>
    /// <param name="module">Module name</param>
    /// <param name="performedBy">SuperAdmin email who re-enabled</param>
    /// <returns>Updated feature flag DTO</returns>
    Task<FeatureFlagDto> ReEnableAfterEmergencyAsync(Guid? tenantId, string module, string performedBy);

    /// <summary>
    /// Gets all global default feature flags
    /// </summary>
    /// <returns>List of global feature flags (TenantId = NULL)</returns>
    Task<List<FeatureFlagDto>> GetGlobalFeatureFlagsAsync();

    /// <summary>
    /// Deletes a feature flag (soft delete)
    /// </summary>
    /// <param name="tenantId">Tenant ID (null = global)</param>
    /// <param name="module">Module name</param>
    /// <param name="performedBy">SuperAdmin email who deleted</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteFeatureFlagAsync(Guid? tenantId, string module, string performedBy);

    /// <summary>
    /// Clears feature flag cache
    /// Call after updating flags to ensure immediate effect
    /// </summary>
    void ClearCache();
}
