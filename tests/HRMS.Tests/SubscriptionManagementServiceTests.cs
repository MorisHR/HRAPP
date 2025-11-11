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

        // Setup Cache Mock (return null for cache misses)
        object? cachedValue = null;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);

        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        // Create Service
        _service = new SubscriptionManagementService(
            _masterDbContext,
            _mockCache.Object,
            _mockLogger.Object
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
            EmployeeTier = EmployeeTier.Tier2_101To500,
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

        // Act
        var result = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            subtotal,
            PaymentType.Subscription,
            DateTime.UtcNow.AddDays(30)
        );

        // Assert
        result.Should().NotBeNull();
        result.SubtotalMUR.Should().Be(subtotal);
        result.TaxRate.Should().Be(0.15m);
        result.TaxAmountMUR.Should().Be(expectedTax);
        result.TotalMUR.Should().Be(expectedTotal);
        result.IsTaxExempt.Should().BeFalse();
        result.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task CreatePaymentRecord_WithTaxExemption_ShouldNotAddVAT()
    {
        // Arrange
        var subtotal = 50000.00m;

        // Act
        var result = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            subtotal,
            PaymentType.Subscription,
            DateTime.UtcNow.AddDays(30),
            isTaxExempt: true
        );

        // Assert
        result.Should().NotBeNull();
        result.SubtotalMUR.Should().Be(subtotal);
        result.TaxRate.Should().Be(0m);
        result.TaxAmountMUR.Should().Be(0m);
        result.TotalMUR.Should().Be(subtotal); // No tax added
        result.IsTaxExempt.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePaymentRecord_WithCustomTaxRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var subtotal = 50000.00m;
        var customTaxRate = 0.10m; // 10% tax rate
        var expectedTax = subtotal * customTaxRate; // 5000.00
        var expectedTotal = subtotal + expectedTax; // 55000.00

        // Act
        var result = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            subtotal,
            PaymentType.Subscription,
            DateTime.UtcNow.AddDays(30),
            taxRate: customTaxRate
        );

        // Assert
        result.Should().NotBeNull();
        result.TaxRate.Should().Be(customTaxRate);
        result.TaxAmountMUR.Should().Be(expectedTax);
        result.TotalMUR.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task CreateProRatedPayment_MidYear_ShouldCalculateCorrectly()
    {
        // Arrange
        var tenant = await _masterDbContext.Tenants.FindAsync(_testTenantId);
        tenant!.SubscriptionEndDate = DateTime.UtcNow.AddMonths(6); // 6 months remaining
        await _masterDbContext.SaveChangesAsync();

        var oldPrice = 50000.00m;
        var newPrice = 100000.00m;
        var priceDifference = newPrice - oldPrice; // 50000.00

        // Act
        var result = await _service.CreateProRatedPaymentAsync(
            _testTenantId,
            oldPrice,
            newPrice
        );

        // Assert
        result.Should().NotBeNull();

        // Should be roughly 25000 (50% of year remaining * 50000 difference) + 15% VAT
        var expectedSubtotal = priceDifference / 2; // Approximately for 6 months
        result!.PaymentType.Should().Be(PaymentType.TierUpgrade);
        result.SubtotalMUR.Should().BeGreaterThan(20000m); // At least 20k for 6 months
        result.SubtotalMUR.Should().BeLessThan(30000m); // Less than 30k for 6 months
        result.TaxRate.Should().Be(0.15m);
        result.TaxAmountMUR.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task CreateProRatedPayment_1MonthRemaining_ShouldCalculateSmallAmount()
    {
        // Arrange
        var tenant = await _masterDbContext.Tenants.FindAsync(_testTenantId);
        tenant!.SubscriptionEndDate = DateTime.UtcNow.AddDays(30); // 1 month remaining
        await _masterDbContext.SaveChangesAsync();

        var oldPrice = 50000.00m;
        var newPrice = 100000.00m;
        var priceDifference = newPrice - oldPrice; // 50000.00

        // Act
        var result = await _service.CreateProRatedPaymentAsync(
            _testTenantId,
            oldPrice,
            newPrice
        );

        // Assert
        result.Should().NotBeNull();

        // Should be roughly 4166 (1/12 of year * 50000 difference) + VAT
        result!.SubtotalMUR.Should().BeGreaterThan(3000m); // At least 3k for 1 month
        result.SubtotalMUR.Should().BeLessThan(6000m); // Less than 6k for 1 month
    }

    [Fact]
    public async Task CreateProRatedPayment_ExpiredSubscription_ShouldReturnNull()
    {
        // Arrange
        var tenant = await _masterDbContext.Tenants.FindAsync(_testTenantId);
        tenant!.SubscriptionEndDate = DateTime.UtcNow.AddDays(-1); // Already expired
        await _masterDbContext.SaveChangesAsync();

        // Act
        var result = await _service.CreateProRatedPaymentAsync(
            _testTenantId,
            50000.00m,
            100000.00m
        );

        // Assert
        result.Should().BeNull("subscription already expired, no pro-rating needed");
    }

    [Fact]
    public async Task CreateRenewalPayment_ShouldCreateForNextYear()
    {
        // Arrange
        var tenant = await _masterDbContext.Tenants.FindAsync(_testTenantId);
        var currentEndDate = tenant!.SubscriptionEndDate;
        var yearlyPrice = tenant.YearlyPriceMUR;

        // Act
        var result = await _service.CreateRenewalPaymentAsync(_testTenantId);

        // Assert
        result.Should().NotBeNull();
        result.PaymentType.Should().Be(PaymentType.Renewal);
        result.SubtotalMUR.Should().Be(yearlyPrice);
        result.TaxRate.Should().Be(0.15m);
        result.TaxAmountMUR.Should().Be(yearlyPrice * 0.15m);
        result.TotalMUR.Should().Be(yearlyPrice * 1.15m);
        result.DueDate.Should().BeCloseTo(currentEndDate, TimeSpan.FromDays(1));
    }

    [Fact]
    public async Task RecordPayment_ShouldUpdateStatusAndDate()
    {
        // Arrange
        var payment = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            50000.00m,
            PaymentType.Subscription,
            DateTime.UtcNow.AddDays(30)
        );

        var paymentDate = DateTime.UtcNow;
        var paymentMethod = "Bank Transfer";
        var reference = "TXN-12345";

        // Act
        var result = await _service.RecordPaymentAsync(
            payment.Id,
            paymentDate,
            paymentMethod,
            reference
        );

        // Assert
        result.Should().BeTrue();

        var updatedPayment = await _masterDbContext.SubscriptionPayments.FindAsync(payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.Paid);
        updatedPayment.PaymentDate.Should().BeCloseTo(paymentDate, TimeSpan.FromSeconds(1));
        updatedPayment.PaymentMethod.Should().Be(paymentMethod);
        updatedPayment.PaymentReference.Should().Be(reference);
    }

    [Fact]
    public async Task RecordPartialPayment_ShouldUpdateAmountPaid()
    {
        // Arrange
        var payment = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            50000.00m,
            PaymentType.Subscription,
            DateTime.UtcNow.AddDays(30)
        );

        var partialAmount = 30000.00m;

        // Act
        var result = await _service.RecordPartialPaymentAsync(
            payment.Id,
            partialAmount,
            DateTime.UtcNow,
            "Partial Payment - Bank Transfer",
            "PARTIAL-001"
        );

        // Assert
        result.Should().BeTrue();

        var updatedPayment = await _masterDbContext.SubscriptionPayments.FindAsync(payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.PartiallyPaid);
        updatedPayment.AmountPaidMUR.Should().Be(partialAmount);
        updatedPayment.PaymentMethod.Should().Be("Partial Payment - Bank Transfer");
    }

    [Fact]
    public async Task RefundPayment_ShouldUpdateStatusAndRefundAmount()
    {
        // Arrange
        var payment = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            50000.00m,
            PaymentType.Subscription,
            DateTime.UtcNow.AddDays(30)
        );

        await _service.RecordPaymentAsync(
            payment.Id,
            DateTime.UtcNow,
            "Bank Transfer",
            "TXN-12345"
        );

        var refundAmount = 57500.00m; // Full amount + VAT
        var refundReason = "Service cancellation";

        // Act
        var result = await _service.RefundPaymentAsync(
            payment.Id,
            refundAmount,
            refundReason
        );

        // Assert
        result.Should().BeTrue();

        var updatedPayment = await _masterDbContext.SubscriptionPayments.FindAsync(payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.Refunded);
        updatedPayment.RefundAmountMUR.Should().Be(refundAmount);
        updatedPayment.RefundDate.Should().NotBeNull();
        updatedPayment.RefundReason.Should().Be(refundReason);
    }

    [Fact]
    public async Task VoidPayment_ShouldUpdateStatus()
    {
        // Arrange
        var payment = await _service.CreatePaymentRecordAsync(
            _testTenantId,
            50000.00m,
            PaymentType.Subscription,
            DateTime.UtcNow.AddDays(30)
        );

        // Act
        var result = await _service.VoidPaymentAsync(payment.Id, "Duplicate payment created");

        // Assert
        result.Should().BeTrue();

        var updatedPayment = await _masterDbContext.SubscriptionPayments.FindAsync(payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.Void);
        updatedPayment.VoidReason.Should().Be("Duplicate payment created");
        updatedPayment.VoidedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTotalRevenue_ShouldCalculateCorrectly()
    {
        // Arrange - Create and mark as paid 3 payments
        var payment1 = await _service.CreatePaymentRecordAsync(_testTenantId, 50000m, PaymentType.Subscription, DateTime.UtcNow.AddDays(30));
        var payment2 = await _service.CreatePaymentRecordAsync(_testTenantId, 30000m, PaymentType.TierUpgrade, DateTime.UtcNow.AddDays(60));
        var payment3 = await _service.CreatePaymentRecordAsync(_testTenantId, 20000m, PaymentType.Renewal, DateTime.UtcNow.AddDays(90));

        await _service.RecordPaymentAsync(payment1.Id, DateTime.UtcNow, "Bank", "REF1");
        await _service.RecordPaymentAsync(payment2.Id, DateTime.UtcNow, "Bank", "REF2");
        await _service.RecordPaymentAsync(payment3.Id, DateTime.UtcNow, "Bank", "REF3");

        var expectedTotal = (50000m + 30000m + 20000m) * 1.15m; // Include VAT

        // Act
        var result = await _service.GetTotalRevenueAsync();

        // Assert
        result.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task GetARR_ShouldCalculateCorrectly()
    {
        // Arrange - Create annual subscription payments
        var payment1 = await _service.CreatePaymentRecordAsync(_testTenantId, 50000m, PaymentType.Subscription, DateTime.UtcNow);
        await _service.RecordPaymentAsync(payment1.Id, DateTime.UtcNow, "Bank", "REF1");

        var expectedARR = 50000m * 1.15m; // Subtotal + VAT

        // Act
        var result = await _service.GetARRAsync();

        // Assert
        result.Should().Be(expectedARR);
    }

    [Fact]
    public async Task GetMRR_ShouldCalculateCorrectly()
    {
        // Arrange
        var payment1 = await _service.CreatePaymentRecordAsync(_testTenantId, 50000m, PaymentType.Subscription, DateTime.UtcNow);
        await _service.RecordPaymentAsync(payment1.Id, DateTime.UtcNow, "Bank", "REF1");

        var expectedMRR = (50000m * 1.15m) / 12m; // ARR / 12

        // Act
        var result = await _service.GetMRRAsync();

        // Assert
        result.Should().BeApproximately(expectedMRR, 0.01m);
    }

    [Fact]
    public async Task GetPendingPayments_ShouldReturnOnlyPendingPayments()
    {
        // Arrange
        var pending1 = await _service.CreatePaymentRecordAsync(_testTenantId, 50000m, PaymentType.Subscription, DateTime.UtcNow.AddDays(30));
        var pending2 = await _service.CreatePaymentRecordAsync(_testTenantId, 30000m, PaymentType.Renewal, DateTime.UtcNow.AddDays(60));
        var paid = await _service.CreatePaymentRecordAsync(_testTenantId, 20000m, PaymentType.Subscription, DateTime.UtcNow.AddDays(90));

        await _service.RecordPaymentAsync(paid.Id, DateTime.UtcNow, "Bank", "REF1");

        // Act
        var result = await _service.GetPendingPaymentsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Id == pending1.Id);
        result.Should().Contain(p => p.Id == pending2.Id);
        result.Should().NotContain(p => p.Id == paid.Id);
    }

    [Fact]
    public async Task GetOverduePayments_ShouldReturnOnlyOverduePayments()
    {
        // Arrange
        var overdue = await _service.CreatePaymentRecordAsync(_testTenantId, 50000m, PaymentType.Subscription, DateTime.UtcNow.AddDays(-5));
        var upcoming = await _service.CreatePaymentRecordAsync(_testTenantId, 30000m, PaymentType.Renewal, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _service.GetOverduePaymentsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(overdue.Id);
        result.Should().NotContain(p => p.Id == upcoming.Id);
    }

    public void Dispose()
    {
        _masterDbContext?.Dispose();
    }
}
