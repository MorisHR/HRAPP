using HRMS.Application.Interfaces;
using HRMS.BackgroundJobs.Jobs;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using HRMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace HRMS.Tests;

/// <summary>
/// Production-ready integration tests for SubscriptionNotificationJob
/// Tests the complete 9-stage notification system, auto-suspension, and email deduplication
/// Covers the entire subscription lifecycle management workflow
/// </summary>
public class SubscriptionNotificationJobTests : IDisposable
{
    private readonly MasterDbContext _masterDbContext;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ILogger<SubscriptionNotificationJob>> _mockJobLogger;
    private readonly Mock<ILogger<SubscriptionManagementService>> _mockServiceLogger;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly ISubscriptionManagementService _subscriptionService;
    private readonly SubscriptionNotificationJob _job;

    public SubscriptionNotificationJobTests()
    {
        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _masterDbContext = new MasterDbContext(options);

        // Setup Mocks
        _mockEmailService = new Mock<IEmailService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockJobLogger = new Mock<ILogger<SubscriptionNotificationJob>>();
        _mockServiceLogger = new Mock<ILogger<SubscriptionManagementService>>();
        _mockCache = new Mock<IMemoryCache>();

        // Setup Cache Mock
        object? cachedValue = null;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        // Setup Email Service Mock - default success
        _mockEmailService.Setup(e => e.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Setup Audit Log Mock
        _mockAuditLogService.Setup(a => a.LogSuperAdminActionAsync(
            It.IsAny<AuditActionType>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync(new AuditLog());

        // Create Services
        _subscriptionService = new SubscriptionManagementService(
            _masterDbContext,
            _mockServiceLogger.Object,
            _mockCache.Object,
            _mockAuditLogService.Object
        );

        _job = new SubscriptionNotificationJob(
            _subscriptionService,
            _mockEmailService.Object,
            _mockAuditLogService.Object,
            _masterDbContext,
            _mockJobLogger.Object
        );
    }

    // ============================================
    // TEST 1: 30-DAY RENEWAL REMINDER
    // ============================================
    [Fact]
    public async Task Execute_Should_Send30DayReminderNotification()
    {
        // Arrange
        var tenant = CreateTenant("Company30Days", DateTime.UtcNow.AddDays(30));
        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act
        await _job.Execute();

        // Assert
        _mockEmailService.Verify(e => e.SendEmailAsync(
            tenant.ContactEmail,
            It.Is<string>(s => s.Contains("30")),
            It.IsAny<string>()),
            Times.Once);

        var notificationSent = await _subscriptionService.HasNotificationBeenSentAsync(
            tenant.Id,
            SubscriptionNotificationType.Reminder30Days);
        notificationSent.Should().BeTrue();
    }

    // ============================================
    // TEST 2: 15-DAY RENEWAL REMINDER
    // ============================================
    [Fact]
    public async Task Execute_Should_Send15DayReminderNotification()
    {
        // Arrange
        var tenant = CreateTenant("Company15Days", DateTime.UtcNow.AddDays(15));
        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act
        await _job.Execute();

        // Assert
        _mockEmailService.Verify(e => e.SendEmailAsync(
            tenant.ContactEmail,
            It.Is<string>(s => s.Contains("15")),
            It.IsAny<string>()),
            Times.Once);
    }

    // ============================================
    // TEST 3: 7-DAY URGENT REMINDER
    // ============================================
    [Fact]
    public async Task Execute_Should_Send7DayUrgentReminder()
    {
        // Arrange
        var tenant = CreateTenant("Company7Days", DateTime.UtcNow.AddDays(7));
        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act
        await _job.Execute();

        // Assert
        _mockEmailService.Verify(e => e.SendEmailAsync(
            tenant.ContactEmail,
            It.Is<string>(s => s.Contains("URGENT") || s.Contains("7")),
            It.IsAny<string>()),
            Times.Once);

        var notificationSent = await _subscriptionService.HasNotificationBeenSentAsync(
            tenant.Id,
            SubscriptionNotificationType.Reminder7Days);
        notificationSent.Should().BeTrue();
    }

    // ============================================
    // TEST 4: 3-DAY CRITICAL REMINDER
    // ============================================
    [Fact]
    public async Task Execute_Should_Send3DayCriticalReminder()
    {
        // Arrange
        var tenant = CreateTenant("Company3Days", DateTime.UtcNow.AddDays(3));
        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act
        await _job.Execute();

        // Assert
        _mockEmailService.Verify(e => e.SendEmailAsync(
            tenant.ContactEmail,
            It.Is<string>(s => s.Contains("CRITICAL") || s.Contains("3")),
            It.IsAny<string>()),
            Times.Once);

        var notificationSent = await _subscriptionService.HasNotificationBeenSentAsync(
            tenant.Id,
            SubscriptionNotificationType.Reminder3Days);
        notificationSent.Should().BeTrue();
    }

    // ============================================
    // TEST 5: 1-DAY FINAL WARNING
    // ============================================
    [Fact]
    public async Task Execute_Should_Send1DayFinalWarning()
    {
        // Arrange
        var tenant = CreateTenant("Company1Day", DateTime.UtcNow.AddDays(1));
        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act
        await _job.Execute();

        // Assert
        _mockEmailService.Verify(e => e.SendEmailAsync(
            tenant.ContactEmail,
            It.Is<string>(s => s.Contains("FINAL") || s.Contains("Tomorrow") || s.Contains("1")),
            It.IsAny<string>()),
            Times.Once);

        var notificationSent = await _subscriptionService.HasNotificationBeenSentAsync(
            tenant.Id,
            SubscriptionNotificationType.Reminder1Day);
        notificationSent.Should().BeTrue();
    }

    // ============================================
    // TEST 6: EXPIRY NOTIFICATION AND GRACE PERIOD START
    // ============================================
    [Fact]
    public async Task Execute_Should_SendExpiryNotificationAndStartGracePeriod()
    {
        // Arrange
        var tenant = CreateTenant("CompanyExpiring", DateTime.UtcNow.Date);
        tenant.GracePeriodStartDate = null; // Not started yet
        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act
        await _job.Execute();

        // Assert
        _mockEmailService.Verify(e => e.SendEmailAsync(
            tenant.ContactEmail,
            It.Is<string>(s => s.Contains("Expired") || s.Contains("Grace Period")),
            It.IsAny<string>()),
            Times.Once);

        var updatedTenant = await _masterDbContext.Tenants.FindAsync(tenant.Id);
        updatedTenant!.GracePeriodStartDate.Should().NotBeNull();
        updatedTenant.Status.Should().Be(TenantStatus.ExpiringSoon);

        var notificationSent = await _subscriptionService.HasNotificationBeenSentAsync(
            tenant.Id,
            SubscriptionNotificationType.ExpiryNotification);
        notificationSent.Should().BeTrue();
    }

    // ============================================
    // TEST 7: GRACE PERIOD WARNING (DAYS 1-7)
    // ============================================
    [Fact]
    public async Task Execute_Should_SendGracePeriodWarning_Days1To7()
    {
        // Arrange
        var tenant = CreateTenant("CompanyInGracePeriod", DateTime.UtcNow.AddDays(-5));
        tenant.GracePeriodStartDate = DateTime.UtcNow.AddDays(-5); // 5 days into grace period
        tenant.Status = TenantStatus.ExpiringSoon;
        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act
        await _job.Execute();

        // Assert
        _mockEmailService.Verify(e => e.SendEmailAsync(
            tenant.ContactEmail,
            It.Is<string>(s => s.Contains("Grace Period") || s.Contains("days remaining")),
            It.IsAny<string>()),
            Times.Once);

        var notificationSent = await _subscriptionService.HasNotificationBeenSentAsync(
            tenant.Id,
            SubscriptionNotificationType.GracePeriodWarning);
        notificationSent.Should().BeTrue();
    }

    // ============================================
    // TEST 8: CRITICAL WARNING (DAYS 8-14)
    // ============================================
    [Fact]
    public async Task Execute_Should_SendCriticalWarning_Days8To14()
    {
        // Arrange
        var tenant = CreateTenant("CompanyCriticalPeriod", DateTime.UtcNow.AddDays(-10));
        tenant.GracePeriodStartDate = DateTime.UtcNow.AddDays(-10); // 10 days into grace period
        tenant.Status = TenantStatus.ExpiringSoon;
        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act
        await _job.Execute();

        // Assert
        _mockEmailService.Verify(e => e.SendEmailAsync(
            tenant.ContactEmail,
            It.Is<string>(s => s.Contains("CRITICAL") || s.Contains("Suspended")),
            It.IsAny<string>()),
            Times.Once);

        var notificationSent = await _subscriptionService.HasNotificationBeenSentAsync(
            tenant.Id,
            SubscriptionNotificationType.CriticalWarning);
        notificationSent.Should().BeTrue();
    }

    // ============================================
    // TEST 9: AUTO-SUSPENSION AFTER GRACE PERIOD
    // ============================================
    [Fact]
    public async Task Execute_Should_AutoSuspendTenant_AfterGracePeriodEnds()
    {
        // Arrange
        var tenant = CreateTenant("CompanyToSuspend", DateTime.UtcNow.AddDays(-20));
        tenant.GracePeriodStartDate = DateTime.UtcNow.AddDays(-15); // 15 days ago (grace period ended)
        tenant.Status = TenantStatus.ExpiringSoon;
        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act
        await _job.Execute();

        // Assert
        _mockEmailService.Verify(e => e.SendEmailAsync(
            tenant.ContactEmail,
            It.Is<string>(s => s.Contains("SUSPENDED") || s.Contains("Payment Required")),
            It.IsAny<string>()),
            Times.Once);

        var updatedTenant = await _masterDbContext.Tenants.FindAsync(tenant.Id);
        updatedTenant!.Status.Should().Be(TenantStatus.Suspended);
        updatedTenant.SuspensionDate.Should().NotBeNull();
        updatedTenant.SuspensionReason.Should().Contain("grace period");

        // Verify audit log was called
        _mockAuditLogService.Verify(a => a.LogSuperAdminActionAsync(
            AuditActionType.TENANT_SUSPENDED,
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            tenant.Id,
            tenant.CompanyName,
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            true,
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object>?>()),
            Times.Once);
    }

    // ============================================
    // TEST 10: EMAIL DEDUPLICATION - SAME NOTIFICATION TYPE
    // ============================================
    [Fact]
    public async Task Execute_Should_NotSendDuplicateNotifications()
    {
        // Arrange
        var tenant = CreateTenant("CompanyDedup", DateTime.UtcNow.AddDays(30));
        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act - Run job twice
        await _job.Execute();
        await _job.Execute();

        // Assert - Email should only be sent once
        _mockEmailService.Verify(e => e.SendEmailAsync(
            tenant.ContactEmail,
            It.Is<string>(s => s.Contains("30")),
            It.IsAny<string>()),
            Times.Once); // Only once, not twice
    }

    // ============================================
    // TEST 11: RENEWAL PAYMENT AUTO-CREATION (30-60 DAYS)
    // ============================================
    [Fact]
    public async Task Execute_Should_AutoCreateRenewalPayment_30To60DaysBeforeExpiry()
    {
        // Arrange
        var tenant = CreateTenant("CompanyAutoRenewal", DateTime.UtcNow.AddDays(45));
        tenant.YearlyPriceMUR = 100000.00m;
        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act
        await _job.Execute();

        // Assert
        var renewalPayment = await _masterDbContext.SubscriptionPayments
            .FirstOrDefaultAsync(p => p.TenantId == tenant.Id
                && p.PeriodStartDate >= tenant.SubscriptionEndDate);

        renewalPayment.Should().NotBeNull();
        renewalPayment!.Status.Should().Be(SubscriptionPaymentStatus.Pending);
        renewalPayment.TotalMUR.Should().BeGreaterThan(0);
    }

    // ============================================
    // TEST 12: SKIP SUSPENSION IF PAYMENT RECEIVED
    // ============================================
    [Fact]
    public async Task Execute_Should_NotSuspend_IfPaymentReceivedDuringGracePeriod()
    {
        // Arrange
        var tenant = CreateTenant("CompanyPaid", DateTime.UtcNow.AddDays(-20));
        tenant.GracePeriodStartDate = DateTime.UtcNow.AddDays(-15);
        tenant.Status = TenantStatus.ExpiringSoon;
        await _masterDbContext.Tenants.AddAsync(tenant);

        // Create and mark payment as paid
        var payment = new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            PeriodStartDate = DateTime.UtcNow,
            PeriodEndDate = DateTime.UtcNow.AddYears(1),
            TotalMUR = 50000.00m,
            SubtotalMUR = 43478.26m,
            TaxAmountMUR = 6521.74m,
            TaxRate = 0.15m,
            Status = SubscriptionPaymentStatus.Paid,
            PaidDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow,
            EmployeeTier = EmployeeTier.Tier3,
            CreatedAt = DateTime.UtcNow
        };
        await _masterDbContext.SubscriptionPayments.AddAsync(payment);
        await _masterDbContext.SaveChangesAsync();

        // Act
        await _job.Execute();

        // Assert
        var updatedTenant = await _masterDbContext.Tenants.FindAsync(tenant.Id);
        updatedTenant!.Status.Should().NotBe(TenantStatus.Suspended);

        _mockEmailService.Verify(e => e.SendEmailAsync(
            tenant.ContactEmail,
            It.Is<string>(s => s.Contains("SUSPENDED")),
            It.IsAny<string>()),
            Times.Never);
    }

    // ============================================
    // TEST 13: TRIAL CONVERSION REMINDER
    // ============================================
    [Fact]
    public async Task Execute_Should_SendTrialConversionReminder_ForExpiredTrials()
    {
        // Arrange
        var tenant = CreateTenant("TrialCompany", null);
        tenant.Status = TenantStatus.Trial;
        tenant.TrialEndDate = DateTime.UtcNow.AddDays(-2); // Trial expired 2 days ago
        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act
        await _job.Execute();

        // Assert
        _mockEmailService.Verify(e => e.SendEmailAsync(
            tenant.ContactEmail,
            It.Is<string>(s => s.Contains("Trial") || s.Contains("Convert")),
            It.IsAny<string>()),
            Times.Once);
    }

    // ============================================
    // TEST 14: MARK OVERDUE PAYMENTS
    // ============================================
    [Fact]
    public async Task Execute_Should_MarkPaymentsAsOverdue_WhenDueDatePassed()
    {
        // Arrange
        var tenant = CreateTenant("CompanyOverdue", DateTime.UtcNow.AddMonths(6));
        await _masterDbContext.Tenants.AddAsync(tenant);

        var payment = new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            PeriodStartDate = DateTime.UtcNow.AddDays(-10),
            PeriodEndDate = DateTime.UtcNow.AddYears(1),
            TotalMUR = 50000.00m,
            SubtotalMUR = 43478.26m,
            TaxAmountMUR = 6521.74m,
            TaxRate = 0.15m,
            Status = SubscriptionPaymentStatus.Pending,
            DueDate = DateTime.UtcNow.AddDays(-5), // Overdue
            EmployeeTier = EmployeeTier.Tier3,
            CreatedAt = DateTime.UtcNow
        };
        await _masterDbContext.SubscriptionPayments.AddAsync(payment);
        await _masterDbContext.SaveChangesAsync();

        // Act
        await _job.Execute();

        // Assert
        var updatedPayment = await _masterDbContext.SubscriptionPayments.FindAsync(payment.Id);
        updatedPayment!.Status.Should().Be(SubscriptionPaymentStatus.Overdue);
    }

    // ============================================
    // TEST 15: COMPLETE JOB EXECUTION WITHOUT ERRORS
    // ============================================
    [Fact]
    public async Task Execute_Should_CompleteSuccessfully_WithMultipleTenants()
    {
        // Arrange - Create multiple tenants in different states
        var tenants = new[]
        {
            CreateTenant("Company30Days", DateTime.UtcNow.AddDays(30)),
            CreateTenant("Company15Days", DateTime.UtcNow.AddDays(15)),
            CreateTenant("Company7Days", DateTime.UtcNow.AddDays(7)),
            CreateTenant("Company3Days", DateTime.UtcNow.AddDays(3)),
            CreateTenant("Company1Day", DateTime.UtcNow.AddDays(1))
        };

        foreach (var tenant in tenants)
        {
            await _masterDbContext.Tenants.AddAsync(tenant);
        }
        await _masterDbContext.SaveChangesAsync();

        // Act
        var exception = await Record.ExceptionAsync(async () => await _job.Execute());

        // Assert
        exception.Should().BeNull("job should execute without throwing exceptions");

        // Verify multiple emails were sent
        _mockEmailService.Verify(e => e.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.AtLeast(5)); // At least 5 notifications sent
    }

    // ============================================
    // HELPER METHODS
    // ============================================
    private Tenant CreateTenant(string companyName, DateTime? subscriptionEndDate)
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            CompanyName = companyName,
            Subdomain = companyName.ToLower().Replace(" ", ""),
            ContactEmail = $"{companyName.ToLower().Replace(" ", "")}@example.com",
            SubscriptionEndDate = subscriptionEndDate,
            YearlyPriceMUR = 50000.00m,
            Status = TenantStatus.Active,
            EmployeeTier = EmployeeTier.Tier3,
            IsGovernmentEntity = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _masterDbContext?.Dispose();
    }
}
