using Microsoft.AspNetCore.Mvc;
using HRMS.Application.DTOs;
using HRMS.Core.Interfaces;

namespace HRMS.API.Controllers;

/// <summary>
/// Authentication API for Super Admin login
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Super Admin Login - Generates JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and admin user details</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
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

            var result = await _authService.LoginAsync(request.Email, request.Password);

            if (result == null)
            {
                _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid email or password"
                });
            }

            _logger.LogInformation("Successful login for admin user: {Email}", request.Email);

            var response = new LoginResponse
            {
                Token = result.Value.Token,
                ExpiresAt = result.Value.ExpiresAt,
                AdminUser = new AdminUserDto
                {
                    Id = result.Value.User.Id,
                    UserName = result.Value.User.UserName,
                    Email = result.Value.User.Email,
                    IsActive = result.Value.User.IsActive,
                    LastLoginDate = result.Value.User.LastLoginDate,
                    IsTwoFactorEnabled = result.Value.User.IsTwoFactorEnabled,
                    CreatedAt = result.Value.User.CreatedAt
                }
            };

            return Ok(new
            {
                success = true,
                data = response,
                message = "Login successful"
            });
        }
        catch (InvalidOperationException ex)
        {
            // Account lockout exception
            _logger.LogWarning(ex, "Account lockout for email: {Email}", request.Email);
            return StatusCode(423, new // 423 Locked
            {
                success = false,
                message = ex.Message,
                locked = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred during login"
            });
        }
    }

    /// <summary>
    /// Unlock a locked admin account (SuperAdmin only)
    /// </summary>
    [HttpPost("unlock/{userId}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockAccount(Guid userId)
    {
        try
        {
            var result = await _authService.UnlockAccountAsync(userId);

            if (!result)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found"
                });
            }

            _logger.LogInformation("Account unlocked for user: {UserId}", userId);

            return Ok(new
            {
                success = true,
                message = "Account unlocked successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking account for user: {UserId}", userId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while unlocking the account"
            });
        }
    }

    /// <summary>
    /// Check if authentication is working (Protected endpoint test)
    /// </summary>
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new
        {
            success = true,
            message = "Auth endpoint is working",
            timestamp = DateTime.UtcNow
        });
    }
}
