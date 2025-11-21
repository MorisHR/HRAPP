using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Infrastructure.Data;
using HRMS.Core.Entities.Master;
using System.ComponentModel.DataAnnotations;

namespace HRMS.API.Controllers.Admin;

/// <summary>
/// System Settings Management API
/// FORTUNE 500 PATTERN: Salesforce Setup, AWS Systems Manager, ServiceNow Administration
/// SECURITY: SuperAdmin role ONLY - System-wide configuration
/// </summary>
[ApiController]
[Route("admin/system-settings")]
[Authorize(Roles = "SuperAdmin")]
public class SystemSettingsController : ControllerBase
{
    private readonly MasterDbContext _context;
    private readonly ILogger<SystemSettingsController> _logger;

    public SystemSettingsController(
        MasterDbContext context,
        ILogger<SystemSettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all system settings grouped by category
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SystemSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSettings()
    {
        try
        {
            var settings = await _context.SystemSettings
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Key)
                .ToListAsync();

            var grouped = settings.GroupBy(s => s.Category)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );

            return Ok(new
            {
                success = true,
                data = grouped,
                message = "System settings retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve system settings");
            return StatusCode(500, new { success = false, error = "Failed to retrieve system settings" });
        }
    }

    /// <summary>
    /// Get settings by category
    /// </summary>
    [HttpGet("{category}")]
    [ProducesResponseType(typeof(List<SystemSetting>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettingsByCategory(string category)
    {
        try
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == category)
                .OrderBy(s => s.Key)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = settings,
                message = $"Settings for category '{category}' retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve settings for category {Category}", category);
            return StatusCode(500, new { success = false, error = "Failed to retrieve settings" });
        }
    }

    /// <summary>
    /// Update a system setting
    /// </summary>
    [HttpPut("{key}")]
    [ProducesResponseType(typeof(SystemSetting), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest request)
    {
        try
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                return NotFound(new { success = false, error = $"Setting '{key}' not found" });
            }

            if (setting.IsReadOnly)
            {
                return BadRequest(new { success = false, error = "This setting is read-only" });
            }

            setting.Value = request.Value;
            setting.UpdatedBy = GetUserId();
            setting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("System setting {Key} updated to {Value} by {User}",
                key, request.Value, User.Identity?.Name);

            return Ok(new
            {
                success = true,
                data = setting,
                message = "Setting updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update setting {Key}", key);
            return StatusCode(500, new { success = false, error = "Failed to update setting" });
        }
    }

    /// <summary>
    /// Toggle maintenance mode
    /// </summary>
    [HttpPost("maintenance/toggle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleMaintenanceMode([FromBody] MaintenanceModeRequest request)
    {
        try
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "MaintenanceMode");

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    Key = "MaintenanceMode",
                    Category = "System",
                    Description = "Enable/disable maintenance mode",
                    DataType = "boolean",
                    CreatedBy = GetUserId(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.SystemSettings.Add(setting);
            }

            setting.Value = request.IsEnabled.ToString().ToLower();
            setting.UpdatedBy = GetUserId();
            setting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogWarning("Maintenance mode {Status} by {User}. Reason: {Reason}",
                request.IsEnabled ? "ENABLED" : "DISABLED",
                User.Identity?.Name,
                request.Reason);

            return Ok(new
            {
                success = true,
                data = new
                {
                    isEnabled = request.IsEnabled,
                    reason = request.Reason
                },
                message = $"Maintenance mode {(request.IsEnabled ? "enabled" : "disabled")}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle maintenance mode");
            return StatusCode(500, new { success = false, error = "Failed to toggle maintenance mode" });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return Guid.Empty;
    }
}

// ═══════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════

public class SystemSettingsResponse
{
    public Dictionary<string, List<SystemSetting>> Settings { get; set; } = new();
}

public class UpdateSettingRequest
{
    [Required]
    public string Value { get; set; } = string.Empty;
}

public class MaintenanceModeRequest
{
    public bool IsEnabled { get; set; }
    public string? Reason { get; set; }
}
