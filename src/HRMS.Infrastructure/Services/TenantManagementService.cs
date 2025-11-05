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
    /// Uses database transactions to ensure data integrity
    /// </summary>
    public async Task<(bool Success, string Message, TenantDto? Tenant)> CreateTenantAsync(CreateTenantRequest request, string createdBy)
    {
        // Use execution strategy to handle retries and transactions together
        var strategy = _masterDbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Start database transaction for atomicity
            await using var transaction = await _masterDbContext.Database.BeginTransactionAsync();

            try
            {
            _logger.LogInformation("✓ Step 1/5: Starting tenant creation for subdomain: {Subdomain}", request.Subdomain);

            // Validate subdomain uniqueness
            if (await _masterDbContext.Tenants.AnyAsync(t => t.Subdomain == request.Subdomain.ToLower()))
            {
                _logger.LogWarning("Tenant creation failed: Subdomain '{Subdomain}' already exists", request.Subdomain);
                return (false, $"A tenant with subdomain '{request.Subdomain}' already exists. Please choose a different subdomain.", null);
            }

            // Generate schema name
            var schemaName = $"tenant_{request.Subdomain.ToLower()}";

            // Check if schema already exists (orphaned from previous failed attempt)
            _logger.LogInformation("✓ Step 2/5: Checking for orphaned schemas...");
            if (await _schemaProvisioningService.SchemaExistsAsync(schemaName))
            {
                _logger.LogWarning("Orphaned schema '{SchemaName}' found. Cleaning up before proceeding...", schemaName);

                // Drop orphaned schema
                var dropped = await _schemaProvisioningService.DropTenantSchemaAsync(schemaName);
                if (!dropped)
                {
                    _logger.LogError("Failed to drop orphaned schema: {SchemaName}", schemaName);
                    return (false, "Failed to clean up orphaned data. Please contact support.", null);
                }

                _logger.LogInformation("Orphaned schema cleaned up successfully");
            }

            // Create tenant entity
            _logger.LogInformation("✓ Step 3/5: Creating tenant record in master database...");
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                CompanyName = request.CompanyName,
                Subdomain = request.Subdomain.ToLower(),
                SchemaName = schemaName,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                Status = TenantStatus.Active,
                EmployeeTier = request.EmployeeTier,
                MonthlyPrice = request.MonthlyPrice,
                MaxUsers = request.MaxUsers,
                MaxStorageGB = request.MaxStorageGB,
                ApiCallsPerMonth = request.ApiCallsPerMonth,
                SubscriptionStartDate = DateTime.UtcNow,
                AdminUserName = request.AdminUserName,
                AdminEmail = request.AdminEmail,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                CurrentUserCount = 0,
                CurrentStorageGB = 0
            };

            // Add tenant to master database
            await _masterDbContext.Tenants.AddAsync(tenant);
            await _masterDbContext.SaveChangesAsync();

            _logger.LogInformation("Tenant record created: TenantId={TenantId}", tenant.Id);

            // Create tenant schema and apply migrations
            _logger.LogInformation("✓ Step 4/5: Creating tenant database schema '{SchemaName}'...", schemaName);
            var schemaCreated = await _schemaProvisioningService.CreateTenantSchemaAsync(schemaName);

            if (!schemaCreated)
            {
                _logger.LogError("❌ Schema creation failed for tenant: {TenantId}. Rolling back transaction...", tenant.Id);

                // Transaction will auto-rollback tenant record
                await transaction.RollbackAsync();

                // Manually drop schema if it was partially created
                await _schemaProvisioningService.DropTenantSchemaAsync(schemaName);

                return (false, "Failed to create tenant database. The operation has been rolled back. Please try again.", null);
            }

            _logger.LogInformation("Schema created and migrations applied successfully");

            // Commit transaction - all or nothing
            _logger.LogInformation("✓ Step 5/5: Committing transaction...");
            await transaction.CommitAsync();

            _logger.LogInformation("✅ SUCCESS: Tenant '{CompanyName}' created successfully! Subdomain: {Subdomain}",
                request.CompanyName, request.Subdomain);

            // TODO: Send welcome email with login credentials

            var tenantDto = MapToDto(tenant);
            return (true, $"Tenant created successfully! Login URL: https://{request.Subdomain}.hrms.com", tenantDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ FAILED: Error during tenant creation for '{CompanyName}'. Rolling back all changes...", request.CompanyName);

            // Rollback transaction
            await transaction.RollbackAsync();

            // Try to clean up any partially created schema
            var schemaName = $"tenant_{request.Subdomain.ToLower()}";
            try
            {
                await _schemaProvisioningService.DropTenantSchemaAsync(schemaName);
                _logger.LogInformation("Cleaned up partially created schema during rollback");
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to clean up schema during rollback (this is non-critical)");
            }

            // Return user-friendly error message
            var errorMessage = ex.InnerException?.Message ?? ex.Message;
            return (false, $"Tenant creation failed: {errorMessage}. All changes have been rolled back. Please try again or contact support if the problem persists.", null);
        }
        });
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
    /// Update tenant employee tier and pricing
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateEmployeeTierAsync(
        Guid tenantId,
        EmployeeTier newTier,
        int maxUsers,
        int maxStorageGB,
        int apiCallsPerMonth,
        decimal monthlyPrice,
        string updatedBy)
    {
        var tenant = await _masterDbContext.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return (false, "Tenant not found");

        tenant.EmployeeTier = newTier;
        tenant.MonthlyPrice = monthlyPrice;
        tenant.MaxUsers = maxUsers;
        tenant.MaxStorageGB = maxStorageGB;
        tenant.ApiCallsPerMonth = apiCallsPerMonth;
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.UpdatedBy = updatedBy;

        await _masterDbContext.SaveChangesAsync();

        _logger.LogInformation("Tenant employee tier updated: {TenantId}, New Tier: {Tier}", tenantId, newTier);

        return (true, "Employee tier updated successfully");
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
            EmployeeTier = tenant.EmployeeTier,
            EmployeeTierDisplay = GetTierDisplayName(tenant.EmployeeTier),
            MonthlyPrice = tenant.MonthlyPrice,
            MaxUsers = tenant.MaxUsers,
            CurrentUserCount = tenant.CurrentUserCount,
            MaxStorageGB = tenant.MaxStorageGB,
            CurrentStorageGB = tenant.CurrentStorageGB,
            ApiCallsPerMonth = tenant.ApiCallsPerMonth,
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

    private string GetTierDisplayName(EmployeeTier tier)
    {
        return tier switch
        {
            EmployeeTier.Tier1 => "1-50 Employees",
            EmployeeTier.Tier2 => "51-100 Employees",
            EmployeeTier.Tier3 => "101-200 Employees",
            EmployeeTier.Tier4 => "201-500 Employees",
            EmployeeTier.Tier5 => "501-1000 Employees",
            EmployeeTier.Custom => "1000+ Employees (Custom)",
            _ => tier.ToString()
        };
    }
}
