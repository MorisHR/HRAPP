using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HRMS.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that captures daily dashboard statistics snapshots
/// FORTUNE 500 PATTERN: AWS CloudWatch Metrics, Datadog, New Relic
/// Runs daily at midnight UTC to capture platform health metrics
/// PRODUCTION-READY: No patches, proper error handling, full audit trail
/// </summary>
public class DashboardSnapshotJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DashboardSnapshotJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    public DashboardSnapshotJob(
        IServiceProvider serviceProvider,
        ILogger<DashboardSnapshotJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dashboard Snapshot Job started - Fortune 500 historical tracking");

        // Calculate time until next midnight UTC
        var now = DateTime.UtcNow;
        var nextMidnight = now.Date.AddDays(1);
        var initialDelay = nextMidnight - now;

        _logger.LogInformation(
            "First snapshot will be captured at {NextRun} (in {Hours}h {Minutes}m)",
            nextMidnight,
            initialDelay.Hours,
            initialDelay.Minutes);

        // Wait until midnight UTC for first run
        try
        {
            await Task.Delay(initialDelay, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Dashboard Snapshot Job cancelled during initial delay");
            return;
        }

        // Run daily at midnight UTC
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CaptureSnapshotAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing dashboard snapshot - will retry in 24 hours");
            }

            // Wait 24 hours until next snapshot
            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Dashboard Snapshot Job cancelled");
                break;
            }
        }
    }

    private async Task<int> GetTenantEmployeeCountAsync(MasterDbContext context, Guid tenantId, string schemaName, CancellationToken cancellationToken)
    {
        try
        {
            // Query the tenant schema directly for employee count
            var sql = $@"
                SELECT COUNT(*)
                FROM ""{schemaName}"".""Employees""
                WHERE ""IsDeleted"" = FALSE AND ""IsActive"" = TRUE";

            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null ? Convert.ToInt32(result) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get employee count for tenant {TenantId} schema {SchemaName}",
                tenantId, schemaName);
            return 0;
        }
    }

    private async Task CaptureSnapshotAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        _logger.LogInformation("Capturing dashboard statistics snapshot for {Date}", DateTime.UtcNow.Date);

        try
        {
            // Check if snapshot for today already exists
            var today = DateTime.UtcNow.Date;
            var existingSnapshot = await context.DashboardStatisticsSnapshots
                .FirstOrDefaultAsync(s => s.SnapshotDate.Date == today, cancellationToken);

            if (existingSnapshot != null)
            {
                _logger.LogWarning("Snapshot for {Date} already exists (ID: {Id}), skipping", today, existingSnapshot.Id);
                return;
            }

            var now = DateTime.UtcNow;

            // ==================================================================
            // TENANT STATISTICS
            // ==================================================================
            var totalTenants = await context.Tenants
                .CountAsync(t => !t.IsDeleted, cancellationToken);

            var activeTenants = await context.Tenants
                .CountAsync(t => t.Status == TenantStatus.Active && !t.IsDeleted, cancellationToken);

            var trialTenants = await context.Tenants
                .CountAsync(t => t.Status == TenantStatus.Trial && !t.IsDeleted, cancellationToken);

            var suspendedTenants = await context.Tenants
                .CountAsync(t => t.Status == TenantStatus.Suspended && !t.IsDeleted, cancellationToken);

            var expiredTenants = await context.Tenants
                .CountAsync(t => t.Status == TenantStatus.Expired && !t.IsDeleted, cancellationToken);

            // New tenants today
            var newTenantsToday = await context.Tenants
                .CountAsync(t => t.CreatedAt.Date == today && !t.IsDeleted, cancellationToken);

            // Churned tenants today (soft deleted today)
            var churnedTenantsToday = await context.Tenants
                .CountAsync(t => t.DeletedAt.HasValue && t.DeletedAt.Value.Date == today, cancellationToken);

            // ==================================================================
            // EMPLOYEE COUNT (Cross all active tenants)
            // ==================================================================
            var activeTenantsWithSchema = await context.Tenants
                .Where(t => t.Status == TenantStatus.Active && !t.IsDeleted)
                .Select(t => new { t.Id, t.SchemaName })
                .ToListAsync(cancellationToken);

            int totalEmployees = 0;
            foreach (var tenant in activeTenantsWithSchema)
            {
                var empCount = await GetTenantEmployeeCountAsync(context, tenant.Id, tenant.SchemaName, cancellationToken);
                totalEmployees += empCount;
            }

            _logger.LogInformation("Calculated total employees: {Count} across {TenantCount} active tenants",
                totalEmployees, activeTenantsWithSchema.Count);

            // ==================================================================
            // REVENUE STATISTICS
            // ==================================================================
            // Calculate MRR from yearly prices (YearlyPriceMUR / 12)
            var activeTenantsList = await context.Tenants
                .Where(t => t.Status == TenantStatus.Active && !t.IsDeleted)
                .Select(t => t.YearlyPriceMUR)
                .ToListAsync(cancellationToken);

            var monthlyRevenue = activeTenantsList.Sum(yearly => yearly / 12m);

            _logger.LogInformation("Calculated MRR: {MRR} MUR from {TenantCount} active tenants",
                monthlyRevenue, activeTenantsList.Count);

            // ==================================================================
            // STORAGE STATISTICS
            // ==================================================================
            var storageSnapshots = await context.TenantStorageSnapshots
                .Where(s => s.SnapshotDate.Date == today)
                .ToListAsync(cancellationToken);

            var totalStorageGB = storageSnapshots.Sum(s => s.TotalStorageGB);
            var avgStorageUsage = storageSnapshots.Any()
                ? storageSnapshots.Average(s => s.UsagePercentage)
                : 0;

            // ==================================================================
            // CREATE SNAPSHOT
            // ==================================================================
            var snapshot = new DashboardStatisticsSnapshot
            {
                Id = Guid.NewGuid(),
                SnapshotDate = today,
                TotalTenants = totalTenants,
                ActiveTenants = activeTenants,
                TotalEmployees = totalEmployees,
                MonthlyRevenue = monthlyRevenue,
                TrialTenants = trialTenants,
                SuspendedTenants = suspendedTenants,
                ExpiredTenants = expiredTenants,
                TotalStorageGB = totalStorageGB,
                AverageStorageUsagePercent = avgStorageUsage,
                NewTenantsToday = newTenantsToday,
                ChurnedTenantsToday = churnedTenantsToday,
                IsAutomatic = true,
                Notes = "Automated daily snapshot - Production-grade Fortune 500 metrics",
                CreatedAt = now,
                UpdatedAt = now
            };

            context.DashboardStatisticsSnapshots.Add(snapshot);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "✅ Dashboard snapshot captured successfully: {Tenants} tenants ({Active} active), {Employees} employees, {Revenue} MUR MRR",
                totalTenants,
                activeTenants,
                totalEmployees,
                monthlyRevenue.ToString("N2"));

            // Log detailed metrics for monitoring
            _logger.LogInformation(
                "📊 Snapshot details - Trial: {Trial}, Suspended: {Suspended}, Expired: {Expired}, New: {New}, Churned: {Churned}, Storage: {Storage} GB",
                trialTenants,
                suspendedTenants,
                expiredTenants,
                newTenantsToday,
                churnedTenantsToday,
                totalStorageGB.ToString("N2"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to capture dashboard snapshot - Critical error in production job");
            throw;
        }
    }

    /// <summary>
    /// Manually trigger a snapshot (for testing or manual backfill)
    /// PRODUCTION USE: Testing, data recovery, manual backfill
    /// </summary>
    public async Task CaptureManualSnapshotAsync(string notes = "Manual snapshot", CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        _logger.LogInformation("Capturing manual dashboard statistics snapshot");

        try
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            // Calculate all statistics (same logic as automated)
            var totalTenants = await context.Tenants.CountAsync(t => !t.IsDeleted, cancellationToken);
            var activeTenants = await context.Tenants.CountAsync(t => t.Status == TenantStatus.Active && !t.IsDeleted, cancellationToken);
            var trialTenants = await context.Tenants.CountAsync(t => t.Status == TenantStatus.Trial && !t.IsDeleted, cancellationToken);
            var suspendedTenants = await context.Tenants.CountAsync(t => t.Status == TenantStatus.Suspended && !t.IsDeleted, cancellationToken);
            var expiredTenants = await context.Tenants.CountAsync(t => t.Status == TenantStatus.Expired && !t.IsDeleted, cancellationToken);

            // Employee count
            var activeTenantsWithSchema = await context.Tenants
                .Where(t => t.Status == TenantStatus.Active && !t.IsDeleted)
                .Select(t => new { t.Id, t.SchemaName })
                .ToListAsync(cancellationToken);

            int totalEmployees = 0;
            foreach (var tenant in activeTenantsWithSchema)
            {
                var empCount = await GetTenantEmployeeCountAsync(context, tenant.Id, tenant.SchemaName, cancellationToken);
                totalEmployees += empCount;
            }

            // Revenue
            var activeTenantsList = await context.Tenants
                .Where(t => t.Status == TenantStatus.Active && !t.IsDeleted)
                .Select(t => t.YearlyPriceMUR)
                .ToListAsync(cancellationToken);

            var monthlyRevenue = activeTenantsList.Sum(yearly => yearly / 12m);

            var snapshot = new DashboardStatisticsSnapshot
            {
                Id = Guid.NewGuid(),
                SnapshotDate = now,
                TotalTenants = totalTenants,
                ActiveTenants = activeTenants,
                TotalEmployees = totalEmployees,
                MonthlyRevenue = monthlyRevenue,
                TrialTenants = trialTenants,
                SuspendedTenants = suspendedTenants,
                ExpiredTenants = expiredTenants,
                TotalStorageGB = 0, // Manual snapshots don't calculate storage (can be added if needed)
                AverageStorageUsagePercent = 0,
                NewTenantsToday = 0,
                ChurnedTenantsToday = 0,
                IsAutomatic = false,
                Notes = notes,
                CreatedAt = now,
                UpdatedAt = now
            };

            context.DashboardStatisticsSnapshots.Add(snapshot);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Manual snapshot captured successfully - {Tenants} tenants, {Employees} employees",
                totalTenants, totalEmployees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to capture manual snapshot");
            throw;
        }
    }
}
