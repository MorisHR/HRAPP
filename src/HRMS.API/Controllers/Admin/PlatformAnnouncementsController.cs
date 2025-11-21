using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Infrastructure.Data;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace HRMS.API.Controllers.Admin;

/// <summary>
/// Platform Announcements Management API
/// FORTUNE 500 PATTERN: Salesforce In-App Notifications, AWS Service Health Dashboard
/// SECURITY: SuperAdmin role ONLY - Platform-wide communications
/// </summary>
[ApiController]
[Route("admin/announcements")]
[Authorize(Roles = "SuperAdmin")]
public class PlatformAnnouncementsController : ControllerBase
{
    private readonly MasterDbContext _context;
    private readonly ILogger<PlatformAnnouncementsController> _logger;

    public PlatformAnnouncementsController(
        MasterDbContext context,
        ILogger<PlatformAnnouncementsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all announcements with filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PlatformAnnouncement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnnouncements(
        [FromQuery] bool? isActive = null,
        [FromQuery] AnnouncementType? type = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = _context.PlatformAnnouncements.AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(a => a.IsActive == isActive.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(a => a.Type == type.Value);
            }

            var totalCount = await query.CountAsync();

            var announcements = await query
                .OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.StartDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = announcements,
                pagination = new
                {
                    currentPage = pageNumber,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve announcements");
            return StatusCode(500, new { success = false, error = "Failed to retrieve announcements" });
        }
    }

    /// <summary>
    /// Get active announcements for current user
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<PlatformAnnouncement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveAnnouncements()
    {
        try
        {
            var now = DateTime.UtcNow;

            var announcements = await _context.PlatformAnnouncements
                .Where(a => a.IsActive &&
                           a.StartDate <= now &&
                           (a.EndDate == null || a.EndDate >= now))
                .OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.StartDate)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = announcements
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active announcements");
            return StatusCode(500, new { success = false, error = "Failed to retrieve announcements" });
        }
    }

    /// <summary>
    /// Create a new announcement
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PlatformAnnouncement), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementRequest request)
    {
        try
        {
            var announcement = new PlatformAnnouncement
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Audience = request.Audience,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = request.IsActive,
                IsDismissible = request.IsDismissible,
                TargetTenantIds = request.TargetTenantIds,
                ActionUrl = request.ActionUrl,
                ActionText = request.ActionText,
                Priority = request.Priority,
                CreatedBy = GetUserId(),
                CreatedAt = DateTime.UtcNow
            };

            _context.PlatformAnnouncements.Add(announcement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Platform announcement created: {Title} by {User}",
                request.Title, User.Identity?.Name);

            return CreatedAtAction(
                nameof(GetAnnouncement),
                new { id = announcement.Id },
                new
                {
                    success = true,
                    data = announcement,
                    message = "Announcement created successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create announcement");
            return StatusCode(500, new { success = false, error = "Failed to create announcement" });
        }
    }

    /// <summary>
    /// Get announcement by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PlatformAnnouncement), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnnouncement(Guid id)
    {
        try
        {
            var announcement = await _context.PlatformAnnouncements.FindAsync(id);

            if (announcement == null)
            {
                return NotFound(new { success = false, error = "Announcement not found" });
            }

            return Ok(new
            {
                success = true,
                data = announcement
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve announcement {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to retrieve announcement" });
        }
    }

    /// <summary>
    /// Update an announcement
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PlatformAnnouncement), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAnnouncement(Guid id, [FromBody] UpdateAnnouncementRequest request)
    {
        try
        {
            var announcement = await _context.PlatformAnnouncements.FindAsync(id);

            if (announcement == null)
            {
                return NotFound(new { success = false, error = "Announcement not found" });
            }

            announcement.Title = request.Title;
            announcement.Message = request.Message;
            announcement.Type = request.Type;
            announcement.Audience = request.Audience;
            announcement.StartDate = request.StartDate;
            announcement.EndDate = request.EndDate;
            announcement.IsActive = request.IsActive;
            announcement.IsDismissible = request.IsDismissible;
            announcement.TargetTenantIds = request.TargetTenantIds;
            announcement.ActionUrl = request.ActionUrl;
            announcement.ActionText = request.ActionText;
            announcement.Priority = request.Priority;
            announcement.UpdatedBy = GetUserId();
            announcement.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Platform announcement updated: {Id} by {User}", id, User.Identity?.Name);

            return Ok(new
            {
                success = true,
                data = announcement,
                message = "Announcement updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update announcement {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to update announcement" });
        }
    }

    /// <summary>
    /// Delete an announcement
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAnnouncement(Guid id)
    {
        try
        {
            var announcement = await _context.PlatformAnnouncements.FindAsync(id);

            if (announcement == null)
            {
                return NotFound(new { success = false, error = "Announcement not found" });
            }

            _context.PlatformAnnouncements.Remove(announcement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Platform announcement deleted: {Id} by {User}", id, User.Identity?.Name);

            return Ok(new
            {
                success = true,
                message = "Announcement deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete announcement {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to delete announcement" });
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

public class CreateAnnouncementRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public AnnouncementType Type { get; set; } = AnnouncementType.INFO;
    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.ALL;

    [Required]
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDismissible { get; set; } = true;
    public string? TargetTenantIds { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public int Priority { get; set; } = 0;
}

public class UpdateAnnouncementRequest : CreateAnnouncementRequest
{
}
