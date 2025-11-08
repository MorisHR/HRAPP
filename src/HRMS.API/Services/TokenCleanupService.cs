using Microsoft.EntityFrameworkCore;
using HRMS.Infrastructure.Data;

namespace HRMS.API.Services;

/// <summary>
/// Background service that periodically cleans up expired and revoked refresh tokens
/// Runs hourly to prevent database bloat
/// Production-grade implementation with proper error handling and logging
/// </summary>
public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Run hourly
    private readonly int _retentionDays = 30; // Keep tokens for 30 days for audit purposes

    public TokenCleanupService(
        IServiceProvider serviceProvider,
        ILogger<TokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token Cleanup Service started. Will run every {Interval} hours.", _cleanupInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync(stoppingToken);

                // Wait for next cleanup cycle
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Token Cleanup Service is stopping...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Token Cleanup Service. Will retry in next cycle.");

                // Wait before retrying on error (shorter interval)
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Token Cleanup Service stopped.");
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

            // Calculate cutoff date (tokens older than this will be deleted)
            var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

            _logger.LogDebug("Starting token cleanup. Cutoff date: {CutoffDate}", cutoffDate);

            // Find tokens to delete:
            // 1. Expired tokens older than retention period
            // 2. Revoked tokens older than retention period
            var tokensToDelete = await context.RefreshTokens
                .Where(rt =>
                    (rt.ExpiresAt < cutoffDate) || // Expired and old
                    (rt.RevokedAt.HasValue && rt.RevokedAt < cutoffDate)) // Revoked and old
                .ToListAsync(cancellationToken);

            if (!tokensToDelete.Any())
            {
                _logger.LogDebug("No expired tokens to clean up.");
                return;
            }

            // Delete tokens in batch
            context.RefreshTokens.RemoveRange(tokensToDelete);
            var deletedCount = await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Token cleanup completed. Deleted {Count} tokens (cutoff: {CutoffDate:yyyy-MM-dd})",
                tokensToDelete.Count,
                cutoffDate);

            // Log statistics for monitoring
            var activeTokensCount = await context.RefreshTokens
                .CountAsync(rt => rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow, cancellationToken);

            var revokedTokensCount = await context.RefreshTokens
                .CountAsync(rt => rt.RevokedAt != null, cancellationToken);

            _logger.LogInformation(
                "Token statistics - Active: {Active}, Revoked: {Revoked}",
                activeTokensCount,
                revokedTokensCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token cleanup");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token Cleanup Service is stopping gracefully...");
        await base.StopAsync(cancellationToken);
    }
}
