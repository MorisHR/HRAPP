using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Core.Interfaces;
using HRMS.Core.Constants;
using HRMS.API.Attributes;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Feature Flag Management API
/// FORTUNE 500 PATTERN: Per-tenant feature control with gradual rollout and emergency rollback
/// SECURITY: SuperAdmin only - requires proper permissions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class FeatureFlagController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<FeatureFlagController> _logger;

    public FeatureFlagController(
        IFeatureFlagService featureFlagService,
        ITenantService tenantService,
        ILogger<FeatureFlagController> logger)
    {
        _featureFlagService = featureFlagService;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all feature flags for the current tenant (from HTTP context)
    /// Used by frontend to check enabled features
    /// </summary>
    [HttpGet("current-tenant")]
    [AllowAnonymous] // Allow tenants to check their own flags
    [ProducesResponseType(typeof(TenantFeatureFlagsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentTenantFeatureFlags()
    {
        try
        {
            // Resolve tenant from HTTP context (TenantResolutionMiddleware)
            var tenantId = _tenantService.GetCurrentTenantId();

            if (!tenantId.HasValue)
            {
                return NotFound(new
                {
                    success = false,
                    message = "No tenant context found. Feature flags require tenant identification."
                });
            }

            var featureFlags = await _featureFlagService.GetFeatureFlagsForTenantAsync(tenantId.Value);

            return Ok(new TenantFeatureFlagsResponse
            {
                Success = true,
                Message = "Feature flags retrieved successfully",
                FeatureFlags = featureFlags,
                TenantId = tenantId.Value,
                TenantName = null // TenantName not available in ITenantService
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature flags for current tenant");
            return StatusCode(500, new
            {
                success = false,
                message = "Error retrieving feature flags"
            });
        }
    }

    /// <summary>
    /// Gets all feature flags for a specific tenant (SuperAdmin)
    /// </summary>
    [HttpGet("tenant/{tenantId}")]
    [RequirePermission(Permissions.SYSTEM_SETTINGS)]
    [ProducesResponseType(typeof(TenantFeatureFlagsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantFeatureFlags(Guid tenantId)
    {
        try
        {
            var featureFlags = await _featureFlagService.GetFeatureFlagsForTenantAsync(tenantId);

            return Ok(new TenantFeatureFlagsResponse
            {
                Success = true,
                Message = "Feature flags retrieved successfully",
                FeatureFlags = featureFlags,
                TenantId = tenantId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature flags for tenant {TenantId}", tenantId);
            return StatusCode(500, new
            {
                success = false,
                message = "Error retrieving feature flags"
            });
        }
    }

    /// <summary>
    /// Gets all global default feature flags (SuperAdmin)
    /// </summary>
    [HttpGet("global")]
    [RequirePermission(Permissions.SYSTEM_SETTINGS)]
    [ProducesResponseType(typeof(TenantFeatureFlagsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGlobalFeatureFlags()
    {
        try
        {
            var featureFlags = await _featureFlagService.GetGlobalFeatureFlagsAsync();

            return Ok(new TenantFeatureFlagsResponse
            {
                Success = true,
                Message = "Global feature flags retrieved successfully",
                FeatureFlags = featureFlags,
                TenantId = null,
                TenantName = "Global Default"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving global feature flags");
            return StatusCode(500, new
            {
                success = false,
                message = "Error retrieving feature flags"
            });
        }
    }

    /// <summary>
    /// Gets a specific feature flag
    /// </summary>
    [HttpGet("{tenantId}/{module}")]
    [RequirePermission(Permissions.SYSTEM_SETTINGS)]
    [ProducesResponseType(typeof(FeatureFlagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFeatureFlag(Guid? tenantId, string module)
    {
        try
        {
            var featureFlag = await _featureFlagService.GetFeatureFlagAsync(tenantId, module);

            if (featureFlag == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Feature flag not found for module: {module}"
                });
            }

            return Ok(new FeatureFlagResponse
            {
                Success = true,
                Message = "Feature flag retrieved successfully",
                Data = featureFlag
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature flag: {Module}", module);
            return StatusCode(500, new
            {
                success = false,
                message = "Error retrieving feature flag"
            });
        }
    }

    /// <summary>
    /// Checks if a feature is enabled (useful for testing)
    /// </summary>
    [HttpGet("check/{module}")]
    [RequirePermission(Permissions.SYSTEM_SETTINGS)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckFeatureEnabled(string module, [FromQuery] Guid? tenantId = null, [FromQuery] Guid? userId = null)
    {
        try
        {
            var isEnabled = await _featureFlagService.IsFeatureEnabledAsync(tenantId, module, userId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    module,
                    tenantId,
                    userId,
                    isEnabled
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature flag: {Module}", module);
            return StatusCode(500, new
            {
                success = false,
                message = "Error checking feature flag"
            });
        }
    }

    /// <summary>
    /// Sets or updates a feature flag (SuperAdmin only)
    /// Creates new flag if doesn't exist, updates if exists
    /// </summary>
    [HttpPost("set")]
    [RequirePermission(Permissions.SYSTEM_SETTINGS)]
    [ProducesResponseType(typeof(FeatureFlagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetFeatureFlag([FromBody] SetFeatureFlagRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            // Get SuperAdmin email from claims
            var performedBy = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";

            var featureFlag = await _featureFlagService.SetFeatureFlagAsync(request, performedBy);

            _logger.LogInformation(
                "Feature flag set: {Module} for tenant {TenantId} by {PerformedBy}",
                request.Module, request.TenantId, performedBy);

            return Ok(new FeatureFlagResponse
            {
                Success = true,
                Message = "Feature flag updated successfully",
                Data = featureFlag
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting feature flag");
            return StatusCode(500, new
            {
                success = false,
                message = "Error setting feature flag"
            });
        }
    }

    /// <summary>
    /// EMERGENCY ROLLBACK: Immediately disables a feature
    /// CRITICAL OPERATION: Sets IsEmergencyDisabled flag for instant effect
    /// Use this when a feature is causing production issues
    /// </summary>
    [HttpPost("emergency-rollback")]
    [RequirePermission(Permissions.SYSTEM_SETTINGS)]
    [ProducesResponseType(typeof(FeatureFlagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EmergencyRollback([FromBody] EmergencyRollbackRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            // Get SuperAdmin email from claims
            var performedBy = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";

            var featureFlag = await _featureFlagService.EmergencyRollbackAsync(request, performedBy);

            _logger.LogCritical(
                "EMERGENCY ROLLBACK: Feature {Module} disabled for tenant {TenantId} by {PerformedBy}. Reason: {Reason}",
                request.Module, request.TenantId, performedBy, request.Reason);

            return Ok(new FeatureFlagResponse
            {
                Success = true,
                Message = "Emergency rollback completed successfully",
                Data = featureFlag
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing emergency rollback");
            return StatusCode(500, new
            {
                success = false,
                message = "Error performing emergency rollback"
            });
        }
    }

    /// <summary>
    /// Re-enables a feature after emergency rollback
    /// </summary>
    [HttpPost("re-enable")]
    [RequirePermission(Permissions.SYSTEM_SETTINGS)]
    [ProducesResponseType(typeof(FeatureFlagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReEnableAfterEmergency([FromQuery] Guid? tenantId, [FromQuery] string module)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(module))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Module parameter is required"
                });
            }

            // Get SuperAdmin email from claims
            var performedBy = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";

            var featureFlag = await _featureFlagService.ReEnableAfterEmergencyAsync(tenantId, module, performedBy);

            _logger.LogInformation(
                "Feature {Module} re-enabled after emergency rollback for tenant {TenantId} by {PerformedBy}",
                module, tenantId, performedBy);

            return Ok(new FeatureFlagResponse
            {
                Success = true,
                Message = "Feature re-enabled successfully",
                Data = featureFlag
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error re-enabling feature");
            return StatusCode(500, new
            {
                success = false,
                message = "Error re-enabling feature"
            });
        }
    }

    /// <summary>
    /// Deletes a feature flag (soft delete)
    /// </summary>
    [HttpDelete("{tenantId}/{module}")]
    [RequirePermission(Permissions.SYSTEM_SETTINGS)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFeatureFlag(Guid? tenantId, string module)
    {
        try
        {
            // Get SuperAdmin email from claims
            var performedBy = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";

            var deleted = await _featureFlagService.DeleteFeatureFlagAsync(tenantId, module, performedBy);

            if (!deleted)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feature flag not found"
                });
            }

            _logger.LogInformation(
                "Feature flag deleted: {Module} for tenant {TenantId} by {PerformedBy}",
                module, tenantId, performedBy);

            return Ok(new
            {
                success = true,
                message = "Feature flag deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feature flag");
            return StatusCode(500, new
            {
                success = false,
                message = "Error deleting feature flag"
            });
        }
    }

    /// <summary>
    /// Clears feature flag cache (useful after bulk updates)
    /// </summary>
    [HttpPost("clear-cache")]
    [RequirePermission(Permissions.SYSTEM_SETTINGS)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ClearCache()
    {
        try
        {
            _featureFlagService.ClearCache();

            _logger.LogInformation("Feature flag cache cleared by {User}", User.FindFirstValue(ClaimTypes.Email));

            return Ok(new
            {
                success = true,
                message = "Feature flag cache cleared successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing feature flag cache");
            return StatusCode(500, new
            {
                success = false,
                message = "Error clearing cache"
            });
        }
    }
}
