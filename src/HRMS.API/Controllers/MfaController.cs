using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HRMS.Core.Interfaces;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Multi-Factor Authentication (MFA) management API
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MfaController : ControllerBase
{
    private readonly IMfaService _mfaService;
    private readonly ILogger<MfaController> _logger;

    public MfaController(
        IMfaService mfaService,
        ILogger<MfaController> logger)
    {
        _mfaService = mfaService;
        _logger = logger;
    }

    /// <summary>
    /// Admin override to disable MFA for a user (recovery/support scenario)
    /// Requires SuperAdmin role
    /// </summary>
    /// <param name="request">Request containing target user ID and reason</param>
    /// <returns>Success status and message</returns>
    [HttpPost("admin/disable-mfa")]
    [ProducesResponseType(typeof(MfaDisableResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AdminDisableMfa([FromBody] AdminDisableMfaRequest request)
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

            // Get admin user ID from claims
            var adminUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(adminUserIdClaim) || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
            {
                _logger.LogWarning("MFA disable attempted without valid user ID in claims");
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid user session"
                });
            }

            // Call MFA service to disable MFA
            var (success, message) = await _mfaService.AdminDisableMfaAsync(
                adminUserId,
                request.TargetUserId,
                request.Reason);

            if (!success)
            {
                _logger.LogWarning("MFA disable failed for user {TargetUserId} by admin {AdminUserId}. Reason: {Message}",
                    request.TargetUserId, adminUserId, message);

                return BadRequest(new MfaDisableResponse
                {
                    Success = false,
                    Message = message
                });
            }

            _logger.LogInformation("MFA successfully disabled for user {TargetUserId} by admin {AdminUserId}",
                request.TargetUserId, adminUserId);

            return Ok(new MfaDisableResponse
            {
                Success = true,
                Message = message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AdminDisableMfa endpoint");
            return StatusCode(500, new
            {
                success = false,
                message = "An internal error occurred"
            });
        }
    }
}

/// <summary>
/// Request model for admin MFA disable
/// </summary>
public class AdminDisableMfaRequest
{
    /// <summary>
    /// ID of the user to disable MFA for
    /// </summary>
    public Guid TargetUserId { get; set; }

    /// <summary>
    /// Reason for disabling MFA (required for audit trail)
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Response model for MFA disable operation
/// </summary>
public class MfaDisableResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
