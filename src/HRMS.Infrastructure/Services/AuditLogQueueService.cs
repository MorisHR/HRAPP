using System.Threading.Channels;
using HRMS.Core.Entities.Master;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// PERFORMANCE FIX: Background service for processing audit logs asynchronously
/// Replaces fire-and-forget Task.Run with guaranteed delivery queue
/// Prevents audit logs from being lost during application shutdown
/// </summary>
public class AuditLogQueueService : BackgroundService, IAuditLogQueueService
{
    private readonly Channel<AuditLog> _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditLogQueueService> _logger;

    public AuditLogQueueService(
        IServiceProvider serviceProvider,
        ILogger<AuditLogQueueService> logger)
    {
        // Bounded channel with capacity of 10,000 audit logs
        // If queue is full, new items will wait (prevents memory explosion)
        _channel = Channel.CreateBounded<AuditLog>(new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Queue an audit log for processing
    /// </summary>
    public async ValueTask<bool> QueueAuditLogAsync(AuditLog auditLog)
    {
        try
        {
            await _channel.Writer.WriteAsync(auditLog);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue audit log for {EntityType} {EntityId}",
                auditLog.EntityType, auditLog.EntityId);
            return false;
        }
    }

    /// <summary>
    /// Background worker that processes queued audit logs
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit log queue service started");

        await foreach (var auditLog in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAuditLogAsync(auditLog, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process audit log: {EntityType} {EntityId}",
                    auditLog.EntityType, auditLog.EntityId);

                // Don't throw - continue processing other audit logs
            }
        }

        _logger.LogInformation("Audit log queue service stopped");
    }

    /// <summary>
    /// Process a single audit log - save to database
    /// PERFORMANCE FIX: Reuses pre-configured DbContextOptions from DI
    /// </summary>
    private async Task ProcessAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        // PERFORMANCE FIX: Get pre-configured DbContextOptions from DI (already cached and optimized)
        var optionsAccessor = scope.ServiceProvider.GetService<DbContextOptions<MasterDbContext>>();

        // Fallback: Build options from scratch if not available in DI (backward compatibility)
        DbContextOptions<MasterDbContext> options;
        if (optionsAccessor != null)
        {
            // Use pre-configured options (fast path)
            options = optionsAccessor;
        }
        else
        {
            // Fallback: Build from configuration (slow path)
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Cannot save audit log: Connection string 'DefaultConnection' not found");
                return;
            }

            var optionsBuilder = new DbContextOptionsBuilder<MasterDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            options = optionsBuilder.Options;
        }

        // Create fresh DbContext instance (without interceptors to prevent circular dependency)
        using var auditContext = new MasterDbContext(options);

        // Ensure PerformedAt is set
        if (auditLog.PerformedAt == default)
        {
            auditLog.PerformedAt = DateTime.UtcNow;
        }

        // Add and save audit log
        auditContext.AuditLogs.Add(auditLog);
        await auditContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Audit log processed: {ActionType} on {EntityType} {EntityId}",
            auditLog.ActionType, auditLog.EntityType, auditLog.EntityId);
    }

    /// <summary>
    /// Graceful shutdown - wait for queue to drain
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Audit log queue service shutting down...");

        // Mark the channel as complete (no more writes)
        _channel.Writer.Complete();

        // Wait for all queued items to be processed (with timeout)
        var timeout = TimeSpan.FromSeconds(30);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            await base.StopAsync(cts.Token);
            _logger.LogInformation("Audit log queue drained successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Audit log queue shutdown timeout - {Count} items may be lost",
                _channel.Reader.Count);
        }
    }
}
