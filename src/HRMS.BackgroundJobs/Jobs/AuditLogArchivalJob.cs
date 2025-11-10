using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using HRMS.Infrastructure.Data;
using HRMS.Core.Enums;

namespace HRMS.BackgroundJobs.Jobs;

/// <summary>
/// Background job to archive old audit logs for 10+ year retention compliance
/// Archives logs older than 2 years to cold storage (IsArchived flag)
/// Runs monthly to maintain optimal database performance
/// </summary>
public class AuditLogArchivalJob
{
    private readonly ILogger<AuditLogArchivalJob> _logger;
    private readonly IServiceProvider _serviceProvider;

    // Archive logs older than 2 years (keep hot data for performance)
    private const int ARCHIVE_THRESHOLD_DAYS = 730; // 2 years

    public AuditLogArchivalJob(
        ILogger<AuditLogArchivalJob> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("=== Audit Log Archival Job Started ===");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

            var archiveDate = DateTime.UtcNow.AddDays(-ARCHIVE_THRESHOLD_DAYS);

            _logger.LogInformation(
                "Archiving audit logs older than {ArchiveDate} ({Days} days)",
                archiveDate, ARCHIVE_THRESHOLD_DAYS);

            // Count logs to be archived
            var logsToArchive = await context.AuditLogs
                .Where(l => !l.IsArchived && l.PerformedAt < archiveDate)
                .CountAsync();

            if (logsToArchive == 0)
            {
                _logger.LogInformation("No audit logs to archive");
                return;
            }

            _logger.LogInformation("Found {Count} audit logs to archive", logsToArchive);

            // Archive in batches to avoid timeout
            const int BATCH_SIZE = 10000;
            var totalArchived = 0;

            while (true)
            {
                // Get batch of logs to archive
                var logsToUpdate = await context.AuditLogs
                    .Where(l => !l.IsArchived && l.PerformedAt < archiveDate)
                    .OrderBy(l => l.PerformedAt)
                    .Take(BATCH_SIZE)
                    .ToListAsync();

                if (!logsToUpdate.Any())
                    break;

                // Mark as archived
                foreach (var log in logsToUpdate)
                {
                    log.IsArchived = true;
                    log.ArchivedAt = DateTime.UtcNow;
                }

                await context.SaveChangesAsync();

                totalArchived += logsToUpdate.Count;

                _logger.LogInformation(
                    "Archived batch of {BatchSize} logs. Total archived: {TotalArchived}/{TotalToArchive}",
                    logsToUpdate.Count, totalArchived, logsToArchive);

                // Small delay between batches to avoid overloading database
                await Task.Delay(1000);
            }

            _logger.LogInformation(
                "=== Audit Log Archival Job Completed: {TotalArchived} logs archived ===",
                totalArchived);

            // Log this archival operation in audit trail
            context.AuditLogs.Add(new Core.Entities.Master.AuditLog
            {
                Id = Guid.NewGuid(),
                ActionType = AuditActionType.SYSTEM_CONFIGURATION_UPDATED,
                Category = AuditCategory.SYSTEM_EVENT,
                Severity = AuditSeverity.INFO,
                EntityType = "AuditLog",
                Success = true,
                PerformedAt = DateTime.UtcNow,
                UserEmail = "system@hrms.com",
                Reason = $"Audit log archival completed: {totalArchived} logs archived",
                AdditionalMetadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    ArchivedCount = totalArchived,
                    ArchiveThresholdDays = ARCHIVE_THRESHOLD_DAYS,
                    ArchiveDate = archiveDate
                })
            });

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving audit logs");
            throw;
        }
    }
}
