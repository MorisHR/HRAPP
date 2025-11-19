using HRMS.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace HRMS.BackgroundJobs.Jobs;

/// <summary>
/// Background job to create daily database backups
/// Runs daily at 2 AM (or configurable time)
/// Automatically cleans up old backups keeping only the last 30
/// </summary>
public class DatabaseBackupJob
{
    private readonly ILogger<DatabaseBackupJob> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DatabaseBackupJob(
        ILogger<DatabaseBackupJob> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("=== Database Backup Job Started ===");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var backupService = scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>();

            // Create backup
            var backupFilePath = await backupService.CreateBackupAsync();

            _logger.LogInformation("Database backup created successfully: {FilePath}", backupFilePath);

            // Clean up old backups (keep last 30)
            await backupService.CleanupOldBackupsAsync(keepCount: 30);

            _logger.LogInformation("=== Database Backup Job Completed Successfully ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database backup job failed");
            throw;
        }
    }
}
