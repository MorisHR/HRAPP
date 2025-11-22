using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using HRMS.API.Services;
using HRMS.Core.Entities.Master;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HRMS.API.Controllers;

/// <summary>
/// Fortune 500-grade tenant impersonation API
/// Allows SuperAdmins to temporarily assume tenant user identity
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class ImpersonationController : ControllerBase
{
    private readonly IImpersonationService _impersonationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImpersonationController> _logger;

    public ImpersonationController(
        IImpersonationService impersonationService,
        IConfiguration configuration,
        ILogger<ImpersonationController> logger)
    {
        _impersonationService = impersonationService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Start impersonating a tenant user
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartImpersonation([FromBody] StartImpersonationRequest request)
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminUserId))
            {
                return Unauthorized(new { message = "Admin user ID not found in token" });
            }

            // Start impersonation session
            var session = await _impersonationService.StartImpersonationAsync(
                adminUserId,
                request.TargetUserId,
                request.Reason);

            // Generate new JWT token with impersonation claims
            var impersonationToken = GenerateImpersonationToken(session, adminUserId);

            return Ok(new StartImpersonationResponse
            {
                Success = true,
                Message = $"Successfully started impersonating {session.TargetUserEmail}",
                SessionId = session.Id,
                Token = impersonationToken,
                TargetUser = new TargetUserInfo
                {
                    Id = session.TargetUserId,
                    Email = session.TargetUserEmail,
                    UserName = session.TargetUserName,
                    TenantId = session.TenantId
                },
                StartedAt = session.StartedAt
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start impersonation");
            return StatusCode(500, new { message = "Failed to start impersonation. Please try again." });
        }
    }

    /// <summary>
    /// Stop current impersonation session
    /// </summary>
    [HttpPost("stop")]
    public async Task<IActionResult> StopImpersonation([FromBody] StopImpersonationRequest request)
    {
        try
        {
            await _impersonationService.StopImpersonationAsync(request.SessionId);

            return Ok(new
            {
                Success = true,
                Message = "Impersonation session stopped successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop impersonation");
            return StatusCode(500, new { message = "Failed to stop impersonation. Please try again." });
        }
    }

    /// <summary>
    /// Get current impersonation session
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentSession()
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminUserId))
            {
                return Unauthorized(new { message = "Admin user ID not found in token" });
            }

            var session = await _impersonationService.GetCurrentSessionAsync(adminUserId);

            if (session == null)
            {
                return Ok(new { isImpersonating = false });
            }

            return Ok(new
            {
                isImpersonating = true,
                sessionId = session.Id,
                targetUser = new
                {
                    id = session.TargetUserId,
                    email = session.TargetUserEmail,
                    userName = session.TargetUserName,
                    tenantId = session.TenantId
                },
                startedAt = session.StartedAt,
                reason = session.Reason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current impersonation session");
            return StatusCode(500, new { message = "Failed to retrieve session information" });
        }
    }

    /// <summary>
    /// Get impersonation history (audit trail)
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? adminUserId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            // If not filtering by specific admin, only allow SuperAdmins to see all
            if (string.IsNullOrEmpty(adminUserId))
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                adminUserId = currentUserId;
            }

            var (sessions, totalCount) = await _impersonationService.GetImpersonationHistoryAsync(
                pageIndex,
                pageSize,
                adminUserId,
                fromDate,
                toDate);

            return Ok(new
            {
                data = sessions,
                pagination = new
                {
                    pageIndex,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve impersonation history");
            return StatusCode(500, new { message = "Failed to retrieve impersonation history" });
        }
    }

    private string GenerateImpersonationToken(ImpersonationSession session, string adminUserId)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT secret not configured");
        var issuer = jwtSettings["Issuer"] ?? "HRMS.API";
        var audience = jwtSettings["Audience"] ?? "HRMS.Client";
        var expirationHours = int.Parse(jwtSettings["ExpirationHours"] ?? "24");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            // Target user claims (the impersonated user)
            new Claim(ClaimTypes.NameIdentifier, session.TargetUserId.ToString()),
            new Claim(ClaimTypes.Name, session.TargetUserName),
            new Claim(ClaimTypes.Email, session.TargetUserEmail),

            // Impersonation metadata
            new Claim("impersonating", "true"),
            new Claim("actual_admin_id", adminUserId),
            new Claim("impersonation_session_id", session.Id),
            new Claim("impersonation_reason", session.Reason),

            // Tenant context
            new Claim("tenant_id", session.TenantId?.ToString() ?? ""),

            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// DTOs
public class StartImpersonationRequest
{
    public Guid TargetUserId { get; set; }
    public string Reason { get; set; } = null!;
}

public class StopImpersonationRequest
{
    public string SessionId { get; set; } = null!;
}

public class StartImpersonationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public string SessionId { get; set; } = null!;
    public string Token { get; set; } = null!;
    public TargetUserInfo TargetUser { get; set; } = null!;
    public DateTime StartedAt { get; set; }
}

public class TargetUserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public Guid? TenantId { get; set; }
}
