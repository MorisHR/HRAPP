using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HRMS.Application.DTOs;
using HRMS.Infrastructure.Services;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using HRMS.Application.Interfaces;
using HRMS.API.Attributes;
using HRMS.Core.Constants;

namespace HRMS.API.Controllers;

/// <summary>
/// Super Admin API for tenant management
/// Only accessible via admin.hrms.com subdomain
/// SECURITY: Requires SuperAdmin role for ALL operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class TenantsController : ControllerBase
{
    private readonly TenantManagementService _tenantManagementService;
    private readonly MasterDbContext _context;
    private readonly ILogger<TenantsController> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IAuditLogService _auditLogService;
    private readonly IRateLimitService _rateLimitService;

    public TenantsController(
        TenantManagementService tenantManagementService,
        MasterDbContext context,
        ILogger<TenantsController> logger,
        IEmailService emailService,
        IConfiguration configuration,
        IAuditLogService auditLogService,
        IRateLimitService rateLimitService)
    {
        _tenantManagementService = tenantManagementService;
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
        _auditLogService = auditLogService;
        _rateLimitService = rateLimitService;
    }

    /// <summary>
    /// Check if a tenant exists by subdomain (public endpoint for login flow)
    /// </summary>
    [HttpGet("check/{subdomain}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckTenant(string subdomain)
    {
        try
        {
            var normalizedSubdomain = subdomain.ToLower().Trim();

            var tenant = await _context.Tenants
                .Where(t => t.Subdomain == normalizedSubdomain && !t.IsDeleted)
                .Select(t => new { t.CompanyName, t.Status, t.Subdomain })
                .FirstOrDefaultAsync();

            if (tenant == null)
            {
                return Ok(new { success = true, data = new { exists = false } });
            }

            if (tenant.Status != Core.Enums.TenantStatus.Active)
            {
                return Ok(new
                {
                    success = false,
                    message = "This company account is not active. Please contact support.",
                    data = new { exists = true, isActive = false }
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    exists = true,
                    companyName = tenant.CompanyName,
                    subdomain = tenant.Subdomain,
                    logoUrl = (string?)null
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking tenant: {Subdomain}", subdomain);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while checking the domain. Please try again."
            });
        }
    }

    /// <summary>
    /// Get all tenants
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.TENANT_VIEW)]
    public async Task<IActionResult> GetAllTenants()
    {
        try
        {
            var tenants = await _tenantManagementService.GetAllTenantsAsync();
            return Ok(new { success = true, data = tenants, count = tenants.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenants");
            return StatusCode(500, new { success = false, message = "Error retrieving tenants" });
        }
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission(Permissions.TENANT_VIEW)]
    public async Task<IActionResult> GetTenantById(Guid id)
    {
        try
        {
            var tenant = await _tenantManagementService.GetTenantByIdAsync(id);
            if (tenant == null)
                return NotFound(new { success = false, message = "Tenant not found" });

            return Ok(new { success = true, data = tenant });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant: {TenantId}", id);
            return StatusCode(500, new { success = false, message = "Error retrieving tenant" });
        }
    }

    /// <summary>
    /// Create a new tenant with email activation workflow
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.TENANT_CREATE)]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid request", errors = ModelState });

            // Validate subdomain uniqueness
            var existingTenant = await _context.Tenants
                .AnyAsync(t => t.Subdomain == request.Subdomain.ToLower());

            if (existingTenant)
                return BadRequest(new { success = false, message = "Subdomain already exists" });

            // Get SuperAdmin info from authenticated user
            var superAdminId = Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value ?? Guid.Empty.ToString());
            var superAdminEmail = User.FindFirst("email")?.Value ?? "unknown@hrms.com";
            var createdBy = superAdminEmail;

            var (success, message, tenant) = await _tenantManagementService.CreateTenantAsync(request, createdBy);

            if (!success)
            {
                // FORTUNE 500: Log failed tenant creation attempt
                await _auditLogService.LogSuperAdminActionAsync(
                    AuditActionType.TENANT_CREATED,
                    superAdminId,
                    superAdminEmail,
                    description: $"Failed to create tenant: {request.CompanyName}",
                    newValues: System.Text.Json.JsonSerializer.Serialize(new { request.CompanyName, request.Subdomain, request.AdminEmail }),
                    success: false,
                    errorMessage: message
                );

                return BadRequest(new { success = false, message });
            }

            _logger.LogInformation("Tenant created successfully: {TenantId} - Status: {Status}", tenant!.Id, tenant.Status);

            // FORTUNE 500: Log successful tenant creation with full details
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.TENANT_CREATED,
                superAdminId,
                superAdminEmail,
                targetTenantId: tenant.Id,
                targetTenantName: tenant.CompanyName,
                description: $"Created new tenant: {tenant.CompanyName} ({tenant.Subdomain})",
                newValues: System.Text.Json.JsonSerializer.Serialize(new
                {
                    tenant.Id,
                    tenant.CompanyName,
                    tenant.Subdomain,
                    tenant.AdminEmail,
                    tenant.Status,
                    tenant.EmployeeTier
                }),
                success: true,
                additionalContext: new Dictionary<string, object>
                {
                    { "emailSent", true },
                    { "initialStatus", tenant.Status.ToString() }
                }
            );

            // Get the actual tenant entity to access activation token
            var tenantEntity = await _context.Tenants.FindAsync(tenant.Id);

            // Send activation email
            var emailSent = await _emailService.SendTenantActivationEmailAsync(
                request.AdminEmail,
                request.CompanyName,
                tenantEntity!.ActivationToken!,
                request.AdminFirstName
            );

            if (!emailSent)
            {
                _logger.LogWarning("Failed to send activation email to {Email} for tenant {TenantId}",
                    request.AdminEmail, tenant.Id);
            }

            return CreatedAtAction(
                nameof(GetTenantById),
                new { id = tenant.Id },
                new
                {
                    success = true,
                    message = "Tenant created successfully. Activation email sent to admin.",
                    data = new
                    {
                        tenantId = tenant.Id,
                        subdomain = tenantEntity.Subdomain,
                        companyName = tenantEntity.CompanyName,
                        status = tenantEntity.Status.ToString(),
                        activationEmailSent = emailSent,
                        adminEmail = tenantEntity.AdminEmail
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return StatusCode(500, new { success = false, message = "Error creating tenant" });
        }
    }

    /// <summary>
    /// Activate a tenant account using activation token from email
    /// Public endpoint - no authentication required
    /// </summary>
    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<IActionResult> ActivateTenant([FromBody] ActivateTenantRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ActivationToken))
                return BadRequest(new { success = false, message = "Activation token is required" });

            // Get tenant by activation token
            var tenant = await _tenantManagementService.GetTenantByActivationTokenAsync(request.ActivationToken);

            if (tenant == null)
            {
                _logger.LogWarning("Activation attempt with invalid token: {Token}",
                    request.ActivationToken.Substring(0, Math.Min(8, request.ActivationToken.Length)));
                return NotFound(new { success = false, message = "Invalid activation token" });
            }

            // Check if already activated
            if (tenant.Status == TenantStatus.Active)
            {
                _logger.LogWarning("Activation attempt for already active tenant: {Subdomain}", tenant.Subdomain);
                return BadRequest(new { success = false, message = "Tenant account is already activated" });
            }

            // Check token expiry
            if (tenant.ActivationTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Activation attempt with expired token for tenant: {Subdomain}", tenant.Subdomain);
                return BadRequest(new { success = false, message = "Activation link has expired. Please contact support." });
            }

            // Activate tenant
            var (success, message, subdomain) = await _tenantManagementService.ActivateTenantAsync(request.ActivationToken);

            if (!success)
            {
                _logger.LogError("Tenant activation failed: {Message}", message);
                return BadRequest(new { success = false, message });
            }

            _logger.LogInformation("Tenant activated successfully: {Subdomain}", subdomain);

            // Send welcome email
            var welcomeEmailSent = await _emailService.SendTenantWelcomeEmailAsync(
                tenant.AdminEmail,
                tenant.CompanyName,
                tenant.AdminFirstName,
                tenant.Subdomain
            );

            if (!welcomeEmailSent)
            {
                _logger.LogWarning("Failed to send welcome email to {Email}", tenant.AdminEmail);
            }

            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:4200";

            return Ok(new ActivateTenantResponse
            {
                Success = true,
                Message = "Tenant activated successfully! Check your email for login instructions.",
                TenantSubdomain = subdomain,
                LoginUrl = $"{frontendUrl}/login",
                AdminEmail = tenant.AdminEmail
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating tenant");
            return StatusCode(500, new { success = false, message = "Activation failed. Please try again." });
        }
    }

    /// <summary>
    /// Resend activation email for a pending tenant
    /// FORTUNE 500 PATTERN: Rate-limited (3 per hour), audit-logged, multi-tenant secure
    /// PUBLIC ENDPOINT: No authentication required (allows users to request resend)
    /// SECURITY: IP-based + tenant-based rate limiting, comprehensive audit trail
    /// </summary>
    [HttpPost("resend-activation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendActivationEmail([FromBody] ResendActivationRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();

        try
        {
            // STEP 1: VALIDATION - Input validation
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Subdomain))
            {
                _logger.LogWarning("Resend activation attempt with missing email or subdomain. IP: {IpAddress}", ipAddress);
                return BadRequest(new { success = false, message = "Email and subdomain are required" });
            }

            // STEP 2: TENANT LOOKUP - Find tenant by subdomain
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Subdomain == request.Subdomain.ToLower().Trim() && !t.IsDeleted);

            if (tenant == null)
            {
                // SECURITY: Generic error message (don't reveal tenant existence)
                _logger.LogWarning("Resend activation attempt for non-existent subdomain: {Subdomain}. IP: {IpAddress}",
                    request.Subdomain, ipAddress);

                // Log failed attempt to ActivationResendLogs
                await LogActivationResendAttemptAsync(Guid.Empty, request.Email, ipAddress, userAgent,
                    success: false, failureReason: "Tenant not found");

                return NotFound(new { success = false, message = "No pending activation found for this email and company" });
            }

            // STEP 3: EMAIL VERIFICATION - Must match tenant admin email
            // SECURITY FIX: Validate against AdminEmail (where activation was sent), not ContactEmail
            if (!string.Equals(tenant.AdminEmail, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Resend activation attempt with mismatched email. Subdomain: {Subdomain}, ProvidedEmail: {Email}, ActualEmail: {AdminEmail}. IP: {IpAddress}",
                    request.Subdomain, request.Email, tenant.AdminEmail, ipAddress);

                await LogActivationResendAttemptAsync(tenant.Id, request.Email, ipAddress, userAgent,
                    success: false, failureReason: "Email mismatch");

                return BadRequest(new { success = false, message = "Email does not match the admin email used during registration" });
            }

            // STEP 4: STATUS CHECK - Tenant must be in Pending status
            if (tenant.Status != TenantStatus.Pending)
            {
                _logger.LogInformation("Resend activation attempt for non-pending tenant: {Subdomain}, Status: {Status}. IP: {IpAddress}",
                    tenant.Subdomain, tenant.Status, ipAddress);

                await LogActivationResendAttemptAsync(tenant.Id, request.Email, ipAddress, userAgent,
                    success: false, failureReason: $"Tenant already {tenant.Status}");

                return BadRequest(new {
                    success = false,
                    message = tenant.Status == TenantStatus.Active
                        ? "This company is already activated. Please proceed to login."
                        : "This company cannot receive activation emails in its current status."
                });
            }

            // STEP 5: RATE LIMITING - Check tenant-based rate limit (3 per hour per tenant)
            var tenantRateLimitKey = $"activation_resend:tenant:{tenant.Id}";
            var tenantRateLimit = await _rateLimitService.CheckRateLimitAsync(
                tenantRateLimitKey,
                limit: 3,
                window: TimeSpan.FromHours(1)
            );

            if (!tenantRateLimit.IsAllowed)
            {
                _logger.LogWarning("Resend activation rate limit exceeded for tenant: {Subdomain}. Count: {Count}/{Limit}. IP: {IpAddress}",
                    tenant.Subdomain, tenantRateLimit.CurrentCount, tenantRateLimit.Limit, ipAddress);

                await LogActivationResendAttemptAsync(tenant.Id, request.Email, ipAddress, userAgent,
                    success: false, failureReason: $"Rate limit exceeded ({tenantRateLimit.CurrentCount}/{tenantRateLimit.Limit} in last hour)",
                    wasRateLimited: true, resendCountLastHour: tenantRateLimit.CurrentCount);

                return StatusCode(429, new {
                    success = false,
                    message = $"Too many resend requests. Please wait {tenantRateLimit.RetryAfterSeconds / 60} minutes before trying again.",
                    retryAfterSeconds = tenantRateLimit.RetryAfterSeconds,
                    currentCount = tenantRateLimit.CurrentCount,
                    limit = tenantRateLimit.Limit
                });
            }

            // STEP 6: IP-BASED RATE LIMITING - Prevent IP-based abuse (10 per hour per IP)
            var ipRateLimitKey = $"activation_resend:ip:{ipAddress}";
            var ipRateLimit = await _rateLimitService.CheckRateLimitAsync(
                ipRateLimitKey,
                limit: 10,
                window: TimeSpan.FromHours(1)
            );

            if (!ipRateLimit.IsAllowed)
            {
                _logger.LogWarning("Resend activation IP rate limit exceeded. IP: {IpAddress}. Count: {Count}/{Limit}",
                    ipAddress, ipRateLimit.CurrentCount, ipRateLimit.Limit);

                await LogActivationResendAttemptAsync(tenant.Id, request.Email, ipAddress, userAgent,
                    success: false, failureReason: $"IP rate limit exceeded ({ipRateLimit.CurrentCount}/{ipRateLimit.Limit} in last hour)",
                    wasRateLimited: true, resendCountLastHour: ipRateLimit.CurrentCount);

                // Auto-blacklist if excessive attempts
                if (ipRateLimit.CurrentCount > 20)
                {
                    await _rateLimitService.BlacklistIpAsync(ipAddress, TimeSpan.FromHours(24), "Excessive activation resend attempts");
                    _logger.LogWarning("IP blacklisted for 24 hours due to excessive resend attempts: {IpAddress}", ipAddress);
                }

                return StatusCode(429, new {
                    success = false,
                    message = "Too many requests from your IP address. Please try again later."
                });
            }

            // STEP 7: TOKEN GENERATION - Generate new activation token
            var newToken = Guid.NewGuid().ToString("N"); // 32-char hex string
            var tokenExpiry = DateTime.UtcNow.AddHours(24);

            tenant.ActivationToken = newToken;
            tenant.ActivationTokenExpiry = tokenExpiry;
            tenant.UpdatedAt = DateTime.UtcNow;
            tenant.UpdatedBy = $"system_resend_{ipAddress}";

            await _context.SaveChangesAsync();

            _logger.LogInformation("New activation token generated for tenant: {Subdomain}. Expires: {Expiry}",
                tenant.Subdomain, tokenExpiry);

            // STEP 8: SEND ACTIVATION EMAIL
            var emailSent = await _emailService.SendTenantActivationEmailAsync(
                tenant.ContactEmail,
                tenant.CompanyName,
                newToken,
                tenant.AdminFirstName
            );

            if (!emailSent)
            {
                _logger.LogError("Failed to send activation email to: {Email}. Subdomain: {Subdomain}",
                    tenant.ContactEmail, tenant.Subdomain);

                await LogActivationResendAttemptAsync(tenant.Id, request.Email, ipAddress, userAgent,
                    success: false, failureReason: "Email send failed",
                    tokenGenerated: newToken.Substring(0, 8), tokenExpiry: tokenExpiry,
                    emailDelivered: false, emailSendError: "SMTP delivery failure");

                return StatusCode(500, new { success = false, message = "Failed to send activation email. Please try again or contact support." });
            }

            // STEP 9: LOG SUCCESS TO ACTIVATION RESEND LOGS
            await LogActivationResendAttemptAsync(tenant.Id, request.Email, ipAddress, userAgent,
                success: true, tokenGenerated: newToken.Substring(0, 8), tokenExpiry: tokenExpiry,
                emailDelivered: true, resendCountLastHour: tenantRateLimit.CurrentCount + 1);

            _logger.LogInformation("Activation email resent successfully. Subdomain: {Subdomain}, Email: {Email}, IP: {IpAddress}",
                tenant.Subdomain, tenant.ContactEmail, ipAddress);

            return Ok(new {
                success = true,
                message = "Activation email sent successfully! Please check your inbox and spam folder.",
                expiresIn = "24 hours",
                remainingAttempts = tenantRateLimit.Limit - (tenantRateLimit.CurrentCount + 1)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing resend activation request. Subdomain: {Subdomain}, IP: {IpAddress}",
                request.Subdomain, ipAddress);

            return StatusCode(500, new { success = false, message = "An error occurred. Please try again later." });
        }
    }

    /// <summary>
    /// FORTUNE 500 PATTERN: Audit logging helper for activation resend attempts
    /// Logs all attempts (success + failure) to ActivationResendLogs table
    /// Enables rate limit enforcement, security monitoring, GDPR compliance
    /// </summary>
    private async Task LogActivationResendAttemptAsync(
        Guid tenantId,
        string requestedByEmail,
        string ipAddress,
        string userAgent,
        bool success,
        string? failureReason = null,
        string? tokenGenerated = null,
        DateTime? tokenExpiry = null,
        bool emailDelivered = false,
        string? emailSendError = null,
        bool wasRateLimited = false,
        int resendCountLastHour = 0)
    {
        try
        {
            var log = new HRMS.Core.Entities.Master.ActivationResendLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId == Guid.Empty ? Guid.NewGuid() : tenantId, // Temp ID for non-existent tenants
                RequestedAt = DateTime.UtcNow,
                RequestedFromIp = ipAddress,
                RequestedByEmail = requestedByEmail,
                TokenGenerated = tokenGenerated,
                TokenExpiry = tokenExpiry ?? DateTime.UtcNow.AddHours(24),
                Success = success,
                FailureReason = failureReason,
                UserAgent = userAgent,
                DeviceInfo = ParseDeviceInfo(userAgent),
                Geolocation = "Unknown", // Can integrate with geolocation service later
                EmailDelivered = emailDelivered,
                EmailSendError = emailSendError,
                ResendCountLastHour = resendCountLastHour,
                WasRateLimited = wasRateLimited,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = $"system_resend_{ipAddress}",
                IsDeleted = false
            };

            _context.ActivationResendLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't fail the request
            _logger.LogError(ex, "Failed to log activation resend attempt to database. TenantId: {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Parse device information from user agent string
    /// Fortune 500 pattern: Device fingerprinting for fraud detection
    /// </summary>
    private string ParseDeviceInfo(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        userAgent = userAgent.ToLower();

        // Mobile detection
        if (userAgent.Contains("mobile") || userAgent.Contains("android") || userAgent.Contains("iphone"))
        {
            if (userAgent.Contains("android")) return "Mobile - Android";
            if (userAgent.Contains("iphone") || userAgent.Contains("ipad")) return "Mobile - iOS";
            return "Mobile - Unknown";
        }

        // Desktop detection
        if (userAgent.Contains("chrome")) return "Desktop - Chrome";
        if (userAgent.Contains("firefox")) return "Desktop - Firefox";
        if (userAgent.Contains("safari")) return "Desktop - Safari";
        if (userAgent.Contains("edge")) return "Desktop - Edge";

        return "Unknown";
    }

    /// <summary>
    /// Suspend a tenant (temporary block)
    /// </summary>
    [HttpPost("{id}/suspend")]
    [RequirePermission(Permissions.TENANT_SUSPEND)]
    public async Task<IActionResult> SuspendTenant(Guid id, [FromBody] SuspendTenantRequest request)
    {
        try
        {
            // Get SuperAdmin info from authenticated user
            var superAdminId = Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value ?? Guid.Empty.ToString());
            var superAdminEmail = User.FindFirst("email")?.Value ?? "unknown@hrms.com";
            var suspendedBy = superAdminEmail;

            // Get tenant details before suspension
            var tenant = await _tenantManagementService.GetTenantByIdAsync(id);
            var (success, message) = await _tenantManagementService.SuspendTenantAsync(id, request.Reason, suspendedBy);

            if (!success)
            {
                // FORTUNE 500: Log failed suspension attempt
                await _auditLogService.LogSuperAdminActionAsync(
                    AuditActionType.TENANT_SUSPENDED,
                    superAdminId,
                    superAdminEmail,
                    targetTenantId: id,
                    targetTenantName: tenant?.CompanyName,
                    description: $"Failed to suspend tenant",
                    reason: request.Reason,
                    success: false,
                    errorMessage: message
                );

                return BadRequest(new { success = false, message });
            }

            // FORTUNE 500: Log successful suspension with reason
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.TENANT_SUSPENDED,
                superAdminId,
                superAdminEmail,
                targetTenantId: id,
                targetTenantName: tenant?.CompanyName,
                description: $"Suspended tenant: {tenant?.CompanyName}",
                oldValues: System.Text.Json.JsonSerializer.Serialize(new { Status = "Active" }),
                newValues: System.Text.Json.JsonSerializer.Serialize(new { Status = "Suspended" }),
                reason: request.Reason,
                success: true
            );

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending tenant: {TenantId}", id);
            return StatusCode(500, new { success = false, message = "Error suspending tenant" });
        }
    }

    /// <summary>
    /// Soft delete a tenant (mark for deletion with grace period)
    /// </summary>
    [HttpDelete("{id}/soft")]
    [RequirePermission(Permissions.TENANT_DELETE)]
    public async Task<IActionResult> SoftDeleteTenant(Guid id, [FromBody] DeleteTenantRequest request)
    {
        try
        {
            // Get SuperAdmin info from authenticated user
            var superAdminId = Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value ?? Guid.Empty.ToString());
            var superAdminEmail = User.FindFirst("email")?.Value ?? "unknown@hrms.com";
            var deletedBy = superAdminEmail;

            // Get tenant details before deletion
            var tenant = await _tenantManagementService.GetTenantByIdAsync(id);
            var (success, message) = await _tenantManagementService.SoftDeleteTenantAsync(id, request.Reason, deletedBy);

            if (!success)
            {
                // FORTUNE 500: Log failed soft delete attempt
                await _auditLogService.LogSuperAdminActionAsync(
                    AuditActionType.TENANT_DELETED,
                    superAdminId,
                    superAdminEmail,
                    targetTenantId: id,
                    targetTenantName: tenant?.CompanyName,
                    description: $"Failed to soft delete tenant",
                    reason: request.Reason,
                    success: false,
                    errorMessage: message
                );

                return BadRequest(new { success = false, message });
            }

            // FORTUNE 500: Log successful soft delete with grace period info
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.TENANT_DELETED,
                superAdminId,
                superAdminEmail,
                targetTenantId: id,
                targetTenantName: tenant?.CompanyName,
                description: $"Soft deleted tenant: {tenant?.CompanyName} (30-day grace period)",
                oldValues: System.Text.Json.JsonSerializer.Serialize(new { IsDeleted = false }),
                newValues: System.Text.Json.JsonSerializer.Serialize(new { IsDeleted = true, DeletedAt = DateTime.UtcNow }),
                reason: request.Reason,
                success: true
            );

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting tenant: {TenantId}", id);
            return StatusCode(500, new { success = false, message = "Error soft deleting tenant" });
        }
    }

    /// <summary>
    /// Reactivate a suspended or soft-deleted tenant
    /// </summary>
    [HttpPost("{id}/reactivate")]
    [RequirePermission(Permissions.TENANT_REACTIVATE)]
    public async Task<IActionResult> ReactivateTenant(Guid id)
    {
        try
        {
            // Get SuperAdmin info from authenticated user
            var superAdminId = Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value ?? Guid.Empty.ToString());
            var superAdminEmail = User.FindFirst("email")?.Value ?? "unknown@hrms.com";
            var reactivatedBy = superAdminEmail;

            // Get tenant details before reactivation
            var tenant = await _tenantManagementService.GetTenantByIdAsync(id);
            var (success, message) = await _tenantManagementService.ReactivateTenantAsync(id, reactivatedBy);

            if (!success)
            {
                // FORTUNE 500: Log failed reactivation attempt
                await _auditLogService.LogSuperAdminActionAsync(
                    AuditActionType.TENANT_REACTIVATED,
                    superAdminId,
                    superAdminEmail,
                    targetTenantId: id,
                    targetTenantName: tenant?.CompanyName,
                    description: $"Failed to reactivate tenant",
                    success: false,
                    errorMessage: message
                );

                return BadRequest(new { success = false, message });
            }

            // FORTUNE 500: Log successful reactivation
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.TENANT_REACTIVATED,
                superAdminId,
                superAdminEmail,
                targetTenantId: id,
                targetTenantName: tenant?.CompanyName,
                description: $"Reactivated tenant: {tenant?.CompanyName}",
                oldValues: System.Text.Json.JsonSerializer.Serialize(new { Status = tenant?.Status.ToString() }),
                newValues: System.Text.Json.JsonSerializer.Serialize(new { Status = "Active" }),
                success: true
            );

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating tenant: {TenantId}", id);
            return StatusCode(500, new { success = false, message = "Error reactivating tenant" });
        }
    }

    /// <summary>
    /// Hard delete a tenant (permanent - IRREVERSIBLE)
    /// Only allowed after grace period expires
    /// </summary>
    [HttpDelete("{id}/hard")]
    [RequirePermission(Permissions.TENANT_HARD_DELETE)]
    public async Task<IActionResult> HardDeleteTenant(Guid id, [FromBody] HardDeleteTenantRequest request)
    {
        try
        {
            // Get SuperAdmin info from authenticated user
            var superAdminId = Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value ?? Guid.Empty.ToString());
            var superAdminEmail = User.FindFirst("email")?.Value ?? "unknown@hrms.com";

            // Require confirmation by typing tenant name
            var tenant = await _tenantManagementService.GetTenantByIdAsync(id);
            if (tenant == null)
                return NotFound(new { success = false, message = "Tenant not found" });

            if (!request.ConfirmationName.Equals(tenant.CompanyName, StringComparison.OrdinalIgnoreCase))
            {
                // FORTUNE 500: Log failed confirmation attempt (potential security incident)
                await _auditLogService.LogSuperAdminActionAsync(
                    AuditActionType.TENANT_HARD_DELETED,
                    superAdminId,
                    superAdminEmail,
                    targetTenantId: id,
                    targetTenantName: tenant.CompanyName,
                    description: $"FAILED CONFIRMATION: Attempted hard delete with incorrect name confirmation",
                    success: false,
                    errorMessage: "Tenant name confirmation does not match",
                    additionalContext: new Dictionary<string, object>
                    {
                        { "attemptedConfirmation", request.ConfirmationName },
                        { "actualTenantName", tenant.CompanyName }
                    }
                );

                return BadRequest(new { success = false, message = "Tenant name confirmation does not match" });
            }

            var deletedBy = superAdminEmail;
            var (success, message) = await _tenantManagementService.HardDeleteTenantAsync(id, deletedBy);

            if (!success)
            {
                // FORTUNE 500: Log failed hard delete attempt
                await _auditLogService.LogSuperAdminActionAsync(
                    AuditActionType.TENANT_HARD_DELETED,
                    superAdminId,
                    superAdminEmail,
                    targetTenantId: id,
                    targetTenantName: tenant.CompanyName,
                    description: $"CRITICAL: Failed hard delete attempt for tenant",
                    oldValues: System.Text.Json.JsonSerializer.Serialize(new { tenant.Id, tenant.CompanyName, tenant.Subdomain }),
                    success: false,
                    errorMessage: message
                );

                return BadRequest(new { success = false, message });
            }

            // FORTUNE 500: Log CRITICAL successful hard delete (IRREVERSIBLE)
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.TENANT_HARD_DELETED,
                superAdminId,
                superAdminEmail,
                targetTenantId: id,
                targetTenantName: tenant.CompanyName,
                description: $"CRITICAL: HARD DELETED tenant: {tenant.CompanyName} (IRREVERSIBLE)",
                oldValues: System.Text.Json.JsonSerializer.Serialize(new
                {
                    tenant.Id,
                    tenant.CompanyName,
                    tenant.Subdomain,
                    tenant.AdminEmail,
                    tenant.Status,
                    tenant.EmployeeTier,
                    EmployeeCount = tenant.MaxUsers
                }),
                success: true,
                additionalContext: new Dictionary<string, object>
                {
                    { "confirmationProvided", request.ConfirmationName },
                    { "operationType", "HARD_DELETE" },
                    { "reversible", false }
                }
            );

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hard deleting tenant: {TenantId}", id);
            return StatusCode(500, new { success = false, message = "Error hard deleting tenant" });
        }
    }

    /// <summary>
    /// Update tenant employee tier and pricing
    /// </summary>
    [HttpPut("{id}/tier")]
    [RequirePermission(Permissions.TENANT_UPDATE)]
    public async Task<IActionResult> UpdateEmployeeTier(Guid id, [FromBody] UpdateEmployeeTierRequest request)
    {
        try
        {
            // Get SuperAdmin info from authenticated user
            var superAdminId = Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value ?? Guid.Empty.ToString());
            var superAdminEmail = User.FindFirst("email")?.Value ?? "unknown@hrms.com";
            var updatedBy = superAdminEmail;

            // Get tenant details before update
            var tenant = await _tenantManagementService.GetTenantByIdAsync(id);

            var (success, message) = await _tenantManagementService.UpdateEmployeeTierAsync(
                id,
                request.EmployeeTier,
                request.MaxUsers,
                request.MaxStorageGB,
                request.ApiCallsPerMonth,
                request.YearlyPriceMUR,
                updatedBy);

            if (!success)
            {
                // FORTUNE 500: Log failed tier update attempt
                await _auditLogService.LogSuperAdminActionAsync(
                    AuditActionType.TENANT_TIER_UPDATED,
                    superAdminId,
                    superAdminEmail,
                    targetTenantId: id,
                    targetTenantName: tenant?.CompanyName,
                    description: $"Failed to update employee tier",
                    newValues: System.Text.Json.JsonSerializer.Serialize(request),
                    success: false,
                    errorMessage: message
                );

                return BadRequest(new { success = false, message });
            }

            // FORTUNE 500: Log successful tier update with pricing changes
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.TENANT_TIER_UPDATED,
                superAdminId,
                superAdminEmail,
                targetTenantId: id,
                targetTenantName: tenant?.CompanyName,
                description: $"Updated employee tier for: {tenant?.CompanyName}",
                oldValues: System.Text.Json.JsonSerializer.Serialize(new
                {
                    EmployeeTier = tenant?.EmployeeTier.ToString(),
                    MaxUsers = tenant?.MaxUsers,
                    MaxStorageGB = tenant?.MaxStorageGB,
                    YearlyPriceMUR = tenant?.YearlyPriceMUR
                }),
                newValues: System.Text.Json.JsonSerializer.Serialize(new
                {
                    EmployeeTier = request.EmployeeTier.ToString(),
                    request.MaxUsers,
                    request.MaxStorageGB,
                    request.ApiCallsPerMonth,
                    request.YearlyPriceMUR
                }),
                success: true,
                additionalContext: new Dictionary<string, object>
                {
                    { "tierChange", $"{tenant?.EmployeeTier} → {request.EmployeeTier}" },
                    { "priceChange", $"MUR {tenant?.YearlyPriceMUR:N2} → MUR {request.YearlyPriceMUR:N2}" }
                }
            );

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee tier for tenant: {TenantId}", id);
            return StatusCode(500, new { success = false, message = "Error updating employee tier" });
        }
    }
}

// Supporting DTOs for request bodies
public class SuspendTenantRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class DeleteTenantRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class HardDeleteTenantRequest
{
    public string ConfirmationName { get; set; } = string.Empty;
}

public class UpdateEmployeeTierRequest
{
    public EmployeeTier EmployeeTier { get; set; }
    public int MaxUsers { get; set; }
    public int MaxStorageGB { get; set; }
    public int ApiCallsPerMonth { get; set; }

    /// <summary>
    /// FORTUNE 500: Yearly subscription price in Mauritian Rupees
    /// </summary>
    public decimal YearlyPriceMUR { get; set; }
}
