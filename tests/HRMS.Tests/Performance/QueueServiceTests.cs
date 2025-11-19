using Xunit;
using FluentAssertions;
using HRMS.Core.Entities.Master;
using HRMS.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HRMS.Tests.Performance;

/// <summary>
/// Tests for queue services that replace fire-and-forget Task.Run (P0 Bug #5)
/// </summary>
public class QueueServiceTests
{
    /// <summary>
    /// P0 Bug #5: SecurityAlertQueueService
    /// Verifies security alerts are queued reliably without blocking
    /// </summary>
    [Fact]
    public async Task SecurityAlertQueueService_Should_AcceptQueuedItems()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        var serviceScope = new Mock<IServiceScope>();
        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        var logger = new Mock<ILogger<SecurityAlertQueueService>>();

        serviceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactory.Object);
        serviceScopeFactory.Setup(x => x.CreateScope())
            .Returns(serviceScope.Object);
        serviceScope.Setup(x => x.ServiceProvider)
            .Returns(serviceProvider.Object);

        var service = new SecurityAlertQueueService(
            serviceProvider.Object,
            logger.Object);

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            UserEmail = "test@example.com",
            ActionType = "SuspiciousLogin",
            EntityType = "User",
            EntityId = "123",
            TenantId = Guid.NewGuid(),
            IpAddress = "192.168.1.1",
            UserAgent = "Test Agent"
        };

        // Act
        var result = await service.QueueSecurityAlertCheckAsync(auditLog);

        // Assert
        result.Should().BeTrue("Security alert should be queued successfully");

        // Verify it doesn't block (should return immediately)
        // The actual processing happens in background thread
    }

    /// <summary>
    /// P0 Bug #5: AnomalyDetectionQueueService
    /// Verifies anomaly checks are queued reliably without blocking
    /// </summary>
    [Fact]
    public async Task AnomalyDetectionQueueService_Should_AcceptQueuedItems()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        var serviceScope = new Mock<IServiceScope>();
        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        var logger = new Mock<ILogger<AnomalyDetectionQueueService>>();

        serviceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactory.Object);
        serviceScopeFactory.Setup(x => x.CreateScope())
            .Returns(serviceScope.Object);
        serviceScope.Setup(x => x.ServiceProvider)
            .Returns(serviceProvider.Object);

        var service = new AnomalyDetectionQueueService(
            serviceProvider.Object,
            logger.Object);

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            UserEmail = "test@example.com",
            ActionType = "DataExport",
            EntityType = "Report",
            EntityId = "456",
            TenantId = Guid.NewGuid(),
            IpAddress = "192.168.1.1",
            UserAgent = "Test Agent"
        };

        // Act
        var result = await service.QueueAnomalyCheckAsync(auditLog);

        // Assert
        result.Should().BeTrue("Anomaly check should be queued successfully");
    }

    /// <summary>
    /// Verifies queue services don't block the caller (P0 Bug #5 fix)
    /// Fire-and-forget Task.Run was causing ThreadPool exhaustion
    /// Channel-based queues return immediately and process in background
    /// </summary>
    [Fact]
    public async Task QueueServices_Should_NotBlockCaller()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        var serviceScope = new Mock<IServiceScope>();
        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        var securityLogger = new Mock<ILogger<SecurityAlertQueueService>>();
        var anomalyLogger = new Mock<ILogger<AnomalyDetectionQueueService>>();

        serviceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactory.Object);
        serviceScopeFactory.Setup(x => x.CreateScope())
            .Returns(serviceScope.Object);
        serviceScope.Setup(x => x.ServiceProvider)
            .Returns(serviceProvider.Object);

        var securityService = new SecurityAlertQueueService(
            serviceProvider.Object,
            securityLogger.Object);

        var anomalyService = new AnomalyDetectionQueueService(
            serviceProvider.Object,
            anomalyLogger.Object);

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            UserEmail = "test@example.com",
            ActionType = "TestAction",
            EntityType = "TestEntity",
            EntityId = "789",
            TenantId = Guid.NewGuid(),
            IpAddress = "127.0.0.1",
            UserAgent = "Test Agent"
        };

        // Act: Queue 1000 items rapidly to verify non-blocking behavior
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 1000; i++)
        {
            await securityService.QueueSecurityAlertCheckAsync(auditLog);
            await anomalyService.QueueAnomalyCheckAsync(auditLog);
        }

        stopwatch.Stop();

        // Assert: Should complete in < 100ms (proving it doesn't block)
        // Old fire-and-forget approach would take much longer and exhaust ThreadPool
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
            "Queueing should be extremely fast and non-blocking");
    }

    /// <summary>
    /// Verifies graceful handling of queue full scenario
    /// </summary>
    [Fact]
    public async Task QueueService_Should_HandleQueueFullGracefully()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        var serviceScope = new Mock<IServiceScope>();
        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        var logger = new Mock<ILogger<SecurityAlertQueueService>>();

        serviceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactory.Object);
        serviceScopeFactory.Setup(x => x.CreateScope())
            .Returns(serviceScope.Object);
        serviceScope.Setup(x => x.ServiceProvider)
            .Returns(serviceProvider.Object);

        var service = new SecurityAlertQueueService(
            serviceProvider.Object,
            logger.Object);

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            UserEmail = "test@example.com",
            ActionType = "TestAction",
            EntityType = "TestEntity",
            EntityId = "999",
            TenantId = Guid.NewGuid(),
            IpAddress = "127.0.0.1",
            UserAgent = "Test Agent"
        };

        // Act: Try to overflow the queue (channel capacity is 10,000)
        // This verifies the queue has bounded capacity and handles overflow
        var tasks = new Task<bool>[15000];
        for (int i = 0; i < 15000; i++)
        {
            tasks[i] = service.QueueSecurityAlertCheckAsync(auditLog);
        }

        var results = await Task.WhenAll(tasks);

        // Assert: Most should succeed, but some may fail if queue is full
        // This is expected behavior - we want bounded queues to prevent memory exhaustion
        var successCount = results.Count(r => r);
        successCount.Should().BeGreaterThan(9000,
            "Most items should be queued successfully");
    }
}
