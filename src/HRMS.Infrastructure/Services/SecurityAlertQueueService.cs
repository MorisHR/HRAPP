using System.Threading.Channels;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Master;
using HRMS.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// PERFORMANCE FIX: Background service for processing security alerts asynchronously
/// Replaces fire-and-forget Task.Run with guaranteed delivery queue
/// Prevents security alerts from being lost during application shutdown
/// Pattern copied from AuditLogQueueService (P0 Bug #5 fix)
/// </summary>
public class SecurityAlertQueueService : BackgroundService, ISecurityAlertQueueService
{
    private readonly Channel<AuditLog> _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SecurityAlertQueueService> _logger;

    public SecurityAlertQueueService(
        IServiceProvider serviceProvider,
        ILogger<SecurityAlertQueueService> logger)
    {
        // Bounded channel with capacity of 5,000 security alert checks
        // Lower capacity than audit logs since security checks are less frequent
        // If queue is full, new items will wait (prevents memory explosion)
        _channel = Channel.CreateBounded<AuditLog>(new BoundedChannelOptions(5000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Queue a security alert check for processing
    /// </summary>
    public async ValueTask<bool> QueueSecurityAlertCheckAsync(AuditLog auditLog)
    {
        try
        {
            await _channel.Writer.WriteAsync(auditLog);
            _logger.LogDebug("Security alert check queued for audit log {AuditLogId}", auditLog.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue security alert check for audit log {AuditLogId}",
                auditLog.Id);
            return false;
        }
    }

    /// <summary>
    /// Background worker that processes queued security alert checks
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Security alert queue service started");

        await foreach (var auditLog in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessSecurityAlertCheckAsync(auditLog, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process security alert check for audit log {AuditLogId}",
                    auditLog.Id);

                // Don't throw - continue processing other alerts
            }
        }

        _logger.LogInformation("Security alert queue service stopped");
    }

    /// <summary>
    /// Process a single security alert check - evaluate and create alert if needed
    /// </summary>
    private async Task ProcessSecurityAlertCheckAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var securityAlertingService = scope.ServiceProvider.GetService<ISecurityAlertingService>();

        if (securityAlertingService == null)
        {
            _logger.LogWarning("SecurityAlertingService not available - skipping alert check for audit log {AuditLogId}",
                auditLog.Id);
            return;
        }

        try
        {
            // Check if this audit log should trigger a security alert
            var (shouldAlert, alertType, riskScore) = await securityAlertingService.ShouldTriggerAlertAsync(auditLog);

            if (shouldAlert && alertType.HasValue)
            {
                _logger.LogWarning(
                    "Security alert triggered: {AlertType} for audit log {AuditLogId} (Risk Score: {RiskScore})",
                    alertType.Value, auditLog.Id, riskScore);

                // Create security alert
                await securityAlertingService.CreateAlertFromAuditLogAsync(
                    auditLog,
                    alertType.Value,
                    $"Security Alert: {alertType.Value}",
                    $"Suspicious activity detected: {auditLog.ActionType} by {auditLog.UserEmail ?? "Unknown User"}",
                    riskScore,
                    $"Review audit log {auditLog.Id} for details. Investigate user activity and take appropriate action.",
                    sendNotifications: true
                );

                _logger.LogInformation(
                    "Security alert created successfully for audit log {AuditLogId}",
                    auditLog.Id);
            }
            else
            {
                _logger.LogDebug(
                    "No security alert needed for audit log {AuditLogId}",
                    auditLog.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process security alert for audit log {AuditLogId}",
                auditLog.Id);
            // Don't rethrow - we've logged the error and don't want to stop the queue processor
        }
    }

    /// <summary>
    /// Graceful shutdown - wait for queue to drain
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Security alert queue service shutting down...");

        // Mark the channel as complete (no more writes)
        _channel.Writer.Complete();

        // Wait for all queued items to be processed (with timeout)
        var timeout = TimeSpan.FromSeconds(30);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            await base.StopAsync(cts.Token);
            _logger.LogInformation("Security alert queue drained successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Security alert queue shutdown timeout - {Count} items may be lost",
                _channel.Reader.Count);
        }
    }
}
