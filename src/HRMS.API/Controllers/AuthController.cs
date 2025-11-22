using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using HRMS.Application.DTOs;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Services;
using System.Security.Claims;
using HRMS.Application.Interfaces;

namespace HRMS.API.Controllers;

/// <summary>
/// Authentication API for Super Admin and Tenant Employee login
/// Implements production-grade JWT token refresh with HttpOnly cookies
/// FORTRESS-GRADE SECURITY: Rate limiting, subdomain validation, password complexity, CSRF protection
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly TenantAuthService _tenantAuthService;
    private readonly IRateLimitService _rateLimitService;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<AuthController> _logger;
    private readonly string _secretPath;

    public AuthController(
        IAuthService authService,
        TenantAuthService tenantAuthService,
        IRateLimitService rateLimitService,
        IAntiforgery antiforgery,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _tenantAuthService = tenantAuthService;
        _rateLimitService = rateLimitService;
        _antiforgery = antiforgery;
        _logger = logger;
        _secretPath = configuration["Auth:SuperAdminSecretPath"]
            ?? throw new InvalidOperationException("Auth:SuperAdminSecretPath configuration not set");
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
    // CSRF PROTECTION (FORTUNE 500 COMPLIANCE)
    // ============================================

    /// <summary>
    /// Get CSRF token for state-changing operations
    /// SECURITY: Returns antiforgery token to prevent CSRF attacks
    /// This endpoint is called by the frontend on app initialization
    /// </summary>
    [HttpGet("csrf-token")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetCsrfToken()
    {
        try
        {
            // Generate and return CSRF token
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

            _logger.LogDebug("CSRF token generated for IP: {IpAddress}", GetIpAddress());

            return Ok(new
            {
                success = true,
                token = tokens.RequestToken,
                message = "CSRF token generated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating CSRF token");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to generate CSRF token"
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
    /// SECURITY: The secret path is configured via Auth:SuperAdminSecretPath setting
    /// </summary>
    [HttpPost("system-{secretPath}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SecretLogin(
        string secretPath,
        [FromBody] LoginRequest request)
    {
        try
        {
            // SECURITY: Validate secret path before processing request
            if (secretPath != _secretPath)
            {
                _logger.LogWarning("Invalid secret path attempt: {AttemptedPath} from IP: {IpAddress}",
                    secretPath, GetIpAddress());
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid endpoint"
                });
            }

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
            // SECURITY: NEVER log TOTP secrets or backup codes - they are authentication credentials
            _logger.LogInformation("=== MFA COMPLETE SETUP REQUEST RECEIVED ===");
            _logger.LogInformation("Request object is null: {IsNull}", request == null);

            if (request != null)
            {
                _logger.LogInformation("UserId: {UserId}", request.UserId);
                _logger.LogInformation("TotpCode present: {HasCode}", !string.IsNullOrEmpty(request.TotpCode));
                // SECURITY: Never log the actual secret - it's equivalent to logging a password
                _logger.LogInformation("Secret present: {HasSecret}, Length: {SecretLength}",
                    !string.IsNullOrEmpty(request.Secret),
                    request.Secret?.Length ?? 0);
                _logger.LogInformation("BackupCodes count: {BackupCodesCount}", request.BackupCodes?.Count ?? 0);
                // SECURITY: Never log backup codes - they are single-use authentication tokens
            }

            _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);

            // Null check for request
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request"
                });
            }

            if (!ModelState.IsValid)
            {
                // Log validation errors
                foreach (var entry in ModelState)
                {
                    foreach (var error in entry.Value.Errors)
                    {
                        _logger.LogWarning("ModelState Error - Key: {Key}, Error: {Error}",
                            entry.Key, error.ErrorMessage);
                    }
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            // Null checks for required fields
            if (string.IsNullOrEmpty(request.Secret) || request.BackupCodes == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Secret and backup codes are required"
                });
            }

            var result = await _authService.CompleteMfaSetupAsync(request.UserId, request.TotpCode, request.Secret!, request.BackupCodes!);

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

            // Get client IP address for security tracking
            var ipAddress = GetIpAddress();

            var result = await _tenantAuthService.LoginAsync(request.Email, request.Password, request.Subdomain, ipAddress);

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

            // Set refresh token as HttpOnly cookie
            SetRefreshTokenCookie(result.Value.RefreshToken);

            var response = new
            {
                Token = result.Value.Token,
                RefreshToken = result.Value.RefreshToken, // Also return in response for clients that can't access cookies
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
    /// Refresh access token for SuperAdmin using refresh token from HttpOnly cookie
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
    /// Refresh access token for Tenant Employees using refresh token from HttpOnly cookie
    /// Implements token rotation: old refresh token revoked, new one issued
    /// </summary>
    /// <returns>New access token and refresh token</returns>
    [HttpPost("tenant/refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TenantRefreshToken()
    {
        try
        {
            // Get refresh token from HttpOnly cookie
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Tenant refresh token not found in cookie");
                return Unauthorized(new
                {
                    success = false,
                    message = "Refresh token not found. Please login again."
                });
            }

            var ipAddress = GetIpAddress();

            // Refresh the token (implements rotation)
            var (accessToken, newRefreshToken) = await _tenantAuthService.RefreshTokenAsync(refreshToken, ipAddress);

            // Set new refresh token as HttpOnly cookie
            SetRefreshTokenCookie(newRefreshToken);

            _logger.LogInformation("Tenant token refreshed successfully from IP: {IpAddress}", ipAddress);

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
            _logger.LogWarning("Tenant token refresh failed: {Message}", ex.Message);
            return Unauthorized(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing tenant token");
            return StatusCode(500, new
            {
                success = false,
                message = "Token refresh failed"
            });
        }
    }

    /// <summary>
    /// DIAGNOSTIC ENDPOINT: Validate current JWT token and show all claims
    /// Used for debugging authentication issues
    /// </summary>
    [HttpGet("validate")]
    [Authorize] // Requires valid JWT token
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken()
    {
        try
        {
            // Extract all claims from the JWT token
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

            // Get all claims for comprehensive debugging
            var allClaims = User.Claims.Select(c => new
            {
                type = c.Type,
                value = c.Value
            }).ToList();

            // Log the validation for debugging
            _logger.LogInformation("🔍 TOKEN VALIDATION - User: {UserId}, Email: {Email}, Role: {Role}, Authenticated: {IsAuthenticated}",
                userId, email, role, isAuthenticated);

            _logger.LogInformation("📋 ALL CLAIMS: {Claims}", string.Join(", ", allClaims.Select(c => $"{c.type}={c.value}")));

            return Ok(new
            {
                success = true,
                data = new
                {
                    userId,
                    email,
                    name,
                    role,
                    isAuthenticated,
                    allClaims,
                    timestamp = DateTime.UtcNow
                },
                message = "Token is valid"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, new
            {
                success = false,
                message = "Error validating token"
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
    // PASSWORD RESET ENDPOINTS
    // ============================================

    /// <summary>
    /// Request password reset email
    /// Public endpoint - no authentication required
    /// SECURITY: Does not reveal if email exists (prevents user enumeration)
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid email"
                });
            }

            var (success, message) = await _authService.ForgotPasswordAsync(request.Email);

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ForgotPassword");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred. Please try again later."
            });
        }
    }

    /// <summary>
    /// Reset password using token from email
    /// Public endpoint - no authentication required
    /// SECURITY FEATURES:
    /// - Token expiry validation (1 hour)
    /// - Password complexity enforcement
    /// - Password history check (no reuse of last 5 passwords)
    /// - Single-use token (revoked after use)
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request"
                });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Passwords do not match"
                });
            }

            var (success, message) = await _authService.ResetPasswordAsync(
                request.Token,
                request.NewPassword);

            return success
                ? Ok(new { success, message })
                : BadRequest(new { success, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResetPassword");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred. Please try again later."
            });
        }
    }

    // ============================================
    // CHANGE PASSWORD ENDPOINTS (Authenticated)
    // ============================================

    /// <summary>
    /// Change password for authenticated SuperAdmin users
    /// Requires authentication - user must be logged in
    /// FORTUNE 500 SECURITY FEATURES:
    /// - Current password verification (prevents session hijacking)
    /// - Password complexity enforcement (12+ chars, all types required)
    /// - Password history check (no reuse of last 5 passwords)
    /// - Password expiry tracking (90 days)
    /// - Comprehensive audit logging
    /// - Rate limiting (5 attempts per 15 minutes)
    /// </summary>
    [HttpPost("change-password")]
    [Authorize] // Requires valid JWT token
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var ipAddress = GetIpAddress();

        try
        {
            // Get user ID from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid authentication token"
                });
            }

            // ==============================================
            // FORTRESS-GRADE: RATE LIMITING
            // ==============================================
            // Prevent brute force attacks on password change
            // Fortune 500 standard: 5 attempts per 15 minutes per user
            var rateLimitKey = $"{userId}:change-password";
            var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(
                rateLimitKey,
                limit: 5,
                window: TimeSpan.FromMinutes(15)
            );

            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning(
                    "RATE_LIMIT_EXCEEDED: Password change blocked for user {UserId}. " +
                    "Attempts: {Current}/{Limit}",
                    userId,
                    rateLimitResult.CurrentCount,
                    rateLimitResult.Limit);

                return StatusCode(429, new
                {
                    success = false,
                    message = $"Too many password change attempts. Please try again in {rateLimitResult.RetryAfterSeconds / 60} minutes.",
                    retryAfterSeconds = rateLimitResult.RetryAfterSeconds
                });
            }

            // ==============================================
            // VALIDATION
            // ==============================================
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "New password and confirmation do not match"
                });
            }

            // ==============================================
            // CHANGE PASSWORD
            // ==============================================
            var (success, message) = await _authService.ChangePasswordAsync(
                userId,
                request.CurrentPassword,
                request.NewPassword);

            if (success)
            {
                _logger.LogInformation(
                    "PASSWORD_CHANGE_SUCCESS: User {UserId} changed password from IP {IpAddress}",
                    userId,
                    ipAddress);

                return Ok(new { success, message });
            }
            else
            {
                _logger.LogWarning(
                    "PASSWORD_CHANGE_FAILED: {Message} for user {UserId} from IP {IpAddress}",
                    message,
                    userId,
                    ipAddress);

                return BadRequest(new { success, message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password from IP {IpAddress}", ipAddress);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while changing password"
            });
        }
    }

    /// <summary>
    /// Change password for authenticated Tenant Employee users
    /// Requires authentication - user must be logged in
    /// MULTI-TENANT: Ensures proper tenant context isolation
    /// FORTUNE 500 SECURITY FEATURES:
    /// - Current password verification (prevents session hijacking)
    /// - Password complexity enforcement (12+ chars, all types required)
    /// - Password history check (no reuse of last 5 passwords)
    /// - Comprehensive audit logging with tenant context
    /// - Rate limiting (5 attempts per 15 minutes)
    /// </summary>
    [HttpPost("tenant/change-password")]
    [Authorize] // Requires valid JWT token
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> TenantChangePassword([FromBody] ChangePasswordRequest request)
    {
        var ipAddress = GetIpAddress();

        try
        {
            // Get employee ID from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var employeeId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid authentication token"
                });
            }

            // Get tenant ID from JWT claims for proper multi-tenant isolation
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid tenant context"
                });
            }

            // ==============================================
            // FORTRESS-GRADE: RATE LIMITING
            // ==============================================
            var rateLimitKey = $"{employeeId}:tenant:change-password";
            var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(
                rateLimitKey,
                limit: 5,
                window: TimeSpan.FromMinutes(15)
            );

            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning(
                    "RATE_LIMIT_EXCEEDED: Tenant password change blocked for employee {EmployeeId}. " +
                    "Attempts: {Current}/{Limit}",
                    employeeId,
                    rateLimitResult.CurrentCount,
                    rateLimitResult.Limit);

                return StatusCode(429, new
                {
                    success = false,
                    message = $"Too many password change attempts. Please try again in {rateLimitResult.RetryAfterSeconds / 60} minutes.",
                    retryAfterSeconds = rateLimitResult.RetryAfterSeconds
                });
            }

            // ==============================================
            // VALIDATION
            // ==============================================
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "New password and confirmation do not match"
                });
            }

            // ==============================================
            // CHANGE PASSWORD (MULTI-TENANT)
            // ==============================================
            var (success, message) = await _tenantAuthService.ChangeEmployeePasswordAsync(
                employeeId,
                tenantId,
                request.CurrentPassword,
                request.NewPassword);

            if (success)
            {
                _logger.LogInformation(
                    "TENANT_PASSWORD_CHANGE_SUCCESS: Employee {EmployeeId} in tenant {TenantId} changed password from IP {IpAddress}",
                    employeeId,
                    tenantId,
                    ipAddress);

                return Ok(new { success, message });
            }
            else
            {
                _logger.LogWarning(
                    "TENANT_PASSWORD_CHANGE_FAILED: {Message} for employee {EmployeeId} in tenant {TenantId} from IP {IpAddress}",
                    message,
                    employeeId,
                    tenantId,
                    ipAddress);

                return BadRequest(new { success, message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing tenant employee password from IP {IpAddress}", ipAddress);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while changing password"
            });
        }
    }

    /// <summary>
    /// Set employee password using token from welcome email
    /// Public endpoint - no authentication required
    /// FORTRESS-GRADE SECURITY FEATURES:
    /// - Rate limiting (5 attempts per hour per IP)
    /// - Token expiry validation (24 hours)
    /// - Password complexity enforcement (12+ chars, all types required)
    /// - Password history check (no reuse of last 5 passwords)
    /// - Single-use token (revoked after use)
    /// - Subdomain validation (anti-spoofing protection)
    /// FORTUNE 500: Used for initial password setup after tenant activation
    /// </summary>
    [HttpPost("employee/set-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SetEmployeePassword([FromBody] SetEmployeePasswordRequest request)
    {
        var ipAddress = GetIpAddress();

        try
        {
            // ==============================================
            // FORTRESS-GRADE: RATE LIMITING
            // ==============================================
            // Prevent brute force attacks on password setup endpoint
            // Fortune 50 standard: 5 attempts per hour per IP
            var rateLimitKey = $"{ipAddress}:employee:set-password";
            var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(
                rateLimitKey,
                limit: 5,               // Max 5 password setup attempts
                window: TimeSpan.FromHours(1) // Per hour
            );

            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning(
                    "RATE_LIMIT_EXCEEDED: Password setup attempt blocked for IP {IpAddress}. " +
                    "Attempts: {Current}/{Limit}, Resets at: {ResetsAt}",
                    ipAddress,
                    rateLimitResult.CurrentCount,
                    rateLimitResult.Limit,
                    rateLimitResult.ResetsAt);

                return StatusCode(429, new // 429 Too Many Requests
                {
                    success = false,
                    message = rateLimitResult.IsBlacklisted
                        ? "Your IP address has been temporarily blocked due to too many failed attempts."
                        : $"Too many password setup attempts. Please try again in {rateLimitResult.RetryAfterSeconds / 60} minutes.",
                    retryAfterSeconds = rateLimitResult.RetryAfterSeconds,
                    resetsAt = rateLimitResult.ResetsAt
                });
            }

            _logger.LogInformation(
                "RATE_LIMIT_CHECK: Password setup attempt {Current}/{Limit} from IP {IpAddress}",
                rateLimitResult.CurrentCount,
                rateLimitResult.Limit,
                ipAddress);

            // ==============================================
            // BASIC VALIDATION
            // ==============================================
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request"
                });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Passwords do not match"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Subdomain))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Subdomain is required"
                });
            }

            // ==============================================
            // FORTRESS-GRADE: SUBDOMAIN VALIDATION
            // ==============================================
            // Prevent subdomain spoofing attacks
            // Fortune 50 security: Validate subdomain format and sanitization
            var sanitizedSubdomain = request.Subdomain.Trim().ToLowerInvariant();

            // Basic subdomain format validation (alphanumeric + hyphens only)
            if (!System.Text.RegularExpressions.Regex.IsMatch(sanitizedSubdomain, @"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$"))
            {
                _logger.LogWarning(
                    "SUBDOMAIN_VALIDATION_FAILED: Invalid subdomain format '{Subdomain}' from IP {IpAddress}",
                    request.Subdomain,
                    ipAddress);

                return BadRequest(new
                {
                    success = false,
                    message = "Invalid subdomain format. Only lowercase letters, numbers, and hyphens are allowed."
                });
            }

            // Subdomain length validation (3-63 characters per DNS RFC 1035)
            if (sanitizedSubdomain.Length < 3 || sanitizedSubdomain.Length > 63)
            {
                _logger.LogWarning(
                    "SUBDOMAIN_VALIDATION_FAILED: Invalid subdomain length '{Subdomain}' ({Length} chars) from IP {IpAddress}",
                    request.Subdomain,
                    sanitizedSubdomain.Length,
                    ipAddress);

                return BadRequest(new
                {
                    success = false,
                    message = "Subdomain must be between 3 and 63 characters."
                });
            }

            _logger.LogInformation(
                "SUBDOMAIN_VALIDATED: '{Subdomain}' passed validation checks",
                sanitizedSubdomain);

            // ==============================================
            // PASSWORD SETUP (with fortress-grade validation)
            // ==============================================
            var (success, message) = await _tenantAuthService.SetEmployeePasswordAsync(
                request.Token,
                request.NewPassword,
                sanitizedSubdomain);

            if (success)
            {
                _logger.LogInformation(
                    "PASSWORD_SETUP_SUCCESS: Employee password set for subdomain '{Subdomain}' from IP {IpAddress}",
                    sanitizedSubdomain,
                    ipAddress);

                return Ok(new { success, message });
            }
            else
            {
                _logger.LogWarning(
                    "PASSWORD_SETUP_FAILED: {Message} for subdomain '{Subdomain}' from IP {IpAddress}",
                    message,
                    sanitizedSubdomain,
                    ipAddress);

                return BadRequest(new { success, message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SetEmployeePassword from IP {IpAddress}", ipAddress);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred. Please try again later."
            });
        }
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
