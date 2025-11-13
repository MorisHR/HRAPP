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

    public TenantsController(
        TenantManagementService tenantManagementService,
        MasterDbContext context,
        ILogger<TenantsController> logger,
        IEmailService emailService,
        IConfiguration configuration,
        IAuditLogService auditLogService)
    {
        _tenantManagementService = tenantManagementService;
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
        _auditLogService = auditLogService;
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
