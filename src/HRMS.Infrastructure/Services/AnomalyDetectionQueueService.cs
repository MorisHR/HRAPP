using System.Threading.Channels;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Master;
using HRMS.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// PERFORMANCE FIX: Background service for processing anomaly detection asynchronously
/// Replaces fire-and-forget Task.Run in AuditLoggingMiddleware (P0 Bug #5 fix)
/// Prevents anomaly detection from blocking the audit logging pipeline
/// Pattern copied from AuditLogQueueService and SecurityAlertQueueService
/// </summary>
public class AnomalyDetectionQueueService : BackgroundService, IAnomalyDetectionQueueService
{
    private readonly Channel<AuditLog> _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnomalyDetectionQueueService> _logger;

    public AnomalyDetectionQueueService(
        IServiceProvider serviceProvider,
        ILogger<AnomalyDetectionQueueService> logger)
    {
        // Bounded channel with capacity of 10,000 anomaly detection checks
        // Same capacity as audit logs since every audit log may need anomaly detection
        // If queue is full, new items will wait (prevents memory explosion)
        _channel = Channel.CreateBounded<AuditLog>(new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Queue an anomaly detection check for processing
    /// </summary>
    public async ValueTask<bool> QueueAnomalyDetectionCheckAsync(AuditLog auditLog)
    {
        try
        {
            await _channel.Writer.WriteAsync(auditLog);
            _logger.LogDebug("Anomaly detection check queued for audit log {AuditLogId}", auditLog.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue anomaly detection check for audit log {AuditLogId}",
                auditLog.Id);
            return false;
        }
    }

    /// <summary>
    /// Background worker that processes queued anomaly detection checks
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Anomaly detection queue service started");

        await foreach (var auditLog in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAnomalyDetectionCheckAsync(auditLog, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process anomaly detection check for audit log {AuditLogId}",
                    auditLog.Id);

                // Don't throw - continue processing other checks
            }
        }

        _logger.LogInformation("Anomaly detection queue service stopped");
    }

    /// <summary>
    /// Process a single anomaly detection check - analyze and log findings
    /// </summary>
    private async Task ProcessAnomalyDetectionCheckAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var anomalyService = scope.ServiceProvider.GetService<IAnomalyDetectionService>();

        if (anomalyService == null)
        {
            _logger.LogDebug("AnomalyDetectionService not available - skipping check for audit log {AuditLogId}",
                auditLog.Id);
            return;
        }

        try
        {
            // Run anomaly detection on the audit log
            var anomalies = await anomalyService.DetectAnomaliesAsync(auditLog);

            if (anomalies != null && anomalies.Any())
            {
                _logger.LogWarning(
                    "Detected {Count} anomalies for audit log {AuditLogId}: {Anomalies}",
                    anomalies.Count,
                    auditLog.Id,
                    string.Join(", ", anomalies.Select(a => a.AnomalyType)));

                // Anomalies are already saved to database by the anomaly service
                // Just log them here for visibility
                foreach (var anomaly in anomalies)
                {
                    _logger.LogInformation(
                        "Anomaly detected: Type={AnomalyType}, RiskLevel={RiskLevel}, Score={RiskScore} for audit log {AuditLogId}",
                        anomaly.AnomalyType,
                        anomaly.RiskLevel,
                        anomaly.RiskScore,
                        auditLog.Id);
                }
            }
            else
            {
                _logger.LogDebug(
                    "No anomalies detected for audit log {AuditLogId}",
                    auditLog.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process anomaly detection for audit log {AuditLogId}",
                auditLog.Id);
            // Don't rethrow - we've logged the error and don't want to stop the queue processor
        }
    }

    /// <summary>
    /// Graceful shutdown - wait for queue to drain
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Anomaly detection queue service shutting down...");

        // Mark the channel as complete (no more writes)
        _channel.Writer.Complete();

        // Wait for all queued items to be processed (with timeout)
        var timeout = TimeSpan.FromSeconds(30);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            await base.StopAsync(cts.Token);
            _logger.LogInformation("Anomaly detection queue drained successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Anomaly detection queue shutdown timeout - {Count} items may be lost",
                _channel.Reader.Count);
        }
    }
}
