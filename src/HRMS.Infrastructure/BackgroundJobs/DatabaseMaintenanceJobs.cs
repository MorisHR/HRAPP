using Hangfire;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace HRMS.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Database maintenance background jobs for automated optimization
    /// Deployed: 2025-11-14
    /// </summary>
    public class DatabaseMaintenanceJobs
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseMaintenanceJobs> _logger;

        public DatabaseMaintenanceJobs(string connectionString, ILogger<DatabaseMaintenanceJobs> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// Daily materialized view refresh
        /// Schedule: 3:00 AM daily
        /// </summary>
        [AutomaticRetry(Attempts = 3)]
        public async Task RefreshMaterializedViewsAsync()
        {
            _logger.LogInformation("Starting daily materialized view refresh...");

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(
                    "SELECT * FROM master.refresh_all_materialized_views_corrected()",
                    connection);
                command.CommandTimeout = 600; // 10 minutes

                await using var reader = await command.ExecuteReaderAsync();
                int refreshedCount = 0;

                while (await reader.ReadAsync())
                {
                    var schemaName = reader.GetString(0);
                    var viewName = reader.GetString(1);
                    var refreshTime = reader.GetTimeSpan(2);
                    var status = reader.GetString(3);

                    if (status == "SUCCESS")
                    {
                        _logger.LogInformation(
                            "Refreshed {Schema}.{View} in {Time}ms",
                            schemaName, viewName, refreshTime.TotalMilliseconds);
                        refreshedCount++;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to refresh {Schema}.{View}: {Status}",
                            schemaName, viewName, status);
                    }
                }

                _logger.LogInformation(
                    "Daily materialized view refresh completed. {Count} views refreshed.",
                    refreshedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during materialized view refresh");
                throw;
            }
        }

        /// <summary>
        /// Daily cleanup of expired refresh tokens
        /// Schedule: 4:00 AM daily
        /// </summary>
        [AutomaticRetry(Attempts = 3)]
        public async Task CleanupExpiredTokensAsync()
        {
            _logger.LogInformation("Starting expired refresh token cleanup...");

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(
                    "CALL master.cleanup_expired_refresh_tokens()",
                    connection);
                command.CommandTimeout = 300; // 5 minutes

                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Expired refresh token cleanup completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup");
                throw;
            }
        }

        /// <summary>
        /// Weekly vacuum maintenance for bloated tables
        /// Schedule: Sunday 4:00 AM
        /// </summary>
        [AutomaticRetry(Attempts = 2)]
        public async Task WeeklyVacuumMaintenanceAsync()
        {
            _logger.LogInformation("Starting weekly vacuum maintenance...");

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(
                    "CALL master.weekly_vacuum_maintenance()",
                    connection);
                command.CommandTimeout = 1800; // 30 minutes

                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Weekly vacuum maintenance completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during vacuum maintenance");
                throw;
            }
        }

        /// <summary>
        /// Monthly partition creation (future partitions)
        /// Schedule: 1st of month, 2:00 AM
        /// </summary>
        [AutomaticRetry(Attempts = 3)]
        public async Task MonthlyPartitionMaintenanceAsync()
        {
            _logger.LogInformation("Starting monthly partition maintenance...");

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(
                    "CALL master.monthly_partition_maintenance()",
                    connection);
                command.CommandTimeout = 600; // 10 minutes

                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Monthly partition maintenance completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during partition maintenance");
                throw;
            }
        }

        /// <summary>
        /// Daily database health check with alerting
        /// Schedule: 6:00 AM daily
        /// </summary>
        [AutomaticRetry(Attempts = 3)]
        public async Task DailyHealthCheckAsync()
        {
            _logger.LogInformation("Starting daily database health check...");

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(
                    "SELECT * FROM master.database_health_check()",
                    connection);
                command.CommandTimeout = 60; // 1 minute

                await using var reader = await command.ExecuteReaderAsync();
                bool hasIssues = false;

                while (await reader.ReadAsync())
                {
                    var category = reader.GetString(0);
                    var checkName = reader.GetString(1);
                    var status = reader.GetString(2);
                    var value = reader.GetString(3);
                    var recommendation = reader.GetString(4);

                    if (status == "CRITICAL" || status == "WARNING")
                    {
                        _logger.LogWarning(
                            "Database Health Issue - {Category}/{Check}: {Status} - {Value}. Recommendation: {Recommendation}",
                            category, checkName, status, value, recommendation);
                        hasIssues = true;
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Database Health OK - {Category}/{Check}: {Status} - {Value}",
                            category, checkName, status, value);
                    }
                }

                if (!hasIssues)
                {
                    _logger.LogInformation("Daily database health check completed. All systems healthy.");
                }
                else
                {
                    _logger.LogWarning("Daily database health check completed with issues. Review logs above.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
                throw;
            }
        }

        /// <summary>
        /// Register all database maintenance jobs with Hangfire
        /// Call this during application startup
        /// </summary>
        /// <param name="recurringJobManager">IRecurringJobManager instance for job scheduling</param>
        public static void RegisterScheduledJobs(IRecurringJobManager recurringJobManager)
        {
            // Daily materialized view refresh - 3:00 AM
            recurringJobManager.AddOrUpdate<DatabaseMaintenanceJobs>(
                "daily-mv-refresh",
                jobs => jobs.RefreshMaterializedViewsAsync(),
                Cron.Daily(3), // 3 AM
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            // Daily token cleanup - 4:00 AM
            recurringJobManager.AddOrUpdate<DatabaseMaintenanceJobs>(
                "daily-token-cleanup",
                jobs => jobs.CleanupExpiredTokensAsync(),
                Cron.Daily(4), // 4 AM
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            // Weekly vacuum - Sunday 4:00 AM
            recurringJobManager.AddOrUpdate<DatabaseMaintenanceJobs>(
                "weekly-vacuum-maintenance",
                jobs => jobs.WeeklyVacuumMaintenanceAsync(),
                Cron.Weekly(DayOfWeek.Sunday, 4), // Sunday 4 AM
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            // Monthly partition maintenance - 1st of month, 2:00 AM
            recurringJobManager.AddOrUpdate<DatabaseMaintenanceJobs>(
                "monthly-partition-maintenance",
                jobs => jobs.MonthlyPartitionMaintenanceAsync(),
                Cron.Monthly(1, 2), // 1st day, 2 AM
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            // Daily health check - 6:00 AM
            recurringJobManager.AddOrUpdate<DatabaseMaintenanceJobs>(
                "daily-health-check",
                jobs => jobs.DailyHealthCheckAsync(),
                Cron.Daily(6), // 6 AM
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });
        }

        /// <summary>
        /// Trigger manual maintenance (for testing or on-demand execution)
        /// </summary>
        public static void TriggerManualMaintenance()
        {
            BackgroundJob.Enqueue<DatabaseMaintenanceJobs>(jobs => jobs.RefreshMaterializedViewsAsync());
            BackgroundJob.Enqueue<DatabaseMaintenanceJobs>(jobs => jobs.CleanupExpiredTokensAsync());
        }
    }
}
