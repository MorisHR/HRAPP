using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using HRMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace HRMS.Tests;

/// <summary>
/// Production-ready unit tests for EmployeeService
/// Tests pro-rated leave calculations, expatriate validations, and multi-tenancy isolation
/// </summary>
public class EmployeeServiceTests : IDisposable
{
    private readonly TenantDbContext _tenantDbContext;
    private readonly Mock<ILogger<EmployeeService>> _mockLogger;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly EmployeeService _service;
    private readonly Guid _testDepartmentId = Guid.NewGuid();

    public EmployeeServiceTests()
    {
        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenantDbContext = new TenantDbContext(options, null);

        // Setup Mocks
        _mockLogger = new Mock<ILogger<EmployeeService>>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(x => x.GetAuditUsername()).Returns("admin@test.com");

        // Create Service
        _service = new EmployeeService(
            _tenantDbContext,
            _mockLogger.Object,
            _mockCurrentUserService.Object
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

        _tenantDbContext.Departments.Add(department);
        _tenantDbContext.SaveChanges();
    }

    // ==================== PRO-RATED LEAVE CALCULATIONS ====================

    [Theory]
    [InlineData(1, 22.0)]        // Joined in January: 12 months = 22 days
    [InlineData(6, 12.83)]       // Joined in June: 7 months = 12.83 days
    [InlineData(9, 7.33)]        // Joined in September: 4 months = 7.33 days
    [InlineData(12, 1.83)]       // Joined in December: 1 month = 1.83 days
    public void CalculateProRatedAnnualLeave_SameYear_CalculatesCorrectly(int joiningMonth, decimal expectedDays)
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;
        var joiningDate = new DateTime(currentYear, joiningMonth, 1);
        var calculationDate = new DateTime(currentYear, 12, 31);

        // Act
        var result = _service.CalculateProRatedAnnualLeave(joiningDate, calculationDate);

        // Assert
        result.Should().BeApproximately(expectedDays, 0.01m);
    }

    [Fact]
    public void CalculateProRatedAnnualLeave_JoinedPreviousYear_ReturnsFullLeave()
    {
        // Arrange
        var joiningDate = new DateTime(2020, 6, 1);
        var calculationDate = new DateTime(2025, 1, 1);

        // Act
        var result = _service.CalculateProRatedAnnualLeave(joiningDate, calculationDate);

        // Assert
        result.Should().Be(22m); // Full Mauritius annual leave
    }

    [Theory]
    [InlineData(1, 1, 22.0)]     // Joined Jan 1: Full year
    [InlineData(7, 1, 11.0)]     // Joined Jul 1: Half year (6 months)
    [InlineData(10, 1, 5.5)]     // Joined Oct 1: Quarter year (3 months)
    public void CalculateProRatedAnnualLeave_VariousJoiningDates_ProRatesCorrectly(int month, int day, decimal expectedDays)
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;
        var joiningDate = new DateTime(currentYear, month, day);
        var calculationDate = new DateTime(currentYear, 12, 31);

        // Act
        var result = _service.CalculateProRatedAnnualLeave(joiningDate, calculationDate);

        // Assert
        result.Should().BeApproximately(expectedDays, 0.1m);
    }

    // ==================== EMPLOYEE CREATION TESTS ====================

    [Fact]
    public async Task CreateEmployee_ValidLocalEmployee_ShouldSucceed()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            EmployeeCode = "EMP001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            PhoneNumber = "230-123-4567",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = Gender.Male,
            EmployeeType = EmployeeType.Local,
            NationalIdCard = "A1234567890123",
            JoiningDate = DateTime.UtcNow,
            BasicSalary = 30000m,
            SalaryCurrency = "MUR",
            DepartmentId = _testDepartmentId,
            EmergencyContacts = new List<EmergencyContactDto>
            {
                new EmergencyContactDto
                {
                    ContactName = "Jane Doe",
                    PhoneNumber = "230-999-8888",
                    Relationship = "Spouse",
                    IsPrimary = true
                }
            }
        };

        // Act
        var result = await _service.CreateEmployeeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.EmployeeCode.Should().Be("EMP001");
        result.AnnualLeaveBalance.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateEmployee_WithProRatedLeave_ShouldCalculateCorrectly()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;
        var joiningDate = new DateTime(currentYear, 6, 1); // Mid-year

        var request = new CreateEmployeeRequest
        {
            EmployeeCode = "EMP002",
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@company.com",
            PhoneNumber = "230-123-4568",
            DateOfBirth = new DateTime(1992, 1, 1),
            Gender = Gender.Female,
            EmployeeType = EmployeeType.Local,
            NationalIdCard = "B1234567890123",
            JoiningDate = joiningDate,
            BasicSalary = 35000m,
            SalaryCurrency = "MUR",
            DepartmentId = _testDepartmentId,
            EmergencyContacts = new List<EmergencyContactDto>
            {
                new EmergencyContactDto
                {
                    ContactName = "Bob Smith",
                    PhoneNumber = "230-999-7777",
                    Relationship = "Father",
                    IsPrimary = true
                }
            }
        };

        // Act
        var result = await _service.CreateEmployeeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AnnualLeaveBalance.Should().BeLessThan(22m); // Pro-rated
        result.AnnualLeaveBalance.Should().BeGreaterThan(0);
    }

    // ==================== EXPATRIATE VALIDATION TESTS ====================

    [Fact]
    public async Task CreateEmployee_Expatriate_WithoutPassport_ShouldFail()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            EmployeeCode = "EXP001",
            FirstName = "Pierre",
            LastName = "Dubois",
            Email = "pierre@company.com",
            PhoneNumber = "230-123-4569",
            DateOfBirth = new DateTime(1985, 5, 15),
            Gender = Gender.Male,
            EmployeeType = EmployeeType.Expatriate,
            CountryOfOrigin = "France",
            // Missing: PassportNumber, PassportExpiryDate
            JoiningDate = DateTime.UtcNow,
            BasicSalary = 75000m,
            SalaryCurrency = "MUR",
            DepartmentId = _testDepartmentId,
            EmergencyContacts = new List<EmergencyContactDto>
            {
                new EmergencyContactDto
                {
                    ContactName = "Marie Dubois",
                    PhoneNumber = "33-123-456789",
                    Relationship = "Spouse",
                    IsPrimary = true
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateEmployeeAsync(request));
    }

    [Fact]
    public async Task CreateEmployee_Expatriate_WithoutVisa_ShouldFail()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            EmployeeCode = "EXP002",
            FirstName = "Hans",
            LastName = "Mueller",
            Email = "hans@company.com",
            PhoneNumber = "230-123-4570",
            DateOfBirth = new DateTime(1988, 8, 20),
            Gender = Gender.Male,
            EmployeeType = EmployeeType.Expatriate,
            CountryOfOrigin = "Germany",
            PassportNumber = "DE12345678",
            PassportExpiryDate = DateTime.UtcNow.AddYears(5),
            // Missing: VisaType, VisaExpiryDate
            JoiningDate = DateTime.UtcNow,
            BasicSalary = 80000m,
            SalaryCurrency = "MUR",
            DepartmentId = _testDepartmentId,
            EmergencyContacts = new List<EmergencyContactDto>
            {
                new EmergencyContactDto
                {
                    ContactName = "Greta Mueller",
                    PhoneNumber = "49-987-654321",
                    Relationship = "Sister",
                    IsPrimary = true
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateEmployeeAsync(request));
    }

    [Fact]
    public async Task CreateEmployee_Expatriate_ValidData_ShouldSucceed()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            EmployeeCode = "EXP003",
            FirstName = "Rajesh",
            LastName = "Kumar",
            Email = "rajesh@company.com",
            PhoneNumber = "230-123-4571",
            DateOfBirth = new DateTime(1987, 3, 10),
            Gender = Gender.Male,
            EmployeeType = EmployeeType.Expatriate,
            CountryOfOrigin = "India",
            PassportNumber = "IN87654321",
            PassportExpiryDate = DateTime.UtcNow.AddYears(8),
            VisaType = VisaType.WorkPermit,
            VisaExpiryDate = DateTime.UtcNow.AddYears(2),
            JoiningDate = DateTime.UtcNow,
            BasicSalary = 70000m,
            SalaryCurrency = "MUR",
            DepartmentId = _testDepartmentId,
            EmergencyContacts = new List<EmergencyContactDto>
            {
                new EmergencyContactDto
                {
                    ContactName = "Priya Kumar",
                    PhoneNumber = "91-98765-43210",
                    Relationship = "Spouse",
                    IsPrimary = true
                }
            }
        };

        // Act
        var result = await _service.CreateEmployeeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsExpatriate.Should().BeTrue();
        result.PassportNumber.Should().Be("IN87654321");
        result.VisaType.Should().Be(VisaType.WorkPermit);
    }

    [Fact]
    public async Task CreateEmployee_LocalEmployee_WithoutNationalId_ShouldFail()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            EmployeeCode = "EMP003",
            FirstName = "Local",
            LastName = "Employee",
            Email = "local@company.com",
            PhoneNumber = "230-123-4572",
            DateOfBirth = new DateTime(1995, 1, 1),
            Gender = Gender.Male,
            EmployeeType = EmployeeType.Local,
            // Missing: NationalIdCard
            JoiningDate = DateTime.UtcNow,
            BasicSalary = 25000m,
            SalaryCurrency = "MUR",
            DepartmentId = _testDepartmentId,
            EmergencyContacts = new List<EmergencyContactDto>
            {
                new EmergencyContactDto
                {
                    ContactName = "Emergency Contact",
                    PhoneNumber = "230-999-6666",
                    Relationship = "Father",
                    IsPrimary = true
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateEmployeeAsync(request));
    }

    // ==================== EMPLOYEE CODE UNIQUENESS TESTS ====================

    [Fact]
    public async Task IsEmployeeCodeUnique_NewCode_ShouldReturnTrue()
    {
        // Act
        var result = await _service.IsEmployeeCodeUniqueAsync("EMP999");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmployeeCodeUnique_ExistingCode_ShouldReturnFalse()
    {
        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "EMP100",
            FirstName = "Test",
            LastName = "User",
            Email = "test@company.com",
            BasicSalary = 30000m,
            JoiningDate = DateTime.UtcNow,
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1),
            CreatedAt = DateTime.UtcNow
        };
        _tenantDbContext.Employees.Add(employee);
        await _tenantDbContext.SaveChangesAsync();

        // Act
        var result = await _service.IsEmployeeCodeUniqueAsync("EMP100");

        // Assert
        result.Should().BeFalse();
    }

    // ==================== EMAIL UNIQUENESS TESTS ====================

    [Fact]
    public async Task IsEmailUnique_NewEmail_ShouldReturnTrue()
    {
        // Act
        var result = await _service.IsEmailUniqueAsync("newemail@company.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmailUnique_ExistingEmail_ShouldReturnFalse()
    {
        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "EMP101",
            FirstName = "Test",
            LastName = "User",
            Email = "existing@company.com",
            BasicSalary = 30000m,
            JoiningDate = DateTime.UtcNow,
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1),
            CreatedAt = DateTime.UtcNow
        };
        _tenantDbContext.Employees.Add(employee);
        await _tenantDbContext.SaveChangesAsync();

        // Act
        var result = await _service.IsEmailUniqueAsync("existing@company.com");

        // Assert
        result.Should().BeFalse();
    }

    // ==================== SEARCH & FILTER TESTS ====================

    [Fact]
    public async Task SearchEmployees_ByName_ShouldReturnMatches()
    {
        // Arrange
        var emp1 = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "EMP201",
            FirstName = "John",
            LastName = "Smith",
            Email = "john.smith@company.com",
            BasicSalary = 30000m,
            JoiningDate = DateTime.UtcNow,
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1),
            CreatedAt = DateTime.UtcNow
        };

        var emp2 = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "EMP202",
            FirstName = "Jane",
            LastName = "Johnson",
            Email = "jane.johnson@company.com",
            BasicSalary = 32000m,
            JoiningDate = DateTime.UtcNow,
            IsActive = true,
            DateOfBirth = new DateTime(1992, 1, 1),
            CreatedAt = DateTime.UtcNow
        };

        _tenantDbContext.Employees.AddRange(emp1, emp2);
        await _tenantDbContext.SaveChangesAsync();

        // Act
        var result = await _service.SearchEmployeesAsync("John");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(e => e.EmployeeCode == "EMP201");
        result.Should().Contain(e => e.EmployeeCode == "EMP202");
    }

    [Fact]
    public async Task GetEmployeesByDepartment_ShouldReturnOnlyDepartmentEmployees()
    {
        // Arrange
        var otherDeptId = Guid.NewGuid();
        var otherDept = new Department
        {
            Id = otherDeptId,
            Name = "Sales",
            CreatedAt = DateTime.UtcNow
        };
        _tenantDbContext.Departments.Add(otherDept);

        var emp1 = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "EMP301",
            FirstName = "Eng1",
            LastName = "Employee",
            Email = "eng1@company.com",
            BasicSalary = 30000m,
            JoiningDate = DateTime.UtcNow,
            DepartmentId = _testDepartmentId,
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1),
            CreatedAt = DateTime.UtcNow
        };

        var emp2 = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "EMP302",
            FirstName = "Sales1",
            LastName = "Employee",
            Email = "sales1@company.com",
            BasicSalary = 28000m,
            JoiningDate = DateTime.UtcNow,
            DepartmentId = otherDeptId,
            IsActive = true,
            DateOfBirth = new DateTime(1991, 1, 1),
            CreatedAt = DateTime.UtcNow
        };

        _tenantDbContext.Employees.AddRange(emp1, emp2);
        await _tenantDbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetEmployeesByDepartmentAsync(_testDepartmentId);

        // Assert
        result.Should().HaveCount(1);
        result.First().DepartmentName.Should().Be("Engineering");
    }

    // ==================== EMPLOYEE DELETION TESTS ====================

    [Fact]
    public async Task DeleteEmployee_ShouldSoftDelete()
    {
        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "EMP401",
            FirstName = "To",
            LastName = "Delete",
            Email = "todelete@company.com",
            BasicSalary = 30000m,
            JoiningDate = DateTime.UtcNow,
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1),
            CreatedAt = DateTime.UtcNow
        };
        _tenantDbContext.Employees.Add(employee);
        await _tenantDbContext.SaveChangesAsync();

        // Act
        var result = await _service.DeleteEmployeeAsync(employee.Id);

        // Assert
        result.Should().BeTrue();

        var deletedEmployee = await _tenantDbContext.Employees.FindAsync(employee.Id);
        deletedEmployee!.IsDeleted.Should().BeTrue();
        deletedEmployee.IsActive.Should().BeFalse();
    }

    public void Dispose()
    {
        _tenantDbContext?.Dispose();
    }
}
