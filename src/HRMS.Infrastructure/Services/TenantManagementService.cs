using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
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
    private readonly ISubscriptionManagementService? _subscriptionService;
    private readonly ILogger<TenantManagementService> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IEncryptionService _encryptionService;

    public TenantManagementService(
        MasterDbContext masterDbContext,
        ISchemaProvisioningService schemaProvisioningService,
        ILogger<TenantManagementService> logger,
        IEmailService emailService,
        IConfiguration configuration,
        IEncryptionService encryptionService,
        ISubscriptionManagementService? subscriptionService = null)
    {
        _masterDbContext = masterDbContext;
        _schemaProvisioningService = schemaProvisioningService;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
        _encryptionService = encryptionService;
        _subscriptionService = subscriptionService; // Optional to avoid circular dependency during startup
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
            // CONCURRENCY FIX: Use Serializable isolation to prevent phantom reads
            // This ensures subdomain uniqueness check is atomic
            await using var transaction = await _masterDbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
            _logger.LogInformation("✓ Step 1/5: Starting tenant creation for subdomain: {Subdomain}", request.Subdomain);

            // Validate subdomain uniqueness
            // With Serializable isolation, this check is atomic and prevents race conditions
            if (await _masterDbContext.Tenants.AnyAsync(t => t.Subdomain == request.Subdomain.ToLower()))
            {
                _logger.LogWarning("Tenant creation failed: Subdomain '{Subdomain}' already exists", request.Subdomain);
                return (false, $"A tenant with subdomain '{request.Subdomain}' already exists. Please choose a different subdomain.", (TenantDto?)null);
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
                    return (false, "Failed to clean up orphaned data. Please contact support.", (TenantDto?)null);
                }

                _logger.LogInformation("Orphaned schema cleaned up successfully");
            }

            // Create tenant entity
            _logger.LogInformation("✓ Step 3/5: Creating tenant record in master database...");

            // Generate activation token
            var activationToken = Guid.NewGuid().ToString("N");
            var activationExpiry = DateTime.UtcNow.AddHours(24);

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                CompanyName = request.CompanyName,
                Subdomain = request.Subdomain.ToLower(),
                SchemaName = schemaName,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                Status = TenantStatus.Pending, // Changed: Pending activation
                EmployeeTier = request.EmployeeTier,
                YearlyPriceMUR = request.YearlyPriceMUR,
                MaxUsers = request.MaxUsers,
                MaxStorageGB = request.MaxStorageGB,
                ApiCallsPerMonth = request.ApiCallsPerMonth,
                SubscriptionStartDate = DateTime.UtcNow,
                AdminUserName = request.AdminUserName,
                AdminEmail = request.AdminEmail,
                AdminFirstName = request.AdminFirstName,
                AdminLastName = request.AdminLastName,
                IsGovernmentEntity = request.IsGovernmentEntity,
                // FORTUNE 500: Industry sector for compliance
                SectorId = request.SectorId,
                SectorSelectedAt = request.SectorId.HasValue ? DateTime.UtcNow : null,
                TrialEndDate = request.TrialEndDate,
                SubscriptionEndDate = request.SubscriptionEndDate,
                // Activation fields
                ActivationToken = activationToken,
                ActivationTokenExpiry = activationExpiry,
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

                return (false, "Failed to create tenant database. The operation has been rolled back. Please try again.", (TenantDto?)null);
            }

            _logger.LogInformation("Schema created and migrations applied successfully");

            // Commit transaction - all or nothing
            _logger.LogInformation("✓ Step 5/5: Committing transaction...");
            await transaction.CommitAsync();

            _logger.LogInformation("✅ SUCCESS: Tenant '{CompanyName}' created successfully! Subdomain: {Subdomain}",
                request.CompanyName, request.Subdomain);

            // ============================================
            // FORTUNE 500: Auto-create first subscription payment
            // ============================================
            if (_subscriptionService != null && tenant.YearlyPriceMUR > 0)
            {
                try
                {
                    var subscriptionEndDate = tenant.SubscriptionEndDate ?? DateTime.UtcNow.AddYears(1);
                    tenant.SubscriptionEndDate = subscriptionEndDate;

                    var payment = await _subscriptionService.CreatePaymentRecordAsync(
                        tenantId: tenant.Id,
                        periodStart: DateTime.UtcNow,
                        periodEnd: subscriptionEndDate,
                        amountMUR: tenant.YearlyPriceMUR,
                        dueDate: DateTime.UtcNow.AddDays(30), // Due in 30 days
                        tier: tenant.EmployeeTier,
                        description: "Initial subscription payment - First year",
                        calculateTax: true);

                    _logger.LogInformation("💰 Auto-created initial payment: MUR {Amount:N2}, Due: {DueDate:yyyy-MM-dd}",
                        payment.TotalMUR, payment.DueDate);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create initial payment for tenant {TenantId} - non-critical", tenant.Id);
                    // Don't fail tenant creation if payment creation fails
                }
            }

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
            return (false, $"Tenant creation failed: {errorMessage}. All changes have been rolled back. Please try again or contact support if the problem persists.", (TenantDto?)null);
        }
        });
    }

    /// <summary>
    /// Get all tenants
    /// FORTUNE 500 OPTIMIZATION: Include Sector with single query (LEFT JOIN)
    /// </summary>
    public async Task<List<TenantDto>> GetAllTenantsAsync()
    {
        var tenants = await _masterDbContext.Tenants
            .Include(t => t.Sector) // FORTUNE 500: Single query with LEFT JOIN
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tenants.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Get tenant by ID
    /// FORTUNE 500 OPTIMIZATION: Include Sector with single query (LEFT JOIN)
    /// </summary>
    public async Task<TenantDto?> GetTenantByIdAsync(Guid tenantId)
    {
        var tenant = await _masterDbContext.Tenants
            .Include(t => t.Sector) // FORTUNE 500: Single query with LEFT JOIN
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
    /// FORTUNE 500: Supports pro-rated upgrades
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateEmployeeTierAsync(
        Guid tenantId,
        EmployeeTier newTier,
        int maxUsers,
        int maxStorageGB,
        int apiCallsPerMonth,
        decimal yearlyPriceMUR,
        string updatedBy)
    {
        var tenant = await _masterDbContext.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return (false, "Tenant not found");

        var oldTier = tenant.EmployeeTier;
        var oldPrice = tenant.YearlyPriceMUR;

        // Update tenant
        tenant.EmployeeTier = newTier;
        tenant.YearlyPriceMUR = yearlyPriceMUR;
        tenant.MaxUsers = maxUsers;
        tenant.MaxStorageGB = maxStorageGB;
        tenant.ApiCallsPerMonth = apiCallsPerMonth;
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.UpdatedBy = updatedBy;

        await _masterDbContext.SaveChangesAsync();

        // ============================================
        // FORTUNE 500: Create pro-rated payment for upgrades
        // ============================================
        if (_subscriptionService != null && yearlyPriceMUR > oldPrice && tenant.SubscriptionEndDate.HasValue)
        {
            try
            {
                var proRatedPayment = await _subscriptionService.CreateProRatedPaymentAsync(
                    tenantId,
                    newTier,
                    yearlyPriceMUR,
                    $"Tier upgrade: {oldTier} → {newTier}");

                if (proRatedPayment != null)
                {
                    _logger.LogInformation(
                        "💰 Pro-rated payment created for tier upgrade: MUR {Amount:N2}",
                        proRatedPayment.TotalMUR);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create pro-rated payment for tier upgrade - non-critical");
                // Don't fail tier update if pro-rated payment fails
            }
        }

        _logger.LogInformation("Tenant employee tier updated: {TenantId}, {OldTier} → {NewTier}", tenantId, oldTier, newTier);

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
            YearlyPriceMUR = tenant.YearlyPriceMUR,
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
            AdminEmail = tenant.AdminEmail,
            // FORTUNE 500: Industry sector data (denormalized for performance)
            SectorId = tenant.SectorId,
            SectorCode = tenant.Sector?.SectorCode,
            SectorName = tenant.Sector?.SectorName,
            SectorSelectedAt = tenant.SectorSelectedAt
        };
    }

    /// <summary>
    /// Get tenant by activation token
    /// </summary>
    public async Task<Tenant?> GetTenantByActivationTokenAsync(string activationToken)
    {
        return await _masterDbContext.Tenants
            .FirstOrDefaultAsync(t => t.ActivationToken == activationToken);
    }

    /// <summary>
    /// Activate tenant account and create admin user
    /// </summary>
    public async Task<(bool Success, string Message, string? Subdomain)> ActivateTenantAsync(string activationToken)
    {
        // Use execution strategy
        var strategy = _masterDbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // CONCURRENCY FIX: Use Serializable isolation for tenant activation
            await using var transaction = await _masterDbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                _logger.LogInformation("Starting tenant activation for token: {Token}", activationToken.Substring(0, 8) + "...");

                // Find tenant by activation token
                var tenant = await _masterDbContext.Tenants
                    .FirstOrDefaultAsync(t => t.ActivationToken == activationToken);

                if (tenant == null)
                {
                    _logger.LogWarning("Activation failed: Invalid activation token");
                    return (false, "Invalid activation token", (string?)null);
                }

                // Check if already activated
                if (tenant.Status == TenantStatus.Active)
                {
                    _logger.LogWarning("Activation failed: Tenant {Subdomain} already activated", tenant.Subdomain);
                    return (false, "Tenant account is already activated", tenant.Subdomain);
                }

                // Check token expiry
                if (tenant.ActivationTokenExpiry < DateTime.UtcNow)
                {
                    _logger.LogWarning("Activation failed: Token expired for tenant {Subdomain}", tenant.Subdomain);
                    return (false, "Activation link has expired. Please contact support.", (string?)null);
                }

                // Create tenant schema if not exists (in case it was only partially created)
                _logger.LogInformation("Ensuring tenant schema exists: {SchemaName}", tenant.SchemaName);
                if (!await _schemaProvisioningService.SchemaExistsAsync(tenant.SchemaName))
                {
                    _logger.LogInformation("Schema does not exist. Creating now...");
                    var schemaCreated = await _schemaProvisioningService.CreateTenantSchemaAsync(tenant.SchemaName);
                    if (!schemaCreated)
                    {
                        _logger.LogError("Failed to create tenant schema during activation");
                        return (false, "Failed to provision tenant database. Please contact support.", (string?)null);
                    }
                }

                // ==============================================
                // FORTUNE 500: Create admin employee with secure password setup
                // ==============================================
                _logger.LogInformation("Creating admin employee for tenant {Subdomain}", tenant.Subdomain);

                // Connect to tenant schema to create admin employee
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
                optionsBuilder.UseNpgsql(connectionString);
                var tenantDbContext = new TenantDbContext(optionsBuilder.Options, tenant.SchemaName, _encryptionService);

                // Check if admin employee already exists (idempotency)
                var existingAdmin = await tenantDbContext.Employees
                    .FirstOrDefaultAsync(e => e.Email == tenant.AdminEmail && e.IsAdmin);

                string? passwordResetToken = null;

                if (existingAdmin == null)
                {
                    // Generate secure password reset token (32-char hex)
                    passwordResetToken = PasswordValidationService.GenerateSecureToken();

                    // Extract admin name from email or use tenant name
                    var adminFirstName = tenant.AdminFirstName ?? tenant.CompanyName;
                    var adminLastName = tenant.AdminLastName ?? "Administrator";

                    // Create admin employee
                    var adminEmployee = new Core.Entities.Tenant.Employee
                    {
                        Id = Guid.NewGuid(),
                        EmployeeCode = "ADMIN001", // Standard admin code
                        FirstName = adminFirstName,
                        LastName = adminLastName,
                        Email = tenant.AdminEmail,
                        PhoneNumber = tenant.ContactPhone ?? string.Empty,
                        DateOfBirth = DateTime.UtcNow.AddYears(-30), // Default DOB
                        Gender = Core.Enums.Gender.PreferNotToSay,
                        MaritalStatus = Core.Enums.MaritalStatus.Single,
                        Nationality = "Mauritius",
                        EmployeeType = Core.Enums.EmployeeType.Local,
                        JobTitle = "System Administrator",
                        JoiningDate = DateTime.UtcNow,
                        IsActive = true,

                        // Security fields
                        IsAdmin = true,
                        MustChangePassword = true, // Force password change on first login
                        PasswordResetToken = passwordResetToken,
                        PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(24),
                        LockoutEnabled = true,
                        AccessFailedCount = 0,

                        // Address (required fields)
                        AddressLine1 = "To be updated",
                        Country = "Mauritius",

                        // Salary (default)
                        BasicSalary = 0, // To be set by admin later
                        SalaryCurrency = "MUR",
                        PaymentFrequency = "Monthly",

                        // Leave balances (default entitlements)
                        AnnualLeaveDays = 20,
                        SickLeaveDays = 15,
                        CasualLeaveDays = 5,
                        AnnualLeaveBalance = 20,
                        SickLeaveBalance = 15,
                        CasualLeaveBalance = 5,
                        CarryForwardAllowed = true,

                        // Audit fields
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "system_tenant_activation",
                        IsDeleted = false
                    };

                    tenantDbContext.Employees.Add(adminEmployee);
                    await tenantDbContext.SaveChangesAsync();

                    _logger.LogInformation("✓ Admin employee created: {Email} with password reset token", tenant.AdminEmail);
                }
                else
                {
                    _logger.LogInformation("Admin employee already exists for {Email}, skipping creation", tenant.AdminEmail);

                    // Generate new password reset token for existing admin
                    passwordResetToken = PasswordValidationService.GenerateSecureToken();
                    existingAdmin.PasswordResetToken = passwordResetToken;
                    existingAdmin.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(24);
                    existingAdmin.MustChangePassword = true;
                    await tenantDbContext.SaveChangesAsync();
                }

                // Update tenant status
                tenant.Status = TenantStatus.Active;
                tenant.ActivatedAt = DateTime.UtcNow;
                tenant.ActivationToken = null; // Clear token for security
                tenant.ActivationTokenExpiry = null;
                tenant.UpdatedAt = DateTime.UtcNow;

                await _masterDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("✓ Tenant {Subdomain} activated successfully with admin employee", tenant.Subdomain);

                // Send welcome email with password reset link
                if (!string.IsNullOrEmpty(passwordResetToken))
                {
                    await _emailService.SendTenantWelcomeEmailAsync(
                        tenant.AdminEmail,
                        tenant.AdminFirstName ?? tenant.CompanyName,
                        tenant.Subdomain,
                        passwordResetToken);
                }

                return (true, "Tenant activated successfully", tenant.Subdomain);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error activating tenant");
                return (false, "An error occurred during activation. Please try again.", (string?)null);
            }
        });
    }

    // Note: Admin user creation is handled separately through the employee onboarding flow
    // after tenant activation. The controller will send welcome email with login instructions.

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
