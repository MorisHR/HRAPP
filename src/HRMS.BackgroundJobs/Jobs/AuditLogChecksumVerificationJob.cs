using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using HRMS.Infrastructure.Data;
using HRMS.Core.Enums;
using System.Security.Cryptography;
using System.Text;

namespace HRMS.BackgroundJobs.Jobs;

/// <summary>
/// Background job to verify integrity of audit logs using checksums
/// Detects tampering attempts by validating SHA256 checksums
/// Runs weekly to ensure compliance with Fortune 500 audit requirements
/// </summary>
public class AuditLogChecksumVerificationJob
{
    private readonly ILogger<AuditLogChecksumVerificationJob> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AuditLogChecksumVerificationJob(
        ILogger<AuditLogChecksumVerificationJob> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("=== Audit Log Checksum Verification Job Started ===");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

            // Verify logs from the last 30 days (hot data)
            var verificationDate = DateTime.UtcNow.AddDays(-30);

            _logger.LogInformation(
                "Verifying audit log checksums for logs since {VerificationDate}",
                verificationDate);

            // Get logs with checksums
            var logsToVerify = await context.AuditLogs
                .Where(l => l.PerformedAt >= verificationDate && !string.IsNullOrEmpty(l.Checksum))
                .OrderBy(l => l.PerformedAt)
                .ToListAsync();

            if (!logsToVerify.Any())
            {
                _logger.LogInformation("No audit logs to verify");
                return;
            }

            _logger.LogInformation("Verifying {Count} audit log checksums", logsToVerify.Count);

            var tamperedLogs = new List<Guid>();
            var verifiedCount = 0;

            foreach (var log in logsToVerify)
            {
                var expectedChecksum = GenerateChecksum(log);

                if (log.Checksum != expectedChecksum)
                {
                    tamperedLogs.Add(log.Id);
                    _logger.LogCritical(
                        "SECURITY ALERT: Audit log checksum mismatch detected! " +
                        "LogId: {LogId}, Expected: {Expected}, Actual: {Actual}",
                        log.Id, expectedChecksum, log.Checksum);
                }
                else
                {
                    verifiedCount++;
                }
            }

            if (tamperedLogs.Any())
            {
                _logger.LogCritical(
                    "=== CRITICAL: {Count} audit logs failed checksum verification ===",
                    tamperedLogs.Count);

                // Create security alert for tampered logs
                context.AuditLogs.Add(new Core.Entities.Master.AuditLog
                {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.SUSPICIOUS_ACTIVITY_DETECTED,
                    Category = AuditCategory.SECURITY_EVENT,
                    Severity = AuditSeverity.EMERGENCY,
                    EntityType = "AuditLog",
                    Success = false,
                    PerformedAt = DateTime.UtcNow,
                    UserEmail = "system@hrms.com",
                    ErrorMessage = $"Audit log tampering detected: {tamperedLogs.Count} logs failed checksum verification",
                    AdditionalMetadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        TamperedLogIds = tamperedLogs,
                        VerificationDate = DateTime.UtcNow,
                        TotalVerified = logsToVerify.Count,
                        TamperedCount = tamperedLogs.Count
                    })
                });

                await context.SaveChangesAsync();
            }
            else
            {
                _logger.LogInformation(
                    "=== Audit Log Checksum Verification Completed: {VerifiedCount} logs verified, 0 tampering detected ===",
                    verifiedCount);

                // Log successful verification
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
                    Reason = $"Audit log checksum verification completed: {verifiedCount} logs verified, 0 tampering detected",
                    AdditionalMetadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        VerifiedCount = verifiedCount,
                        VerificationDate = DateTime.UtcNow
                    })
                });

                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying audit log checksums");
            throw;
        }
    }

    /// <summary>
    /// Truncate DateTime to microsecond precision to match PostgreSQL timestamp storage
    /// CRITICAL FIX: PostgreSQL stores timestamps with microsecond precision (6 decimal places),
    /// while .NET DateTime has 100-nanosecond tick precision (7 decimal places).
    /// This mismatch causes checksum validation failures after data is round-tripped through the database.
    /// </summary>
    /// <param name="dateTime">DateTime to truncate</param>
    /// <returns>DateTime truncated to microsecond precision</returns>
    private static DateTime TruncateToMicroseconds(DateTime dateTime)
    {
        // Convert to microseconds and back to remove sub-microsecond precision
        // 1 microsecond = 10 ticks (100 nanoseconds per tick)
        var microseconds = dateTime.Ticks / 10;
        return new DateTime(microseconds * 10, dateTime.Kind);
    }

    /// <summary>
    /// Generate SHA256 checksum for tamper detection
    /// CRITICAL FIX: Uses microsecond-truncated PerformedAt to match PostgreSQL timestamp precision
    /// This prevents false positive tampering alerts caused by sub-microsecond precision loss
    /// </summary>
    private string GenerateChecksum(Core.Entities.Master.AuditLog log)
    {
        try
        {
            // CRITICAL FIX: Truncate to microseconds to match PostgreSQL precision
            var performedAt = TruncateToMicroseconds(log.PerformedAt);
            var data = $"{log.Id}|{log.ActionType}|{log.UserId}|{log.EntityType}|{log.EntityId}|{performedAt:O}";
            var bytes = Encoding.UTF8.GetBytes(data);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLower();
        }
        catch
        {
            return string.Empty;
        }
    }
}
