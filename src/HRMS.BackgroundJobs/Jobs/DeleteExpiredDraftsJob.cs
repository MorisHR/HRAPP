using HRMS.Application.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.BackgroundJobs.Jobs;

/// <summary>
/// Background job to delete expired employee drafts (>30 days old)
/// Runs daily at 2:00 AM to clean up stale drafts
/// Frees up database space and maintains data hygiene
/// </summary>
public class DeleteExpiredDraftsJob
{
    private readonly ILogger<DeleteExpiredDraftsJob> _logger;
    private readonly MasterDbContext _masterContext;

    public DeleteExpiredDraftsJob(
        ILogger<DeleteExpiredDraftsJob> logger,
        MasterDbContext masterContext)
    {
        _logger = logger;
        _masterContext = masterContext;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Delete Expired Drafts Job started at {Time}", DateTime.UtcNow);

        try
        {
            // Get all active tenants
            var tenants = await _masterContext.Tenants
                .Where(t => t.Status == Core.Enums.TenantStatus.Active && !t.IsDeleted)
                .ToListAsync();

            int totalDraftsDeleted = 0;

            foreach (var tenant in tenants)
            {
                _logger.LogInformation("Checking expired drafts for tenant: {TenantName}", tenant.CompanyName);

                var deletedCount = await DeleteExpiredDraftsForTenantAsync(tenant.SchemaName);
                totalDraftsDeleted += deletedCount;

                if (deletedCount > 0)
                {
                    _logger.LogInformation(
                        "Deleted {Count} expired draft(s) for tenant {TenantName}",
                        deletedCount, tenant.CompanyName);
                }
            }

            _logger.LogInformation(
                "Delete Expired Drafts Job completed. Total drafts deleted: {Count}",
                totalDraftsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Delete Expired Drafts Job");
            throw;
        }
    }

    private async Task<int> DeleteExpiredDraftsForTenantAsync(string schemaName)
    {
        try
        {
            // Create tenant-specific context
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            var connectionString = _masterContext.Database.GetConnectionString();
            optionsBuilder.UseNpgsql(connectionString);

            using var tenantContext = new TenantDbContext(optionsBuilder.Options, schemaName);

            // Find all expired drafts (ExpiresAt < now and not already deleted)
            var expiredDrafts = await tenantContext.EmployeeDrafts
                .Where(d => !d.IsDeleted && d.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expiredDrafts.Count == 0)
            {
                return 0;
            }

            // Soft delete all expired drafts
            foreach (var draft in expiredDrafts)
            {
                draft.IsDeleted = true;
                draft.DeletedAt = DateTime.UtcNow;

                _logger.LogDebug(
                    "Marking draft {DraftId} ({DraftName}) as deleted - expired {DaysAgo} days ago",
                    draft.Id,
                    draft.DraftName,
                    (DateTime.UtcNow - draft.ExpiresAt).Days);
            }

            await tenantContext.SaveChangesAsync();

            return expiredDrafts.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expired drafts for tenant schema: {SchemaName}", schemaName);
            return 0;
        }
    }
}
