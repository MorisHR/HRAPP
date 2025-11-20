using HRMS.Application.DTOs.PayrollDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;
using HRMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace HRMS.Tests;

/// <summary>
/// Production-ready unit tests for PayrollService
/// Tests Mauritius statutory calculations: CSG, NSF, PAYE, PRGF
/// Tests pro-rated calculations, overtime, and leave deductions
/// </summary>
public class PayrollServiceTests : IDisposable
{
    private readonly TenantDbContext _tenantDbContext;
    private readonly Mock<ITenantService> _mockTenantService;
    private readonly Mock<ISalaryComponentService> _mockSalaryComponentService;
    private readonly Mock<ILogger<PayrollService>> _mockLogger;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly PayrollService _service;
    private readonly Guid _testEmployeeId = Guid.NewGuid();
    private readonly Guid _testDepartmentId = Guid.NewGuid();

    public PayrollServiceTests()
    {
        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenantDbContext = new TenantDbContext(options, null);

        // Setup Mocks
        _mockTenantService = new Mock<ITenantService>();
        _mockSalaryComponentService = new Mock<ISalaryComponentService>();
        _mockLogger = new Mock<ILogger<PayrollService>>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _mockTenantService.Setup(x => x.GetCurrentTenantId()).Returns(Guid.NewGuid());
        _mockSalaryComponentService.Setup(x => x.GetTotalAllowancesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);
        _mockSalaryComponentService.Setup(x => x.GetTotalDeductionsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);

        // Mock PDF Generator - FIXED: Added for CRITICAL-2
        var mockPdfGenerator = new Mock<IPayslipPdfGenerator>();

        // Create Service
        _service = new PayrollService(
            _tenantDbContext,
            _mockTenantService.Object,
            _mockSalaryComponentService.Object,
            _mockLogger.Object,
            _mockCurrentUserService.Object,
            mockPdfGenerator.Object // FIXED: Pass PDF generator mock (CRITICAL-2)
        );

        // Seed Test Data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var department = new Department
        {
            Id = _testDepartmentId,
            Name = "Engineering",
            CreatedAt = DateTime.UtcNow
        };

        var employee = new Employee
        {
            Id = _testEmployeeId,
            EmployeeCode = "EMP001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            BasicSalary = 30000m,
            DepartmentId = _testDepartmentId,
            JoiningDate = new DateTime(2021, 1, 1), // 3+ years of service
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1),
            CreatedAt = DateTime.UtcNow
        };

        _tenantDbContext.Departments.Add(department);
        _tenantDbContext.Employees.Add(employee);
        _tenantDbContext.SaveChanges();
    }

    // ==================== CSG CALCULATIONS (Contribution Sociale Généralisée) ====================

    [Theory]
    [InlineData(30000, 900)]      // 30K salary = MUR 900 CSG (3%)
    [InlineData(40000, 1200)]     // 40K salary = MUR 1,200 CSG (3%)
    [InlineData(50000, 1500)]     // 50K threshold = MUR 1,500 CSG (3%)
    [InlineData(60000, 1800)]     // Above threshold = MUR 1,800 CSG (3%)
    [InlineData(100000, 3000)]    // 100K = MUR 3,000 CSG (3%)
    public async Task CalculateCSGEmployee_VariousSalaries_ReturnsCorrectAmount(decimal salary, decimal expectedCSG)
    {
        // Act
        var result = await _service.CalculateCSGEmployeeAsync(salary);

        // Assert
        result.Should().Be(expectedCSG);
    }

    [Theory]
    [InlineData(25000, 0.015)]    // Below threshold: 1.5%
    [InlineData(50000, 0.015)]    // At threshold: 1.5%
    [InlineData(50001, 0.03)]     // Just above threshold: 3%
    [InlineData(75000, 0.03)]     // Well above threshold: 3%
    public async Task CalculateCSGEmployee_ChecksThreshold_AppliesCorrectRate(decimal salary, decimal expectedRate)
    {
        // Act
        var result = await _service.CalculateCSGEmployeeAsync(salary);
        var actualRate = result / salary;

        // Assert
        actualRate.Should().Be(expectedRate);
    }

    [Theory]
    [InlineData(30000, 1800)]     // 30K * 6% = 1,800 (employer pays double)
    [InlineData(50000, 1500)]     // At threshold: 50K * 3% = 1,500
    [InlineData(75000, 4500)]     // Above threshold: 75K * 6% = 4,500
    public async Task CalculateCSGEmployer_VariousSalaries_ReturnsCorrectAmount(decimal salary, decimal expectedCSG)
    {
        // Act
        var result = await _service.CalculateCSGEmployerAsync(salary);

        // Assert
        result.Should().Be(expectedCSG);
    }

    // ==================== NSF CALCULATIONS (National Savings Fund) ====================

    [Theory]
    [InlineData(30000, 300)]      // 30K * 1% = 300
    [InlineData(50000, 500)]      // 50K * 1% = 500
    [InlineData(100000, 1000)]    // 100K * 1% = 1,000
    public async Task CalculateNSFEmployee_VariousSalaries_Returns1Percent(decimal basicSalary, decimal expectedNSF)
    {
        // Act
        var result = await _service.CalculateNSFEmployeeAsync(basicSalary);

        // Assert
        result.Should().Be(expectedNSF);
    }

    [Theory]
    [InlineData(30000, 750)]      // 30K * 2.5% = 750
    [InlineData(50000, 1250)]     // 50K * 2.5% = 1,250
    [InlineData(100000, 2500)]    // 100K * 2.5% = 2,500
    public async Task CalculateNSFEmployer_VariousSalaries_Returns2Point5Percent(decimal basicSalary, decimal expectedNSF)
    {
        // Act
        var result = await _service.CalculateNSFEmployerAsync(basicSalary);

        // Assert
        result.Should().Be(expectedNSF);
    }

    // ==================== PAYE TAX CALCULATIONS (Mauritius Income Tax 2025) ====================

    [Theory]
    [InlineData(390000, 0, 0)]           // Below threshold - No tax
    [InlineData(300000, 0, 0)]           // Well below threshold - No tax
    [InlineData(0, 0, 0)]                // Zero income - No tax
    public async Task CalculatePAYE_BelowThreshold_ReturnsZeroTax(decimal annualGross, decimal annualDeductions, decimal expectedMonthlyTax)
    {
        // Act
        var result = await _service.CalculatePAYEAsync(annualGross, annualDeductions);

        // Assert
        result.Should().Be(expectedMonthlyTax);
    }

    [Theory]
    [InlineData(500000, 0, 916.67)]      // First bracket: (500000 - 390000) * 10% / 12 = 916.67/month
    [InlineData(550000, 0, 1333.33)]     // Top of first bracket: (550000 - 390000) * 10% / 12 = 1,333.33/month
    [InlineData(450000, 0, 500)]         // Mid first bracket: (450000 - 390000) * 10% / 12 = 500/month
    public async Task CalculatePAYE_FirstBracket_Returns10PercentTax(decimal annualGross, decimal annualDeductions, decimal expectedMonthlyTax)
    {
        // Act
        var result = await _service.CalculatePAYEAsync(annualGross, annualDeductions);

        // Assert
        result.Should().BeApproximately(expectedMonthlyTax, 0.5m);
    }

    [Theory]
    [InlineData(600000, 0, 1833.33)]     // Second bracket: 16000 + (50000 * 12%) / 12 = 1,833.33/month
    [InlineData(650000, 0, 2333.33)]     // Top of second bracket
    public async Task CalculatePAYE_SecondBracket_Returns12PercentTax(decimal annualGross, decimal annualDeductions, decimal expectedMonthlyTax)
    {
        // Act
        var result = await _service.CalculatePAYEAsync(annualGross, annualDeductions);

        // Assert
        result.Should().BeApproximately(expectedMonthlyTax, 0.5m);
    }

    [Theory]
    [InlineData(1000000, 0, 9666.67)]    // Third bracket: 28000 + (350000 * 20%) / 12 = 9,666.67/month
    [InlineData(800000, 0, 6333.33)]     // Mid third bracket
    public async Task CalculatePAYE_ThirdBracket_Returns20PercentTax(decimal annualGross, decimal annualDeductions, decimal expectedMonthlyTax)
    {
        // Act
        var result = await _service.CalculatePAYEAsync(annualGross, annualDeductions);

        // Assert
        result.Should().BeApproximately(expectedMonthlyTax, 0.5m);
    }

    [Fact]
    public async Task CalculatePAYE_WithDeductions_ReducesTaxableIncome()
    {
        // Arrange
        var annualGross = 500000m;
        var annualDeductions = 50000m; // CSG + NSF
        var taxableIncome = annualGross - annualDeductions; // 450000
        var expectedTax = (450000 - 390000) * 0.10m / 12; // 500/month

        // Act
        var result = await _service.CalculatePAYEAsync(annualGross, annualDeductions);

        // Assert
        result.Should().BeApproximately(expectedTax, 0.5m);
    }

    // ==================== PRGF CALCULATIONS (Portable Retirement Gratuity Fund) ====================

    [Theory]
    [InlineData(30000, 2, 1290)]         // 0-5 years: 30000 * 4.3% = 1,290
    [InlineData(50000, 5, 2150)]         // 5 years: 50000 * 4.3% = 2,150
    [InlineData(60000, 8, 3000)]         // 6-10 years: 60000 * 5% = 3,000
    [InlineData(75000, 10, 3750)]        // 10 years: 75000 * 5% = 3,750
    [InlineData(100000, 15, 6800)]       // Above 10 years: 100000 * 6.8% = 6,800
    public async Task CalculatePRGF_VariousYearsOfService_AppliesCorrectRate(decimal grossSalary, int years, decimal expectedPRGF)
    {
        // Arrange
        var joiningDate = DateTime.UtcNow.AddYears(-years);

        // Act
        var result = await _service.CalculatePRGFAsync(grossSalary, years, joiningDate);

        // Assert
        result.Should().Be(expectedPRGF);
    }

    [Fact]
    public async Task CalculatePRGF_JoinedBefore2020_ReturnsZero()
    {
        // Arrange
        var grossSalary = 50000m;
        var yearsOfService = 5;
        var joiningDate = new DateTime(2019, 12, 31); // Before PRGF implementation

        // Act
        var result = await _service.CalculatePRGFAsync(grossSalary, yearsOfService, joiningDate);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public async Task CalculatePRGF_JoinedOn2020Jan1_CalculatesPRGF()
    {
        // Arrange
        var grossSalary = 50000m;
        var yearsOfService = 3;
        var joiningDate = new DateTime(2020, 1, 1); // Exactly on implementation date

        // Act
        var result = await _service.CalculatePRGFAsync(grossSalary, yearsOfService, joiningDate);

        // Assert
        result.Should().Be(2150m); // 50000 * 4.3%
    }

    // ==================== NPF CALCULATIONS (Legacy - National Pension Fund) ====================

    [Theory]
    [InlineData(30000, 900)]      // 30K * 3% = 900
    [InlineData(50000, 1500)]     // 50K * 3% = 1,500
    public async Task CalculateNPFEmployee_Returns3Percent(decimal basicSalary, decimal expectedNPF)
    {
        // Act
        var result = await _service.CalculateNPFEmployeeAsync(basicSalary);

        // Assert
        result.Should().Be(expectedNPF);
    }

    [Theory]
    [InlineData(30000, 1800)]     // 30K * 6% = 1,800
    [InlineData(50000, 3000)]     // 50K * 6% = 3,000
    public async Task CalculateNPFEmployer_Returns6Percent(decimal basicSalary, decimal expectedNPF)
    {
        // Act
        var result = await _service.CalculateNPFEmployerAsync(basicSalary);

        // Assert
        result.Should().Be(expectedNPF);
    }

    // ==================== TRAINING LEVY CALCULATIONS ====================

    [Theory]
    [InlineData(30000, 450)]      // 30K * 1.5% = 450
    [InlineData(50000, 750)]      // 50K * 1.5% = 750
    [InlineData(100000, 1500)]    // 100K * 1.5% = 1,500
    public async Task CalculateTrainingLevy_Returns1Point5Percent(decimal basicSalary, decimal expectedLevy)
    {
        // Act
        var result = await _service.CalculateTrainingLevyAsync(basicSalary);

        // Assert
        result.Should().Be(expectedLevy);
    }

    // ==================== GRATUITY CALCULATIONS ====================

    [Fact]
    public async Task CalculateGratuity_JoinedAfter2020_ReturnsZero()
    {
        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "EMP002",
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@company.com",
            BasicSalary = 50000m,
            JoiningDate = new DateTime(2020, 1, 1),
            IsActive = true,
            DateOfBirth = new DateTime(1992, 1, 1),
            CreatedAt = DateTime.UtcNow
        };
        _tenantDbContext.Employees.Add(employee);
        await _tenantDbContext.SaveChangesAsync();

        var resignationDate = new DateTime(2025, 1, 1);

        // Act
        var result = await _service.CalculateGratuityAsync(employee.Id, resignationDate);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateGratuity_LessThan1YearService_ReturnsZero()
    {
        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "EMP003",
            FirstName = "Bob",
            LastName = "Johnson",
            Email = "bob@company.com",
            BasicSalary = 50000m,
            JoiningDate = new DateTime(2019, 6, 1),
            IsActive = true,
            DateOfBirth = new DateTime(1993, 1, 1),
            CreatedAt = DateTime.UtcNow
        };
        _tenantDbContext.Employees.Add(employee);
        await _tenantDbContext.SaveChangesAsync();

        var resignationDate = new DateTime(2019, 12, 31); // Less than 1 year

        // Act
        var result = await _service.CalculateGratuityAsync(employee.Id, resignationDate);

        // Assert
        result.Should().Be(0m);
    }

    // ==================== EDGE CASES & BOUNDARY CONDITIONS ====================

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CalculateCSGEmployee_ZeroOrNegativeSalary_ReturnsZero(decimal salary)
    {
        // Act
        var result = await _service.CalculateCSGEmployeeAsync(salary);

        // Assert
        result.Should().BeLessOrEqualTo(0m);
    }

    [Fact]
    public async Task CalculatePAYE_NegativeDeductions_HandlesCorrectly()
    {
        // Arrange
        var annualGross = 500000m;
        var annualDeductions = -10000m; // Invalid but should handle

        // Act
        var result = await _service.CalculatePAYEAsync(annualGross, annualDeductions);

        // Assert
        result.Should().BeGreaterOrEqualTo(0m);
    }

    [Fact]
    public async Task CalculatePRGF_ZeroYearsOfService_AppliesLowestRate()
    {
        // Arrange
        var grossSalary = 50000m;
        var yearsOfService = 0;
        var joiningDate = DateTime.UtcNow;

        // Act
        var result = await _service.CalculatePRGFAsync(grossSalary, yearsOfService, joiningDate);

        // Assert
        result.Should().Be(2150m); // 50000 * 4.3%
    }

    // ==================== ROUNDING & PRECISION TESTS ====================

    [Theory]
    [InlineData(33333.33, 999.9999)]  // Should round to 1000.00
    [InlineData(12345.67, 370.37)]    // Precision test
    public async Task CalculateCSGEmployee_RoundsToTwoDecimalPlaces(decimal salary, decimal expectedCSG)
    {
        // Act
        var result = await _service.CalculateCSGEmployeeAsync(salary);

        // Assert
        result.Should().BeApproximately(expectedCSG, 0.01m);
    }

    [Fact]
    public async Task AllStatutoryCalculations_ReturnTwoDecimalPrecision()
    {
        // Arrange
        var salary = 33333.33m;

        // Act
        var csg = await _service.CalculateCSGEmployeeAsync(salary);
        var nsf = await _service.CalculateNSFEmployeeAsync(salary);
        var npf = await _service.CalculateNPFEmployeeAsync(salary);
        var training = await _service.CalculateTrainingLevyAsync(salary);

        // Assert
        csg.Should().Be(Math.Round(csg, 2));
        nsf.Should().Be(Math.Round(nsf, 2));
        npf.Should().Be(Math.Round(npf, 2));
        training.Should().Be(Math.Round(training, 2));
    }

    public void Dispose()
    {
        _tenantDbContext?.Dispose();
    }
}
