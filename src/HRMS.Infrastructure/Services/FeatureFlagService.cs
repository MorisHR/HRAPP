using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Feature Flag Service Implementation
/// FORTUNE 500 PATTERN: Per-tenant feature control with caching and gradual rollout
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly MasterDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FeatureFlagService> _logger;
    private readonly IAuditLogService _auditLogService;

    // CONCURRENCY FIX: Thread-local SHA256 for high-performance hash computation
    // Avoids creating new instances on every call under concurrent load
    private static readonly ThreadLocal<SHA256> _sha256ThreadLocal = new ThreadLocal<SHA256>(() => SHA256.Create());

    private const string CACHE_KEY_PREFIX = "FeatureFlag_";
    private const int CACHE_DURATION_MINUTES = 5;

    public FeatureFlagService(
        MasterDbContext context,
        IMemoryCache cache,
        ILogger<FeatureFlagService> logger,
        IAuditLogService auditLogService)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Gets all feature flags for a specific tenant
    /// Returns tenant-specific overrides merged with global defaults
    /// </summary>
    public async Task<List<FeatureFlagDto>> GetFeatureFlagsForTenantAsync(Guid tenantId)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}Tenant_{tenantId}";

        if (_cache.TryGetValue(cacheKey, out List<FeatureFlagDto>? cachedFlags) && cachedFlags != null)
        {
            _logger.LogDebug("Feature flags cache hit for tenant {TenantId}", tenantId);
            return cachedFlags;
        }

        _logger.LogDebug("Feature flags cache miss for tenant {TenantId}", tenantId);

        // Get all global defaults
        var globalFlags = await _context.FeatureFlagsConfig
            .Where(f => f.TenantId == null && !f.IsDeleted)
            .ToListAsync();

        // Get tenant-specific overrides
        var tenantFlags = await _context.FeatureFlagsConfig
            .Where(f => f.TenantId == tenantId && !f.IsDeleted)
            .ToListAsync();

        // Merge: tenant-specific overrides take precedence
        var mergedFlags = new List<FeatureFlagDto>();

        // Add all tenant-specific flags first
        foreach (var flag in tenantFlags)
        {
            mergedFlags.Add(MapToDto(flag));
        }

        // Add global flags that don't have tenant-specific overrides
        var tenantModules = tenantFlags.Select(f => f.Module).ToHashSet();
        foreach (var flag in globalFlags)
        {
            if (!tenantModules.Contains(flag.Module))
            {
                mergedFlags.Add(MapToDto(flag));
            }
        }

        // Cache for 5 minutes
        _cache.Set(cacheKey, mergedFlags, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

        return mergedFlags;
    }

    /// <summary>
    /// Gets a specific feature flag for a tenant
    /// Returns tenant-specific override if exists, otherwise global default
    /// </summary>
    public async Task<FeatureFlagDto?> GetFeatureFlagAsync(Guid? tenantId, string module)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}{tenantId?.ToString() ?? "Global"}_{module}";

        if (_cache.TryGetValue(cacheKey, out FeatureFlagDto? cachedFlag) && cachedFlag != null)
        {
            return cachedFlag;
        }

        FeatureFlag? flag;

        if (tenantId.HasValue)
        {
            // Try to get tenant-specific override first
            flag = await _context.FeatureFlagsConfig
                .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Module == module && !f.IsDeleted);

            // If not found, fall back to global default
            if (flag == null)
            {
                flag = await _context.FeatureFlagsConfig
                    .FirstOrDefaultAsync(f => f.TenantId == null && f.Module == module && !f.IsDeleted);
            }
        }
        else
        {
            // Get global default
            flag = await _context.FeatureFlagsConfig
                .FirstOrDefaultAsync(f => f.TenantId == null && f.Module == module && !f.IsDeleted);
        }

        if (flag == null)
        {
            return null;
        }

        var dto = MapToDto(flag);
        _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

        return dto;
    }

    /// <summary>
    /// Checks if a feature is enabled for a tenant
    /// Respects rollout percentage and emergency disable flags
    /// Uses deterministic user-based rollout (same user always gets same result)
    /// </summary>
    public async Task<bool> IsFeatureEnabledAsync(Guid? tenantId, string module, Guid? userId = null)
    {
        var flag = await GetFeatureFlagAsync(tenantId, module);

        if (flag == null)
        {
            // Feature not configured = disabled by default (safe default)
            return false;
        }

        // Emergency disable takes precedence over everything
        if (flag.IsEmergencyDisabled)
        {
            _logger.LogWarning(
                "Feature {Module} is emergency disabled for tenant {TenantId}. Reason: {Reason}",
                module, tenantId, flag.EmergencyDisabledReason);
            return false;
        }

        // If feature is disabled, return false
        if (!flag.IsEnabled)
        {
            return false;
        }

        // If rollout is 100%, return true
        if (flag.RolloutPercentage >= 100)
        {
            return true;
        }

        // If rollout is 0%, return false
        if (flag.RolloutPercentage <= 0)
        {
            return false;
        }

        // Gradual rollout: Use deterministic hash-based rollout
        // This ensures the same user always gets the same result
        if (userId.HasValue)
        {
            var rolloutKey = $"{tenantId}_{module}_{userId}";
            var hash = ComputeRolloutHash(rolloutKey);
            var userPercentile = hash % 100; // 0-99

            return userPercentile < flag.RolloutPercentage;
        }

        // No userId provided: use tenant-level rollout
        if (tenantId.HasValue)
        {
            var rolloutKey = $"{tenantId}_{module}";
            var hash = ComputeRolloutHash(rolloutKey);
            var tenantPercentile = hash % 100; // 0-99

            return tenantPercentile < flag.RolloutPercentage;
        }

        // No tenant or user: return based on percentage (random-ish)
        return flag.RolloutPercentage >= 50; // Default threshold
    }

    /// <summary>
    /// Sets or updates a feature flag
    /// Creates new flag if doesn't exist, updates if exists
    /// </summary>
    public async Task<FeatureFlagDto> SetFeatureFlagAsync(SetFeatureFlagRequest request, string performedBy)
    {
        // Check if flag exists
        var existingFlag = await _context.FeatureFlagsConfig
            .FirstOrDefaultAsync(f => f.TenantId == request.TenantId && f.Module == request.Module);

        FeatureFlag flag;
        bool isNew = false;

        if (existingFlag != null)
        {
            // Update existing flag
            var oldValues = new
            {
                existingFlag.IsEnabled,
                existingFlag.RolloutPercentage,
                existingFlag.Description,
                existingFlag.Tags,
                existingFlag.MinimumTier
            };

            existingFlag.IsEnabled = request.IsEnabled;
            existingFlag.RolloutPercentage = request.RolloutPercentage;
            existingFlag.Description = request.Description;
            existingFlag.Tags = request.Tags;
            existingFlag.MinimumTier = request.MinimumTier;
            existingFlag.UpdatedAt = DateTime.UtcNow;
            existingFlag.UpdatedBy = performedBy;

            flag = existingFlag;

            _logger.LogInformation(
                "Feature flag updated: {Module} for tenant {TenantId} by {PerformedBy}",
                request.Module, request.TenantId, performedBy);

            // Audit log
            await _auditLogService.LogActionAsync(
                AuditActionType.FEATURE_FLAG_UPDATED,
                AuditCategory.FEATURE_FLAGS,
                AuditSeverity.INFO,
                "FeatureFlag",
                flag.Id,
                System.Text.Json.JsonSerializer.Serialize(oldValues),
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    request.IsEnabled,
                    request.RolloutPercentage,
                    request.Description,
                    request.Tags,
                    request.MinimumTier
                }),
                success: true,
                reason: $"Feature flag updated for module: {request.Module}"
            );
        }
        else
        {
            // Create new flag
            flag = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Module = request.Module,
                IsEnabled = request.IsEnabled,
                RolloutPercentage = request.RolloutPercentage,
                Description = request.Description,
                Tags = request.Tags,
                MinimumTier = request.MinimumTier,
                IsEmergencyDisabled = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = performedBy
            };

            _context.FeatureFlagsConfig.Add(flag);
            isNew = true;

            _logger.LogInformation(
                "Feature flag created: {Module} for tenant {TenantId} by {PerformedBy}",
                request.Module, request.TenantId, performedBy);

            // Audit log
            await _auditLogService.LogActionAsync(
                AuditActionType.FEATURE_FLAG_CREATED,
                AuditCategory.FEATURE_FLAGS,
                AuditSeverity.INFO,
                "FeatureFlag",
                flag.Id,
                null,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    request.IsEnabled,
                    request.RolloutPercentage,
                    request.Description,
                    request.Tags,
                    request.MinimumTier
                }),
                success: true,
                reason: $"Feature flag created for module: {request.Module}"
            );
        }

        await _context.SaveChangesAsync();

        // Clear cache
        ClearCacheForFlag(request.TenantId, request.Module);

        return MapToDto(flag);
    }

    /// <summary>
    /// Emergency rollback: Immediately disables a feature
    /// </summary>
    public async Task<FeatureFlagDto> EmergencyRollbackAsync(EmergencyRollbackRequest request, string performedBy)
    {
        var flag = await _context.FeatureFlagsConfig
            .FirstOrDefaultAsync(f => f.TenantId == request.TenantId && f.Module == request.Module && !f.IsDeleted);

        if (flag == null)
        {
            throw new InvalidOperationException($"Feature flag not found: {request.Module} for tenant {request.TenantId}");
        }

        var oldValues = new
        {
            flag.IsEmergencyDisabled,
            flag.EmergencyDisabledReason,
            flag.EmergencyDisabledAt,
            flag.EmergencyDisabledBy
        };

        flag.IsEmergencyDisabled = true;
        flag.EmergencyDisabledReason = request.Reason;
        flag.EmergencyDisabledAt = DateTime.UtcNow;
        flag.EmergencyDisabledBy = performedBy;
        flag.UpdatedAt = DateTime.UtcNow;
        flag.UpdatedBy = performedBy;

        await _context.SaveChangesAsync();

        _logger.LogWarning(
            "EMERGENCY ROLLBACK: Feature {Module} disabled for tenant {TenantId} by {PerformedBy}. Reason: {Reason}",
            request.Module, request.TenantId, performedBy, request.Reason);

        // Audit log with CRITICAL severity
        await _auditLogService.LogActionAsync(
            AuditActionType.FEATURE_FLAG_EMERGENCY_ROLLBACK,
            AuditCategory.FEATURE_FLAGS,
            AuditSeverity.CRITICAL,
            "FeatureFlag",
            flag.Id,
            System.Text.Json.JsonSerializer.Serialize(oldValues),
            System.Text.Json.JsonSerializer.Serialize(new
            {
                flag.IsEmergencyDisabled,
                flag.EmergencyDisabledReason,
                flag.EmergencyDisabledAt,
                flag.EmergencyDisabledBy
            }),
            success: true,
            reason: $"EMERGENCY ROLLBACK: {request.Reason}"
        );

        // Clear cache immediately
        ClearCacheForFlag(request.TenantId, request.Module);

        return MapToDto(flag);
    }

    /// <summary>
    /// Re-enables a feature after emergency rollback
    /// </summary>
    public async Task<FeatureFlagDto> ReEnableAfterEmergencyAsync(Guid? tenantId, string module, string performedBy)
    {
        var flag = await _context.FeatureFlagsConfig
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Module == module && !f.IsDeleted);

        if (flag == null)
        {
            throw new InvalidOperationException($"Feature flag not found: {module} for tenant {tenantId}");
        }

        if (!flag.IsEmergencyDisabled)
        {
            throw new InvalidOperationException($"Feature {module} is not emergency disabled");
        }

        flag.IsEmergencyDisabled = false;
        flag.EmergencyDisabledReason = null;
        flag.EmergencyDisabledAt = null;
        flag.EmergencyDisabledBy = null;
        flag.UpdatedAt = DateTime.UtcNow;
        flag.UpdatedBy = performedBy;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Feature {Module} re-enabled after emergency rollback for tenant {TenantId} by {PerformedBy}",
            module, tenantId, performedBy);

        // Audit log
        await _auditLogService.LogActionAsync(
            AuditActionType.FEATURE_FLAG_RE_ENABLED,
            AuditCategory.FEATURE_FLAGS,
            AuditSeverity.WARNING,
            "FeatureFlag",
            flag.Id,
            System.Text.Json.JsonSerializer.Serialize(new { IsEmergencyDisabled = true }),
            System.Text.Json.JsonSerializer.Serialize(new { IsEmergencyDisabled = false }),
            success: true,
            reason: $"Re-enabled feature {module} after emergency rollback"
        );

        // Clear cache
        ClearCacheForFlag(tenantId, module);

        return MapToDto(flag);
    }

    /// <summary>
    /// Gets all global default feature flags
    /// </summary>
    public async Task<List<FeatureFlagDto>> GetGlobalFeatureFlagsAsync()
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}Global_All";

        if (_cache.TryGetValue(cacheKey, out List<FeatureFlagDto>? cachedFlags) && cachedFlags != null)
        {
            return cachedFlags;
        }

        var flags = await _context.FeatureFlagsConfig
            .Where(f => f.TenantId == null && !f.IsDeleted)
            .ToListAsync();

        var dtos = flags.Select(MapToDto).ToList();

        _cache.Set(cacheKey, dtos, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

        return dtos;
    }

    /// <summary>
    /// Deletes a feature flag (soft delete)
    /// </summary>
    public async Task<bool> DeleteFeatureFlagAsync(Guid? tenantId, string module, string performedBy)
    {
        var flag = await _context.FeatureFlagsConfig
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Module == module && !f.IsDeleted);

        if (flag == null)
        {
            return false;
        }

        flag.IsDeleted = true;
        flag.DeletedAt = DateTime.UtcNow;
        flag.DeletedBy = performedBy;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Feature flag deleted: {Module} for tenant {TenantId} by {PerformedBy}",
            module, tenantId, performedBy);

        // Audit log
        await _auditLogService.LogActionAsync(
            AuditActionType.FEATURE_FLAG_DELETED,
            AuditCategory.FEATURE_FLAGS,
            AuditSeverity.WARNING,
            "FeatureFlag",
            flag.Id,
            System.Text.Json.JsonSerializer.Serialize(new { flag.Module, flag.IsEnabled }),
            null,
            success: true,
            reason: $"Feature flag deleted for module: {module}"
        );

        // Clear cache
        ClearCacheForFlag(tenantId, module);

        return true;
    }

    /// <summary>
    /// Clears feature flag cache
    /// </summary>
    public void ClearCache()
    {
        _logger.LogInformation("Clearing all feature flag cache");
        // Note: IMemoryCache doesn't have a clear all method
        // In production, consider using Redis with key pattern scanning
        // For now, cache will auto-expire after 5 minutes
    }

    // ============================================
    // PRIVATE HELPER METHODS
    // ============================================

    private FeatureFlagDto MapToDto(FeatureFlag flag)
    {
        return new FeatureFlagDto
        {
            Id = flag.Id,
            TenantId = flag.TenantId,
            Module = flag.Module,
            IsEnabled = flag.IsEnabled,
            RolloutPercentage = flag.RolloutPercentage,
            Description = flag.Description,
            Tags = flag.Tags,
            MinimumTier = flag.MinimumTier,
            IsEmergencyDisabled = flag.IsEmergencyDisabled,
            EmergencyDisabledReason = flag.EmergencyDisabledReason,
            EmergencyDisabledAt = flag.EmergencyDisabledAt,
            EmergencyDisabledBy = flag.EmergencyDisabledBy,
            CreatedAt = flag.CreatedAt,
            UpdatedAt = flag.UpdatedAt,
            CreatedBy = flag.CreatedBy,
            UpdatedBy = flag.UpdatedBy
        };
    }

    private void ClearCacheForFlag(Guid? tenantId, string module)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}{tenantId?.ToString() ?? "Global"}_{module}";
        _cache.Remove(cacheKey);

        if (tenantId.HasValue)
        {
            var tenantCacheKey = $"{CACHE_KEY_PREFIX}Tenant_{tenantId}";
            _cache.Remove(tenantCacheKey);
        }

        var globalCacheKey = $"{CACHE_KEY_PREFIX}Global_All";
        _cache.Remove(globalCacheKey);

        _logger.LogDebug("Cache cleared for feature flag: {Module}, Tenant: {TenantId}", module, tenantId);
    }

    /// <summary>
    /// Computes a deterministic hash for rollout percentage
    /// Same input always produces same output (stable rollout)
    /// CONCURRENCY FIX: Uses thread-local SHA256 for better performance under load
    /// </summary>
    private int ComputeRolloutHash(string key)
    {
        // Use thread-local SHA256 instance - thread-safe and performant
        var sha256 = _sha256ThreadLocal.Value!;
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        // Take first 4 bytes and convert to int
        return Math.Abs(BitConverter.ToInt32(hashBytes, 0));
    }
}
