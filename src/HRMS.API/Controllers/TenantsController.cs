using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HRMS.Application.DTOs;
using HRMS.Infrastructure.Services;
using HRMS.Core.Enums;

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
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        TenantManagementService tenantManagementService,
        ILogger<TenantsController> logger)
    {
        _tenantManagementService = tenantManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Get all tenants
    /// </summary>
    [HttpGet]
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
    /// Create a new tenant
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid request", errors = ModelState });

            var createdBy = "SuperAdmin"; // TODO: Get from authenticated user
            var (success, message, tenant) = await _tenantManagementService.CreateTenantAsync(request, createdBy);

            if (!success)
                return BadRequest(new { success = false, message });

            _logger.LogInformation("Tenant created successfully: {TenantId}", tenant!.Id);

            return CreatedAtAction(
                nameof(GetTenantById),
                new { id = tenant.Id },
                new { success = true, message, data = tenant });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return StatusCode(500, new { success = false, message = "Error creating tenant" });
        }
    }

    /// <summary>
    /// Suspend a tenant (temporary block)
    /// </summary>
    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> SuspendTenant(Guid id, [FromBody] SuspendTenantRequest request)
    {
        try
        {
            var suspendedBy = "SuperAdmin"; // TODO: Get from authenticated user
            var (success, message) = await _tenantManagementService.SuspendTenantAsync(id, request.Reason, suspendedBy);

            if (!success)
                return BadRequest(new { success = false, message });

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
    public async Task<IActionResult> SoftDeleteTenant(Guid id, [FromBody] DeleteTenantRequest request)
    {
        try
        {
            var deletedBy = "SuperAdmin"; // TODO: Get from authenticated user
            var (success, message) = await _tenantManagementService.SoftDeleteTenantAsync(id, request.Reason, deletedBy);

            if (!success)
                return BadRequest(new { success = false, message });

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
    public async Task<IActionResult> ReactivateTenant(Guid id)
    {
        try
        {
            var reactivatedBy = "SuperAdmin"; // TODO: Get from authenticated user
            var (success, message) = await _tenantManagementService.ReactivateTenantAsync(id, reactivatedBy);

            if (!success)
                return BadRequest(new { success = false, message });

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
    public async Task<IActionResult> HardDeleteTenant(Guid id, [FromBody] HardDeleteTenantRequest request)
    {
        try
        {
            // Require confirmation by typing tenant name
            var tenant = await _tenantManagementService.GetTenantByIdAsync(id);
            if (tenant == null)
                return NotFound(new { success = false, message = "Tenant not found" });

            if (!request.ConfirmationName.Equals(tenant.CompanyName, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Tenant name confirmation does not match" });

            var deletedBy = "SuperAdmin"; // TODO: Get from authenticated user
            var (success, message) = await _tenantManagementService.HardDeleteTenantAsync(id, deletedBy);

            if (!success)
                return BadRequest(new { success = false, message });

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
    public async Task<IActionResult> UpdateEmployeeTier(Guid id, [FromBody] UpdateEmployeeTierRequest request)
    {
        try
        {
            var updatedBy = "SuperAdmin"; // TODO: Get from authenticated user
            var (success, message) = await _tenantManagementService.UpdateEmployeeTierAsync(
                id,
                request.EmployeeTier,
                request.MaxUsers,
                request.MaxStorageGB,
                request.ApiCallsPerMonth,
                request.MonthlyPrice,
                updatedBy);

            if (!success)
                return BadRequest(new { success = false, message });

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
    public decimal MonthlyPrice { get; set; }
}
