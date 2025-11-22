using HRMS.Application.DTOs;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Session Management Controller - Fortune 500 Security Feature
/// Allows users to view and manage their active sessions across devices
/// Pattern: Google Account Security, AWS Console, Microsoft 365
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionManagementController : ControllerBase
{
    private readonly MasterDbContext _context;
    private readonly IAuthService _authService;
    private readonly IDeviceFingerprintService _deviceFingerprintService;
    private readonly ILogger<SessionManagementController> _logger;

    public SessionManagementController(
        MasterDbContext context,
        IAuthService authService,
        IDeviceFingerprintService deviceFingerprintService,
        ILogger<SessionManagementController> logger)
    {
        _context = context;
        _authService = authService;
        _deviceFingerprintService = deviceFingerprintService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active sessions for the current user
    /// FORTUNE 500: Session visibility for security monitoring
    /// </summary>
    /// <returns>List of active sessions with device and location info</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ActiveSessionsResponse), 200)]
    public async Task<IActionResult> GetActiveSessions()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            // Get current session's refresh token from cookie
            var currentRefreshToken = Request.Cookies["refreshToken"];

            // Get all active refresh tokens for this user
            var activeSessions = await _context.RefreshTokens
                .Where(rt => rt.AdminUserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(rt => rt.LastActivityAt)
                .Select(rt => new SessionDto
                {
                    Id = rt.Id,
                    DeviceInfo = "Session", // Will be enriched below
                    IpAddress = rt.CreatedByIp,
                    CreatedAt = rt.CreatedAt,
                    LastActivityAt = rt.LastActivityAt,
                    ExpiresAt = rt.ExpiresAt,
                    IsCurrent = rt.Token == currentRefreshToken,
                    Location = null // Can be enriched with IP geolocation service
                })
                .ToListAsync();

            var maxConcurrentSessions = 3; // Default limit

            var response = new ActiveSessionsResponse
            {
                Sessions = activeSessions,
                TotalActiveSessions = activeSessions.Count,
                MaxConcurrentSessions = maxConcurrentSessions,
                RemainingSlots = Math.Max(0, maxConcurrentSessions - activeSessions.Count)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions");
            return StatusCode(500, new { message = "An error occurred while retrieving active sessions" });
        }
    }

    /// <summary>
    /// Revoke a specific session (logout from a device)
    /// FORTUNE 500: Remote session termination for security
    /// </summary>
    /// <param name="sessionId">Session ID (refresh token ID) to revoke</param>
    /// <returns>Success status</returns>
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RevokeSession(Guid sessionId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var session = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Id == sessionId && rt.AdminUserId == userId);

            if (session == null)
            {
                return NotFound(new { message = "Session not found or does not belong to you" });
            }

            if (session.RevokedAt != null)
            {
                return BadRequest(new { message = "Session is already revoked" });
            }

            // Get client IP for audit trail
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Revoke the session
            await _authService.RevokeTokenAsync(session.Token, clientIp, "Revoked by user via session management");

            _logger.LogInformation(
                "User {UserId} revoked session {SessionId} from {IpAddress}",
                userId, sessionId, session.CreatedByIp);

            return Ok(new
            {
                message = "Session revoked successfully",
                sessionId,
                deviceInfo = session.CreatedByIp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session {SessionId}", sessionId);
            return StatusCode(500, new { message = "An error occurred while revoking the session" });
        }
    }

    /// <summary>
    /// Revoke all sessions except the current one
    /// FORTUNE 500: Security feature for "logout all other devices"
    /// </summary>
    /// <returns>Number of sessions revoked</returns>
    [HttpPost("revoke-all-others")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> RevokeAllOtherSessions()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            // Get current session's refresh token from cookie
            var currentRefreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(currentRefreshToken))
            {
                return BadRequest(new { message = "No active refresh token found" });
            }

            // Get all active sessions except current
            var otherSessions = await _context.RefreshTokens
                .Where(rt => rt.AdminUserId == userId &&
                            rt.RevokedAt == null &&
                            rt.ExpiresAt > DateTime.UtcNow &&
                            rt.Token != currentRefreshToken)
                .ToListAsync();

            // Get client IP for audit trail
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Revoke all other sessions
            foreach (var session in otherSessions)
            {
                session.RevokedAt = DateTime.UtcNow;
                session.RevokedByIp = clientIp;
                session.ReasonRevoked = "Revoked by user - logout all other devices";
            }

            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "User {UserId} revoked {Count} other sessions from {IpAddress}",
                userId, otherSessions.Count, clientIp);

            return Ok(new
            {
                message = $"Successfully revoked {otherSessions.Count} other session(s)",
                sessionsRevoked = otherSessions.Count,
                currentSessionPreserved = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all other sessions");
            return StatusCode(500, new { message = "An error occurred while revoking sessions" });
        }
    }

    /// <summary>
    /// Get current session information
    /// </summary>
    /// <returns>Current session details</returns>
    [HttpGet("current")]
    [ProducesResponseType(typeof(SessionDto), 200)]
    public async Task<IActionResult> GetCurrentSession()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            // Get current session's refresh token from cookie
            var currentRefreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(currentRefreshToken))
            {
                return BadRequest(new { message = "No active refresh token found" });
            }

            var currentSession = await _context.RefreshTokens
                .Where(rt => rt.Token == currentRefreshToken && rt.AdminUserId == userId)
                .Select(rt => new SessionDto
                {
                    Id = rt.Id,
                    DeviceInfo = _deviceFingerprintService.GetDeviceInfo(HttpContext),
                    IpAddress = rt.CreatedByIp,
                    CreatedAt = rt.CreatedAt,
                    LastActivityAt = rt.LastActivityAt,
                    ExpiresAt = rt.ExpiresAt,
                    IsCurrent = true,
                    Location = null
                })
                .FirstOrDefaultAsync();

            if (currentSession == null)
            {
                return NotFound(new { message = "Current session not found" });
            }

            return Ok(currentSession);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current session");
            return StatusCode(500, new { message = "An error occurred while retrieving session information" });
        }
    }
}
