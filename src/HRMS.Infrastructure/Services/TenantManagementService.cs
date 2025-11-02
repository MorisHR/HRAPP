using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRMS.Application.DTOs;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for managing tenant lifecycle operations
/// </summary>
public class TenantManagementService
{
    private readonly MasterDbContext _masterDbContext;
    private readonly ISchemaProvisioningService _schemaProvisioningService;
    private readonly ILogger<TenantManagementService> _logger;

    public TenantManagementService(
        MasterDbContext masterDbContext,
        ISchemaProvisioningService schemaProvisioningService,
        ILogger<TenantManagementService> logger)
    {
        _masterDbContext = masterDbContext;
        _schemaProvisioningService = schemaProvisioningService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new tenant with automatic schema creation
    /// </summary>
    public async Task<(bool Success, string Message, TenantDto? Tenant)> CreateTenantAsync(CreateTenantRequest request, string createdBy)
    {
        try
        {
            _logger.LogInformation("Creating new tenant: {CompanyName}, Subdomain: {Subdomain}", request.CompanyName, request.Subdomain);

            // Validate subdomain uniqueness
            if (await _masterDbContext.Tenants.AnyAsync(t => t.Subdomain == request.Subdomain.ToLower()))
            {
                return (false, "Subdomain already exists", null);
            }

            // Generate schema name
            var schemaName = $"tenant_{request.Subdomain.ToLower()}";

            // Check if schema already exists
            if (await _schemaProvisioningService.SchemaExistsAsync(schemaName))
            {
                return (false, "Schema already exists", null);
            }

            // Create tenant entity
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                CompanyName = request.CompanyName,
                Subdomain = request.Subdomain.ToLower(),
                SchemaName = schemaName,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                Status = TenantStatus.Active,
                SubscriptionPlan = request.SubscriptionPlan,
                MaxUsers = request.MaxUsers,
                MaxStorageBytes = request.MaxStorageBytes,
                MaxApiCallsPerHour = request.MaxApiCallsPerHour,
                SubscriptionStartDate = DateTime.UtcNow,
                AdminUserName = request.AdminUserName,
                AdminEmail = request.AdminEmail,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                CurrentUserCount = 0,
                CurrentStorageBytes = 0
            };

            // Add tenant to master database
            await _masterDbContext.Tenants.AddAsync(tenant);
            await _masterDbContext.SaveChangesAsync();

            _logger.LogInformation("Tenant record created in master schema: {TenantId}", tenant.Id);

            // Create tenant schema and apply migrations
            var schemaCreated = await _schemaProvisioningService.CreateTenantSchemaAsync(schemaName);

            if (!schemaCreated)
            {
                _logger.LogError("Failed to create schema for tenant: {TenantId}", tenant.Id);
                // Rollback tenant creation
                _masterDbContext.Tenants.Remove(tenant);
                await _masterDbContext.SaveChangesAsync();
                return (false, "Failed to create tenant schema", null);
            }

            _logger.LogInformation("Tenant created successfully: {TenantId}, Schema: {SchemaName}", tenant.Id, schemaName);

            // TODO: Send welcome email with login credentials

            var tenantDto = MapToDto(tenant);
            return (true, "Tenant created successfully", tenantDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant: {CompanyName}", request.CompanyName);
            return (false, $"Error creating tenant: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Get all tenants
    /// </summary>
    public async Task<List<TenantDto>> GetAllTenantsAsync()
    {
        var tenants = await _masterDbContext.Tenants
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tenants.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    public async Task<TenantDto?> GetTenantByIdAsync(Guid tenantId)
    {
        var tenant = await _masterDbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        return tenant != null ? MapToDto(tenant) : null;
    }

    /// <summary>
    /// Suspend tenant (temporary block)
    /// </summary>
    public async Task<(bool Success, string Message)> SuspendTenantAsync(Guid tenantId, string reason, string suspendedBy)
    {
        var tenant = await _masterDbContext.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return (false, "Tenant not found");

        if (tenant.Status == TenantStatus.Suspended)
            return (false, "Tenant is already suspended");

        tenant.Status = TenantStatus.Suspended;
        tenant.SuspensionReason = reason;
        tenant.SuspensionDate = DateTime.UtcNow;
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.UpdatedBy = suspendedBy;

        await _masterDbContext.SaveChangesAsync();

        _logger.LogInformation("Tenant suspended: {TenantId}, Reason: {Reason}", tenantId, reason);

        return (true, "Tenant suspended successfully");
    }

    /// <summary>
    /// Soft delete tenant (mark for deletion with grace period)
    /// </summary>
    public async Task<(bool Success, string Message)> SoftDeleteTenantAsync(Guid tenantId, string reason, string deletedBy)
    {
        var tenant = await _masterDbContext.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return (false, "Tenant not found");

        if (tenant.Status == TenantStatus.SoftDeleted)
            return (false, "Tenant is already marked for deletion");

        tenant.Status = TenantStatus.SoftDeleted;
        tenant.SoftDeleteDate = DateTime.UtcNow;
        tenant.DeletionReason = reason;
        tenant.IsDeleted = true;
        tenant.DeletedAt = DateTime.UtcNow;
        tenant.DeletedBy = deletedBy;

        await _masterDbContext.SaveChangesAsync();

        _logger.LogWarning("Tenant soft deleted: {TenantId}, Reason: {Reason}, Grace Period: {Days} days",
            tenantId, reason, tenant.GracePeriodDays);

        return (true, $"Tenant marked for deletion. Will be permanently deleted in {tenant.GracePeriodDays} days.");
    }

    /// <summary>
    /// Reactivate suspended or soft-deleted tenant
    /// </summary>
    public async Task<(bool Success, string Message)> ReactivateTenantAsync(Guid tenantId, string reactivatedBy)
    {
        var tenant = await _masterDbContext.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null)
            return (false, "Tenant not found");

        if (tenant.Status == TenantStatus.Active)
            return (false, "Tenant is already active");

        tenant.Status = TenantStatus.Active;
        tenant.SuspensionReason = null;
        tenant.SuspensionDate = null;
        tenant.SoftDeleteDate = null;
        tenant.DeletionReason = null;
        tenant.IsDeleted = false;
        tenant.DeletedAt = null;
        tenant.DeletedBy = null;
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.UpdatedBy = reactivatedBy;

        await _masterDbContext.SaveChangesAsync();

        _logger.LogInformation("Tenant reactivated: {TenantId}", tenantId);

        // TODO: Send reactivation notification email

        return (true, "Tenant reactivated successfully");
    }

    /// <summary>
    /// Hard delete tenant (permanent - IRREVERSIBLE)
    /// </summary>
    public async Task<(bool Success, string Message)> HardDeleteTenantAsync(Guid tenantId, string deletedBy)
    {
        var tenant = await _masterDbContext.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null)
            return (false, "Tenant not found");

        if (!tenant.CanBeHardDeleted())
        {
            var daysRemaining = tenant.DaysUntilHardDelete();
            return (false, $"Tenant cannot be hard deleted yet. {daysRemaining} days remaining in grace period.");
        }

        _logger.LogWarning("Performing HARD DELETE (IRREVERSIBLE) for tenant: {TenantId}, Schema: {SchemaName}",
            tenantId, tenant.SchemaName);

        // TODO: Create backup before deletion

        // Drop the tenant schema
        var schemaDropped = await _schemaProvisioningService.DropTenantSchemaAsync(tenant.SchemaName);

        if (!schemaDropped)
        {
            _logger.LogError("Failed to drop schema for tenant: {TenantId}", tenantId);
            return (false, "Failed to drop tenant schema");
        }

        // Remove tenant record from master database
        _masterDbContext.Tenants.Remove(tenant);
        await _masterDbContext.SaveChangesAsync();

        _logger.LogWarning("Tenant HARD DELETED (PERMANENT): {TenantId}", tenantId);

        // TODO: Log audit entry for compliance

        return (true, "Tenant permanently deleted");
    }

    /// <summary>
    /// Update tenant subscription plan
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateSubscriptionPlanAsync(
        Guid tenantId,
        SubscriptionPlan newPlan,
        int maxUsers,
        long maxStorageBytes,
        int maxApiCallsPerHour,
        string updatedBy)
    {
        var tenant = await _masterDbContext.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return (false, "Tenant not found");

        tenant.SubscriptionPlan = newPlan;
        tenant.MaxUsers = maxUsers;
        tenant.MaxStorageBytes = maxStorageBytes;
        tenant.MaxApiCallsPerHour = maxApiCallsPerHour;
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.UpdatedBy = updatedBy;

        await _masterDbContext.SaveChangesAsync();

        _logger.LogInformation("Tenant subscription updated: {TenantId}, New Plan: {Plan}", tenantId, newPlan);

        return (true, "Subscription plan updated successfully");
    }

    private TenantDto MapToDto(Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            CompanyName = tenant.CompanyName,
            Subdomain = tenant.Subdomain,
            SchemaName = tenant.SchemaName,
            ContactEmail = tenant.ContactEmail,
            ContactPhone = tenant.ContactPhone,
            Status = tenant.Status,
            StatusDisplay = tenant.Status.ToString(),
            SubscriptionPlan = tenant.SubscriptionPlan,
            SubscriptionPlanDisplay = tenant.SubscriptionPlan.ToString(),
            MaxUsers = tenant.MaxUsers,
            CurrentUserCount = tenant.CurrentUserCount,
            MaxStorageBytes = tenant.MaxStorageBytes,
            CurrentStorageBytes = tenant.CurrentStorageBytes,
            MaxApiCallsPerHour = tenant.MaxApiCallsPerHour,
            CreatedAt = tenant.CreatedAt,
            SubscriptionStartDate = tenant.SubscriptionStartDate,
            SubscriptionEndDate = tenant.SubscriptionEndDate,
            TrialEndDate = tenant.TrialEndDate,
            SuspensionReason = tenant.SuspensionReason,
            SuspensionDate = tenant.SuspensionDate,
            SoftDeleteDate = tenant.SoftDeleteDate,
            DeletionReason = tenant.DeletionReason,
            DaysUntilHardDelete = tenant.DaysUntilHardDelete(),
            AdminUserName = tenant.AdminUserName,
            AdminEmail = tenant.AdminEmail
        };
    }
}
