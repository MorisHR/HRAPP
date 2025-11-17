using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

/// <summary>
/// Feature Flag DTO for reading feature flag configurations
/// </summary>
public class FeatureFlagDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Module { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int RolloutPercentage { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public string? MinimumTier { get; set; }
    public bool IsEmergencyDisabled { get; set; }
    public string? EmergencyDisabledReason { get; set; }
    public DateTime? EmergencyDisabledAt { get; set; }
    public string? EmergencyDisabledBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Request DTO for setting/updating a feature flag
/// </summary>
public class SetFeatureFlagRequest
{
    /// <summary>
    /// Tenant ID - NULL for global default, GUID for tenant-specific override
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Module name: "auth", "dashboard", "employees", "payroll", "attendance", "leaves", etc.
    /// </summary>
    [Required(ErrorMessage = "Module is required")]
    [StringLength(100, ErrorMessage = "Module name cannot exceed 100 characters")]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Whether the feature is enabled
    /// </summary>
    [Required]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Rollout percentage (0-100)
    /// </summary>
    [Required]
    [Range(0, 100, ErrorMessage = "Rollout percentage must be between 0 and 100")]
    public int RolloutPercentage { get; set; }

    /// <summary>
    /// Optional description for documentation
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Optional tags for categorization
    /// </summary>
    [StringLength(200, ErrorMessage = "Tags cannot exceed 200 characters")]
    public string? Tags { get; set; }

    /// <summary>
    /// Minimum tier required (NULL = all tiers)
    /// </summary>
    [StringLength(50, ErrorMessage = "MinimumTier cannot exceed 50 characters")]
    public string? MinimumTier { get; set; }
}

/// <summary>
/// Request DTO for emergency rollback
/// </summary>
public class EmergencyRollbackRequest
{
    /// <summary>
    /// Tenant ID - NULL for global rollback, GUID for tenant-specific rollback
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Module name to rollback
    /// </summary>
    [Required(ErrorMessage = "Module is required")]
    [StringLength(100, ErrorMessage = "Module name cannot exceed 100 characters")]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Reason for emergency rollback (required for audit trail)
    /// </summary>
    [Required(ErrorMessage = "Reason is required for emergency rollback")]
    [StringLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for feature flag operations
/// </summary>
public class FeatureFlagResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FeatureFlagDto? Data { get; set; }
}

/// <summary>
/// Response DTO for getting all feature flags for a tenant
/// </summary>
public class TenantFeatureFlagsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<FeatureFlagDto> FeatureFlags { get; set; } = new();
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
}
