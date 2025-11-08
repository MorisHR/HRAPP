using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HRMS.Application.DTOs;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Services;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Authentication API for Super Admin and Tenant Employee login
/// Implements production-grade JWT token refresh with HttpOnly cookies
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly TenantAuthService _tenantAuthService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        TenantAuthService tenantAuthService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _tenantAuthService = tenantAuthService;
        _logger = logger;
    }

    /// <summary>
    /// Super Admin Login - Generates JWT token + refresh token
    /// UPDATED: Now sets refresh token as HttpOnly cookie
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT access token and admin user details</returns>
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

            // Get client IP address for security tracking
            var ipAddress = GetIpAddress();

            var result = await _authService.LoginAsync(request.Email, request.Password, ipAddress);

            if (result == null)
            {
                _logger.LogWarning("Failed login attempt for email: {Email} from IP: {IpAddress}", request.Email, ipAddress);
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid email or password"
                });
            }

            _logger.LogInformation("Successful login for admin user: {Email} from IP: {IpAddress}", request.Email, ipAddress);

            // ============================================
            // PRODUCTION-GRADE: Set refresh token as HttpOnly cookie
            // ============================================
            SetRefreshTokenCookie(result.Value.RefreshToken);

            var response = new LoginResponse
            {
                Token = result.Value.Token,
                RefreshToken = result.Value.RefreshToken, // Also return in response for clients that can't access cookies
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

    // ============================================
    // PRODUCTION-GRADE MFA ENDPOINTS
    // Secret URL for SuperAdmin login + mandatory MFA
    // ============================================

    /// <summary>
    /// SECRET URL SuperAdmin Login - Step 1: Email/Password verification
    /// Returns MFA setup or verification requirement
    /// </summary>
    [HttpPost("system-9f7a2b4c-3d8e-4a1b-8c9d-1e2f3a4b5c6d")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SecretLogin([FromBody] LoginRequest request)
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

            var ipAddress = GetIpAddress();
            var result = await _authService.LoginAsync(request.Email, request.Password, ipAddress);

            if (result == null)
            {
                _logger.LogWarning("Failed secret login attempt for email: {Email} from IP: {IpAddress}", request.Email, ipAddress);
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid email or password"
                });
            }

            var adminUser = result.Value.User;

            // FIRST LOGIN: MFA not set up yet → Force MFA setup
            if (!adminUser.IsTwoFactorEnabled)
            {
                _logger.LogInformation("First login for user {Email}, initiating MFA setup", request.Email);

                var mfaSetup = await _authService.SetupMfaAsync(adminUser.Id, adminUser.Email);

                if (mfaSetup == null)
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Failed to setup MFA"
                    });
                }

                return Ok(new
                {
                    success = true,
                    requiresMfaSetup = true,
                    data = new
                    {
                        userId = adminUser.Id,
                        email = adminUser.Email,
                        qrCode = mfaSetup.Value.QrCodeBase64,
                        secret = mfaSetup.Value.Secret,
                        backupCodes = mfaSetup.Value.BackupCodes
                    },
                    message = "Please set up two-factor authentication to continue"
                });
            }

            // SUBSEQUENT LOGINS: MFA already enabled → Require MFA verification
            _logger.LogInformation("MFA verification required for user {Email}", request.Email);

            return Ok(new
            {
                success = true,
                requiresMfaVerification = true,
                data = new
                {
                    userId = adminUser.Id,
                    email = adminUser.Email
                },
                message = "Please enter your 6-digit TOTP code"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Account lockout for email: {Email}", request.Email);
            return StatusCode(423, new
            {
                success = false,
                message = ex.Message,
                locked = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during secret login for email: {Email}", request.Email);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred during login"
            });
        }
    }

    /// <summary>
    /// Complete MFA setup - Step 2 after scanning QR code
    /// Verifies TOTP code and saves MFA configuration
    /// </summary>
    [HttpPost("mfa/complete-setup")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteMfaSetup([FromBody] CompleteMfaSetupRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data"
                });
            }

            var result = await _authService.CompleteMfaSetupAsync(request.UserId, request.TotpCode, request.Secret, request.BackupCodes);

            if (!result)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid TOTP code. Please try again."
                });
            }

            // After successful MFA setup, generate tokens
            var adminUser = await _authService.GetAdminUserAsync(request.UserId);
            if (adminUser == null)
            {
                return StatusCode(500, new { success = false, message = "User not found" });
            }

            var ipAddress = GetIpAddress();
            var (token, refreshToken, expiresAt) = _authService.GenerateTokens(adminUser, ipAddress);

            SetRefreshTokenCookie(refreshToken);

            _logger.LogInformation("MFA setup completed for user {UserId}", request.UserId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    token,
                    refreshToken,
                    expiresAt,
                    adminUser = new
                    {
                        id = adminUser.Id,
                        userName = adminUser.UserName,
                        email = adminUser.Email,
                        isTwoFactorEnabled = adminUser.IsTwoFactorEnabled
                    }
                },
                message = "MFA setup completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing MFA setup");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred during MFA setup"
            });
        }
    }

    /// <summary>
    /// Verify MFA code - Step 2 for subsequent logins
    /// Supports both TOTP codes and backup codes
    /// </summary>
    [HttpPost("mfa/verify")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data"
                });
            }

            var adminUser = await _authService.GetAdminUserAsync(request.UserId);
            if (adminUser == null || !adminUser.IsTwoFactorEnabled)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "MFA not enabled for this user"
                });
            }

            bool isValid = false;
            bool usedBackupCode = false;

            // Try TOTP code first
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                isValid = await _authService.ValidateMfaAsync(request.UserId, request.Code);

                // If TOTP failed, try as backup code
                if (!isValid && request.Code.Length == 8) // Backup codes are 8 characters
                {
                    isValid = await _authService.ValidateBackupCodeAsync(request.UserId, request.Code);
                    if (isValid)
                    {
                        usedBackupCode = true;
                        var remaining = await _authService.GetRemainingBackupCodesAsync(request.UserId);
                        _logger.LogWarning("Backup code used for user {UserId}. Remaining codes: {Remaining}", request.UserId, remaining);
                    }
                }
            }

            if (!isValid)
            {
                _logger.LogWarning("MFA verification failed for user {UserId}", request.UserId);
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid verification code"
                });
            }

            // Generate tokens after successful MFA
            var ipAddress = GetIpAddress();
            var (token, refreshToken, expiresAt) = _authService.GenerateTokens(adminUser, ipAddress);

            SetRefreshTokenCookie(refreshToken);

            _logger.LogInformation("MFA verification successful for user {UserId}", request.UserId);

            var response = new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                AdminUser = new AdminUserDto
                {
                    Id = adminUser.Id,
                    UserName = adminUser.UserName,
                    Email = adminUser.Email,
                    IsActive = adminUser.IsActive,
                    LastLoginDate = adminUser.LastLoginDate,
                    IsTwoFactorEnabled = adminUser.IsTwoFactorEnabled,
                    CreatedAt = adminUser.CreatedAt
                }
            };

            var responseData = new
            {
                success = true,
                data = response,
                message = usedBackupCode
                    ? $"Login successful using backup code. {await _authService.GetRemainingBackupCodesAsync(request.UserId)} backup codes remaining."
                    : "Login successful"
            };

            return Ok(responseData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying MFA");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred during MFA verification"
            });
        }
    }

    /// <summary>
    /// Tenant Employee Login - Generates JWT token with tenant context
    /// </summary>
    /// <param name="request">Tenant login credentials with subdomain</param>
    /// <returns>JWT token and employee details</returns>
    [HttpPost("tenant/login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TenantLogin([FromBody] TenantLoginRequest request)
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

            var result = await _tenantAuthService.LoginAsync(request.Email, request.Password, request.Subdomain);

            if (result == null)
            {
                _logger.LogWarning("Failed tenant login attempt for email: {Email}, subdomain: {Subdomain}",
                    request.Email, request.Subdomain);
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid email, password, or tenant"
                });
            }

            _logger.LogInformation("Successful tenant login for employee: {Email}, tenant: {Subdomain}",
                request.Email, request.Subdomain);

            var response = new
            {
                Token = result.Value.Token,
                ExpiresAt = result.Value.ExpiresAt,
                Employee = new
                {
                    Id = result.Value.User.Id,
                    EmployeeCode = result.Value.User.EmployeeCode,
                    FullName = result.Value.User.FullName,
                    Email = result.Value.User.Email,
                    JobTitle = result.Value.User.JobTitle,
                    DepartmentName = result.Value.User.Department?.Name,
                    IsActive = result.Value.User.IsActive
                },
                TenantId = result.Value.TenantId,
                Subdomain = request.Subdomain
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
            // Account lockout or tenant inactive exception
            _logger.LogWarning(ex, "Tenant login error for email: {Email}", request.Email);
            return StatusCode(423, new // 423 Locked
            {
                success = false,
                message = ex.Message,
                locked = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tenant login for email: {Email}, subdomain: {Subdomain}",
                request.Email, request.Subdomain);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred during login"
            });
        }
    }

    // ============================================
    // PRODUCTION-GRADE TOKEN REFRESH ENDPOINTS
    // ============================================

    /// <summary>
    /// Refresh access token using refresh token from HttpOnly cookie
    /// Implements token rotation: old refresh token revoked, new one issued
    /// </summary>
    /// <returns>New access token and refresh token</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            // Get refresh token from HttpOnly cookie
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Refresh token not found in cookie");
                return Unauthorized(new
                {
                    success = false,
                    message = "Refresh token not found. Please login again."
                });
            }

            var ipAddress = GetIpAddress();

            // Refresh the token (implements rotation)
            var (accessToken, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

            // Set new refresh token as HttpOnly cookie
            SetRefreshTokenCookie(newRefreshToken);

            _logger.LogInformation("Token refreshed successfully from IP: {IpAddress}", ipAddress);

            return Ok(new
            {
                success = true,
                data = new
                {
                    token = accessToken,
                    refreshToken = newRefreshToken,
                    expiresAt = DateTime.UtcNow.AddMinutes(15) // Access token expiration
                },
                message = "Token refreshed successfully"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Token refresh failed: {Message}", ex.Message);
            return Unauthorized(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new
            {
                success = false,
                message = "Token refresh failed"
            });
        }
    }

    /// <summary>
    /// Revoke a refresh token (logout from current device)
    /// </summary>
    /// <param name="request">Optional token to revoke (uses cookie if not provided)</param>
    /// <returns>Success message</returns>
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest? request = null)
    {
        try
        {
            // Get token from request body or cookie
            var refreshToken = request?.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Refresh token required"
                });
            }

            var ipAddress = GetIpAddress();
            await _authService.RevokeTokenAsync(refreshToken, ipAddress, "Revoked by user (logout)");

            // Clear the refresh token cookie
            Response.Cookies.Delete("refreshToken");

            _logger.LogInformation("Token revoked from IP: {IpAddress}", ipAddress);

            return Ok(new
            {
                success = true,
                message = "Token revoked successfully. You have been logged out."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return StatusCode(500, new
            {
                success = false,
                message = "Error revoking token"
            });
        }
    }

    /// <summary>
    /// Revoke all refresh tokens for current user (logout from all devices)
    /// Requires authentication
    /// </summary>
    /// <returns>Success message</returns>
    [HttpPost("revoke-all")]
    [Authorize] // Requires valid JWT
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAllTokens()
    {
        try
        {
            // Get user ID from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid user token"
                });
            }

            var ipAddress = GetIpAddress();
            await _authService.RevokeAllTokensAsync(userId, ipAddress, "Revoked by user (logout all devices)");

            // Clear the refresh token cookie for current device
            Response.Cookies.Delete("refreshToken");

            _logger.LogInformation("All tokens revoked for user: {UserId} from IP: {IpAddress}", userId, ipAddress);

            return Ok(new
            {
                success = true,
                message = "All refresh tokens revoked successfully. You have been logged out from all devices."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all tokens");
            return StatusCode(500, new
            {
                success = false,
                message = "Error revoking tokens"
            });
        }
    }

    /// <summary>
    /// Unlock a locked admin account (SuperAdmin only)
    /// </summary>
    [HttpPost("unlock/{userId}")]
    [Authorize(Roles = "SuperAdmin")]
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

    // ============================================
    // PRIVATE HELPER METHODS
    // ============================================

    /// <summary>
    /// Sets refresh token as HttpOnly cookie
    /// SECURITY: HttpOnly prevents JavaScript access (XSS protection)
    /// SECURITY: Secure ensures HTTPS-only transmission
    /// SECURITY: SameSite prevents CSRF attacks
    /// </summary>
    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // Cannot be accessed by JavaScript (XSS protection)
            Secure = !HttpContext.Request.Host.Host.Contains("localhost"), // HTTPS only in production
            SameSite = SameSiteMode.Lax, // CSRF protection (Strict would break OAuth flows)
            Expires = DateTimeOffset.UtcNow.AddDays(7), // 7 days expiration
            Path = "/", // Available for all paths
            Domain = GetCookieDomain(), // Subdomain support
            IsEssential = true // Required for authentication
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

        _logger.LogDebug("Refresh token cookie set with domain: {Domain}, secure: {Secure}",
            cookieOptions.Domain ?? "null", cookieOptions.Secure);
    }

    /// <summary>
    /// Gets cookie domain for subdomain support
    /// Development: null (allows localhost and *.localhost)
    /// Production: .morishr.com (allows morishr.com and *.morishr.com)
    /// </summary>
    private string? GetCookieDomain()
    {
        var host = Request.Host.Host;

        // Development: localhost or *.localhost
        if (host.Contains("localhost"))
        {
            return null; // No domain restriction for localhost
        }

        // Production: Extract base domain for wildcard subdomain support
        var parts = host.Split('.');

        // If host is already base domain (e.g., morishr.com), use it with leading dot
        if (parts.Length == 2)
        {
            return $".{host}"; // .morishr.com
        }

        // If subdomain exists (e.g., acme.morishr.com), extract base domain
        if (parts.Length >= 3)
        {
            var baseDomain = string.Join(".", parts.TakeLast(2)); // morishr.com
            return $".{baseDomain}"; // .morishr.com (allows all subdomains)
        }

        return null; // Fallback: no domain restriction
    }

    /// <summary>
    /// Gets client IP address for security tracking
    /// Handles X-Forwarded-For header for proxies/load balancers
    /// </summary>
    private string GetIpAddress()
    {
        // Check X-Forwarded-For header (set by reverse proxies)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Take first IP (client IP)
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    return ips[0].Trim();
                }
            }
        }

        // Fallback to direct connection IP
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Request model for token revocation
/// </summary>
public class RevokeTokenRequest
{
    public string? Token { get; set; }
}

/// <summary>
/// Request model for completing MFA setup
/// </summary>
public class CompleteMfaSetupRequest
{
    public Guid UserId { get; set; }
    public string TotpCode { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public List<string> BackupCodes { get; set; } = new();
}

/// <summary>
/// Request model for verifying MFA code
/// </summary>
public class VerifyMfaRequest
{
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
}
