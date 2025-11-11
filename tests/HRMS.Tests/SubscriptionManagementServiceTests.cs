using HRMS.Application.Interfaces;
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
/// Production-ready unit tests for SubscriptionManagementService
/// Covers VAT calculations, pro-rated upgrades, revenue analytics, and payment transitions
/// </summary>
public class SubscriptionManagementServiceTests : IDisposable
{
    private readonly MasterDbContext _masterDbContext;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<ILogger<SubscriptionManagementService>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly SubscriptionManagementService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public SubscriptionManagementServiceTests()
    {
        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;

        _masterDbContext = new MasterDbContext(options);

        // Setup Mocks
        _mockCache = new Mock<IMemoryCache>();
        _mockLogger = new Mock<ILogger<SubscriptionManagementService>>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        // Setup Cache Mock (return null for cache misses)
        object? cachedValue = null;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);

        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        // Create Service
        _service = new SubscriptionManagementService(
            _masterDbContext,
            _mockLogger.Object,
            _mockCache.Object,
            _mockAuditLogService.Object
        );

        // Seed Test Data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var tenant = new Tenant
        {
            Id = _testTenantId,
            CompanyName = "Test Company",
            Subdomain = "testcompany",
            SubscriptionEndDate = DateTime.UtcNow.AddYears(1),
            YearlyPriceMUR = 50000.00m,
            Status = TenantStatus.Active,
            EmployeeTier = EmployeeTier.Tier3,
            ContactEmail = "test@example.com",
            IsGovernmentEntity = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _masterDbContext.Tenants.Add(tenant);
        _masterDbContext.SaveChanges();
    }

    [Fact]
    public async Task CreatePaymentRecord_ShouldCalculateVATCorrectly_15Percent()
    {
        // Arrange
        var subtotal = 50000.00m;
        var expectedTax = subtotal * 0.15m; // 7500.00
        var expectedTotal = subtotal + expectedTax; // 57500.00

        var periodStart = DateTime.UtcNow;
        var periodEnd = periodStart.AddYears(1);
        var dueDate = periodStart.AddDays(30);

        // Act
        var result = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            periodStart,
            periodEnd,
            subtotal,
            dueDate,
            EmployeeTier.Tier3,
            "Test payment",
            calculateTax: true
        );

        // Assert
        result.Should().NotBeNull();
        result.SubtotalMUR.Should().Be(subtotal);
        result.TaxRate.Should().Be(0.15m);
        result.TaxAmountMUR.Should().Be(expectedTax);
        result.TotalMUR.Should().Be(expectedTotal);
        result.IsTaxExempt.Should().BeFalse();
        result.Status.Should().Be(SubscriptionPaymentStatus.Pending);
    }

    [Fact]
    public async Task CreatePaymentRecord_WithoutTax_ShouldNotAddVAT()
    {
        // Arrange
        var amount = 50000.00m;
        var periodStart = DateTime.UtcNow;
        var periodEnd = periodStart.AddYears(1);
        var dueDate = periodStart.AddDays(30);

        // Act
        var result = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            periodStart,
            periodEnd,
            amount,
            dueDate,
            EmployeeTier.Tier3,
            "Test payment",
            calculateTax: false
        );

        // Assert
        result.Should().NotBeNull();
        result.TotalMUR.Should().Be(amount);
        result.TaxAmountMUR.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateTax_ShouldReturn15PercentForNonGovernment()
    {
        // Arrange
        var amount = 50000.00m;

        // Act
        var result = await _service.CalculateTaxAsync(amount, isGovernmentEntity: false);

        // Assert
        result.Subtotal.Should().Be(amount);
        result.TaxRate.Should().Be(0.15m);
        result.TaxAmount.Should().Be(7500.00m);
        result.Total.Should().Be(57500.00m);
    }

    [Fact]
    public async Task CalculateTax_GovernmentEntity_ShouldBeExempt()
    {
        // Arrange
        var amount = 50000.00m;

        // Act
        var result = await _service.CalculateTaxAsync(amount, isGovernmentEntity: true);

        // Assert
        result.Subtotal.Should().Be(amount);
        result.TaxRate.Should().Be(0m);
        result.TaxAmount.Should().Be(0m);
        result.Total.Should().Be(amount);
    }

    [Fact]
    public async Task MarkPaymentAsPaid_ShouldUpdateStatusCorrectly()
    {
        // Arrange
        var periodStart = DateTime.UtcNow;
        var payment = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            periodStart,
            periodStart.AddYears(1),
            50000.00m,
            periodStart.AddDays(30),
            EmployeeTier.Tier3
        );

        var paymentDate = DateTime.UtcNow;
        var processedBy = "admin@example.com";
        var reference = "TXN-12345";

        // Act
        var result = await _service.MarkPaymentAsPaidAsync(
            payment.Id,
            processedBy,
            reference,
            "Bank Transfer",
            paymentDate
        );

        // Assert
        result.Success.Should().BeTrue();

        var updatedPayment = await _masterDbContext.SubscriptionPayments.FindAsync(payment.Id);
        updatedPayment!.Status.Should().Be(SubscriptionPaymentStatus.Paid);
        updatedPayment.PaidDate.Should().NotBeNull();
        updatedPayment.ProcessedBy.Should().Be(processedBy);
        updatedPayment.PaymentReference.Should().Be(reference);
    }

    [Fact]
    public async Task MarkPaymentAsOverdue_ShouldUpdateStatus()
    {
        // Arrange
        var periodStart = DateTime.UtcNow.AddDays(-40);
        var payment = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            periodStart,
            periodStart.AddYears(1),
            50000.00m,
            periodStart.AddDays(30),
            EmployeeTier.Tier3
        );

        // Act
        var result = await _service.MarkPaymentAsOverdueAsync(payment.Id);

        // Assert
        result.Success.Should().BeTrue();

        var updatedPayment = await _masterDbContext.SubscriptionPayments.FindAsync(payment.Id);
        updatedPayment!.Status.Should().Be(SubscriptionPaymentStatus.Overdue);
    }

    [Fact]
    public async Task WaivePayment_ShouldUpdateStatusAndReason()
    {
        // Arrange
        var periodStart = DateTime.UtcNow;
        var payment = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            periodStart,
            periodStart.AddYears(1),
            50000.00m,
            periodStart.AddDays(30),
            EmployeeTier.Tier3
        );

        var waivedBy = "admin@example.com";
        var reason = "Promotional waiver";

        // Act
        var result = await _service.WaivePaymentAsync(payment.Id, waivedBy, reason);

        // Assert
        result.Success.Should().BeTrue();

        var updatedPayment = await _masterDbContext.SubscriptionPayments.FindAsync(payment.Id);
        updatedPayment!.Status.Should().Be(SubscriptionPaymentStatus.Waived);
        updatedPayment.ProcessedBy.Should().Be(waivedBy);
        updatedPayment.Notes.Should().Contain(reason);
    }

    [Fact]
    public async Task GetOverduePayments_ShouldReturnCorrectPayments()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-10);
        var overduePayment = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            pastDate,
            pastDate.AddYears(1),
            50000.00m,
            pastDate.AddDays(-5), // Due date in the past
            EmployeeTier.Tier3
        );

        var futureDate = DateTime.UtcNow;
        var upcomingPayment = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            futureDate,
            futureDate.AddYears(1),
            30000.00m,
            futureDate.AddDays(30),
            EmployeeTier.Tier3
        );

        // Act
        var result = await _service.GetOverduePaymentsAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(p => p.Id == overduePayment.Id);
    }

    [Fact]
    public async Task GetPendingPayments_ShouldReturnOnlyPending()
    {
        // Arrange
        var periodStart = DateTime.UtcNow;
        var pending1 = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            periodStart,
            periodStart.AddYears(1),
            50000.00m,
            periodStart.AddDays(30),
            EmployeeTier.Tier3
        );

        var pending2 = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            periodStart,
            periodStart.AddYears(1),
            30000.00m,
            periodStart.AddDays(60),
            EmployeeTier.Tier3
        );

        var paid = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            periodStart,
            periodStart.AddYears(1),
            20000.00m,
            periodStart.AddDays(90),
            EmployeeTier.Tier3
        );

        await _service.MarkPaymentAsPaidAsync(paid.Id, "admin", "REF1", "Bank", DateTime.UtcNow);

        // Act
        var result = await _service.GetPendingPaymentsAsync();

        // Assert
        result.Should().HaveCountGreaterOrEqualTo(2);
        result.Should().Contain(p => p.Id == pending1.Id);
        result.Should().Contain(p => p.Id == pending2.Id);
        result.Should().NotContain(p => p.Id == paid.Id);
    }

    [Fact]
    public async Task GetAnnualRecurringRevenue_ShouldCalculateCorrectly()
    {
        // Act
        var result = await _service.GetAnnualRecurringRevenueAsync();

        // Assert
        result.Should().Be(50000.00m); // From seeded tenant
    }

    [Fact]
    public async Task CalculateProRatedAmount_ShouldCalculateCorrectly()
    {
        // Arrange
        var tenant = await _masterDbContext.Tenants.FindAsync(_testTenantId);
        tenant!.SubscriptionEndDate = DateTime.UtcNow.AddMonths(6); // 6 months remaining
        await _masterDbContext.SaveChangesAsync();

        var newYearlyPrice = 100000.00m;

        // Act
        var result = await _service.CalculateProRatedAmountAsync(_testTenantId, newYearlyPrice, includeTax: true);

        // Assert
        result.SubtotalMUR.Should().BeGreaterThan(0m);
        result.TaxMUR.Should().BeGreaterThan(0m);
        result.TotalMUR.Should().Be(result.SubtotalMUR + result.TaxMUR);
    }

    [Fact]
    public async Task CreateProRatedPayment_ShouldCreateCorrectPayment()
    {
        // Arrange
        var tenant = await _masterDbContext.Tenants.FindAsync(_testTenantId);
        tenant!.SubscriptionEndDate = DateTime.UtcNow.AddMonths(6);
        await _masterDbContext.SaveChangesAsync();

        // Act
        var result = await _service.CreateProRatedPaymentAsync(
            _testTenantId,
            EmployeeTier.Tier4,
            100000.00m,
            "Tier upgrade from Tier3 to Tier4"
        );

        // Assert
        result.Should().NotBeNull();
        result!.EmployeeTier.Should().Be(EmployeeTier.Tier4);
        result.Status.Should().Be(SubscriptionPaymentStatus.Pending);
    }

    [Fact]
    public async Task RenewSubscription_ShouldCreatePaymentAndExtendDate()
    {
        // Act
        var result = await _service.RenewSubscriptionAsync(_testTenantId, years: 1, processedBy: "admin");

        // Assert
        result.Success.Should().BeTrue();
        result.Payment.Should().NotBeNull();

        var tenant = await _masterDbContext.Tenants.FindAsync(_testTenantId);
        tenant!.SubscriptionEndDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ConvertTrialToPaid_ShouldUpdateStatusAndCreatePayment()
    {
        // Arrange
        var trialTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            CompanyName = "Trial Company",
            Subdomain = "trialcompany",
            Status = TenantStatus.Trial,
            TrialEndDate = DateTime.UtcNow.AddDays(-1),
            EmployeeTier = EmployeeTier.Tier2,
            ContactEmail = "trial@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _masterDbContext.Tenants.Add(trialTenant);
        await _masterDbContext.SaveChangesAsync();

        // Act
        var result = await _service.ConvertTrialToPaidAsync(trialTenant.Id, 25000.00m);

        // Assert
        result.Success.Should().BeTrue();
        result.Payment.Should().NotBeNull();

        var updatedTenant = await _masterDbContext.Tenants.FindAsync(trialTenant.Id);
        updatedTenant!.Status.Should().Be(TenantStatus.Active);
        updatedTenant.SubscriptionEndDate.Should().NotBeNull();
    }

    [Fact]
    public async Task HasNotificationBeenSent_ShouldReturnFalseForNewNotification()
    {
        // Act
        var result = await _service.HasNotificationBeenSentAsync(
            _testTenantId,
            SubscriptionNotificationType.Reminder30Days
        );

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LogNotificationSent_ShouldCreateLog()
    {
        // Act
        await _service.LogNotificationSentAsync(
            _testTenantId,
            SubscriptionNotificationType.Reminder30Days,
            "test@example.com",
            "30-day reminder",
            success: true
        );

        // Assert
        var hasBeenSent = await _service.HasNotificationBeenSentAsync(
            _testTenantId,
            SubscriptionNotificationType.Reminder30Days
        );
        hasBeenSent.Should().BeTrue();
    }

    [Fact]
    public async Task GetTenantsNeedingRenewalNotification_ShouldReturnCorrectTenants()
    {
        // Arrange
        var tenant = await _masterDbContext.Tenants.FindAsync(_testTenantId);
        tenant!.SubscriptionEndDate = DateTime.UtcNow.AddDays(30);
        await _masterDbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetTenantsNeedingRenewalNotificationAsync(
            30,
            SubscriptionNotificationType.Reminder30Days
        );

        // Assert
        result.Should().Contain(t => t.Id == _testTenantId);
    }

    [Fact(Skip = "InMemory database doesn't support GroupBy - requires real SQL database")]
    public async Task GetRevenueDashboard_ShouldReturnCompleteMetrics()
    {
        // Act
        var result = await _service.GetRevenueDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalActiveSubscriptions.Should().BeGreaterOrEqualTo(1);
        result.AnnualRecurringRevenueMUR.Should().BeGreaterThan(0);
        result.MonthlyRecurringRevenueMUR.Should().BeGreaterThan(0);
    }

    public void Dispose()
    {
        _masterDbContext?.Dispose();
    }
}
