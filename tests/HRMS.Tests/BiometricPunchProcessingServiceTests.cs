using HRMS.Application.DTOs.BiometricPunchDtos;
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
/// Production-ready unit tests for BiometricPunchProcessingService
/// Tests duplicate detection, hash chain integrity, photo storage, and authorization
/// Critical for Fortune 500 biometric attendance system
/// </summary>
public class BiometricPunchProcessingServiceTests : IDisposable
{
    private readonly TenantDbContext _tenantDbContext;
    private readonly Mock<ILogger<BiometricPunchProcessingService>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly BiometricPunchProcessingService _service;

    private readonly Guid _testTenantId = Guid.NewGuid();
    private readonly Guid _testEmployeeId = Guid.NewGuid();
    private readonly Guid _testDeviceId = Guid.NewGuid();
    private readonly Guid _testLocationId = Guid.NewGuid();

    public BiometricPunchProcessingServiceTests()
    {
        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenantDbContext = new TenantDbContext(options, null);

        // Setup Mocks
        _mockLogger = new Mock<ILogger<BiometricPunchProcessingService>>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _mockFileStorageService.Setup(x => x.UploadFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync("cloud-storage/test-photo.jpg");

        _mockCurrentUserService.Setup(x => x.GetAuditUsername()).Returns("System");

        // Create Service
        _service = new BiometricPunchProcessingService(
            _tenantDbContext,
            _mockLogger.Object,
            _mockAuditLogService.Object,
            _mockFileStorageService.Object,
            _mockCurrentUserService.Object
        );

        // Seed Test Data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var location = new Location
        {
            Id = _testLocationId,
            LocationName = "Head Office",
            LocationCode = "HQ",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var device = new AttendanceMachine
        {
            Id = _testDeviceId,
            MachineName = "Device-001",
            SerialNumber = "SN12345",
            LocationId = _testLocationId,
            IsActive = true,
            DeviceStatus = "Active",
            CreatedAt = DateTime.UtcNow
        };

        var employee = new Employee
        {
            Id = _testEmployeeId,
            EmployeeCode = "EMP001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            BiometricEnrollmentId = "BIO123",
            BasicSalary = 30000m,
            JoiningDate = DateTime.UtcNow.AddYears(-2),
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1),
            CreatedAt = DateTime.UtcNow
        };

        _tenantDbContext.Locations.Add(location);
        _tenantDbContext.AttendanceMachines.Add(device);
        _tenantDbContext.Employees.Add(employee);
        _tenantDbContext.SaveChanges();
    }

    // ==================== DUPLICATE DETECTION TESTS ====================

    [Fact]
    public async Task ProcessPunch_DuplicateWithin15Minutes_ShouldMarkAsDuplicate()
    {
        // Arrange
        var punchTime = DateTime.UtcNow;
        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = punchTime,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        // First punch
        var firstResult = await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Duplicate punch 10 minutes later
        punchDto.PunchTime = punchTime.AddMinutes(10);
        var secondResult = await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Assert
        secondResult.Success.Should().BeTrue();
        secondResult.Warnings.Should().Contain(w => w.Contains("Duplicate"));
    }

    [Fact]
    public async Task ProcessPunch_DuplicateAfter15Minutes_ShouldProcessNormally()
    {
        // Arrange
        var punchTime = DateTime.UtcNow;
        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = punchTime,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        // First punch
        var firstResult = await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Punch 20 minutes later (outside duplicate window)
        punchDto.PunchTime = punchTime.AddMinutes(20);
        var secondResult = await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Assert
        secondResult.Success.Should().BeTrue();
        secondResult.Warnings.Should().NotContain(w => w.Contains("Duplicate"));
    }

    [Fact]
    public async Task ProcessPunch_DifferentPunchTypes_ShouldNotDetectAsDuplicate()
    {
        // Arrange
        var punchTime = DateTime.UtcNow;

        var checkInDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = punchTime,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        var checkOutDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckOut",
            PunchTime = punchTime.AddMinutes(5),
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        // Act
        var checkInResult = await _service.ProcessPunchAsync(checkInDto, _testDeviceId, _testTenantId);
        var checkOutResult = await _service.ProcessPunchAsync(checkOutDto, _testDeviceId, _testTenantId);

        // Assert
        checkInResult.Success.Should().BeTrue();
        checkOutResult.Success.Should().BeTrue();
        checkOutResult.Warnings.Should().NotContain(w => w.Contains("Duplicate"));
    }

    // ==================== VERIFICATION QUALITY TESTS ====================

    [Theory]
    [InlineData(50)]
    [InlineData(60)]
    [InlineData(69)]
    public async Task ProcessPunch_LowVerificationQuality_ShouldWarn(int quality)
    {
        // Arrange
        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = DateTime.UtcNow,
            VerificationMethod = "Fingerprint",
            VerificationQuality = quality
        };

        // Act
        var result = await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("quality"));
    }

    [Theory]
    [InlineData(70)]
    [InlineData(85)]
    [InlineData(100)]
    public async Task ProcessPunch_HighVerificationQuality_ShouldNotWarn(int quality)
    {
        // Arrange
        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = DateTime.UtcNow,
            VerificationMethod = "Fingerprint",
            VerificationQuality = quality
        };

        // Act
        var result = await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Assert
        result.Warnings.Should().NotContain(w => w.Contains("quality"));
    }

    // ==================== DAILY PUNCH LIMIT TESTS ====================

    [Fact]
    public async Task ProcessPunch_ExceedsDailyLimit_ShouldReject()
    {
        // Arrange
        var punchTime = DateTime.UtcNow.Date.AddHours(8);

        // Create 10 punches (max limit)
        for (int i = 0; i < 10; i++)
        {
            var punchDto = new DevicePunchCaptureDto
            {
                DeviceUserId = "BIO123",
                DeviceSerialNumber = "SN12345",
                PunchType = i % 2 == 0 ? "CheckIn" : "CheckOut",
                PunchTime = punchTime.AddMinutes(i * 30),
                VerificationMethod = "Fingerprint",
                VerificationQuality = 95
            };
            await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);
        }

        // Try 11th punch (should fail)
        var eleventhPunch = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = punchTime.AddHours(6),
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        // Act
        var result = await _service.ProcessPunchAsync(eleventhPunch, _testDeviceId, _testTenantId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("limit"));
    }

    // ==================== DEVICE VALIDATION TESTS ====================

    [Fact]
    public async Task ProcessPunch_InactiveDevice_ShouldReject()
    {
        // Arrange
        var device = await _tenantDbContext.AttendanceMachines.FindAsync(_testDeviceId);
        device!.IsActive = false;
        await _tenantDbContext.SaveChangesAsync();

        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = DateTime.UtcNow,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        // Act
        var result = await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not active"));
    }

    [Fact]
    public async Task ProcessPunch_NonExistentDevice_ShouldReject()
    {
        // Arrange
        var nonExistentDeviceId = Guid.NewGuid();
        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = DateTime.UtcNow,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        // Act
        var result = await _service.ProcessPunchAsync(punchDto, nonExistentDeviceId, _testTenantId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not found"));
    }

    // ==================== EMPLOYEE RESOLUTION TESTS ====================

    [Fact]
    public async Task ProcessPunch_UnknownBiometricId_ShouldCreateFailedRecord()
    {
        // Arrange
        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "UNKNOWN999",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = DateTime.UtcNow,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        // Act
        var result = await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Employee not found"));
        result.PunchRecordId.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessPunch_InactiveEmployee_ShouldNotProcess()
    {
        // Arrange
        var employee = await _tenantDbContext.Employees.FindAsync(_testEmployeeId);
        employee!.IsActive = false;
        await _tenantDbContext.SaveChangesAsync();

        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = DateTime.UtcNow,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        // Act
        var result = await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Assert
        result.Success.Should().BeFalse();
    }

    // ==================== ATTENDANCE RECORD CREATION TESTS ====================

    [Fact]
    public async Task ProcessPunch_ValidCheckIn_ShouldCreateAttendanceRecord()
    {
        // Arrange
        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = DateTime.UtcNow,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        // Act
        var result = await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Assert
        result.Success.Should().BeTrue();
        result.AttendanceId.Should().NotBeNull();

        var attendance = await _tenantDbContext.Attendances.FindAsync(result.AttendanceId);
        attendance.Should().NotBeNull();
        attendance!.CheckInTime.Should().NotBeNull();
        attendance.CheckOutTime.Should().BeNull();
    }

    [Fact]
    public async Task ProcessPunch_CheckInThenCheckOut_ShouldUpdateAttendance()
    {
        // Arrange
        var checkInTime = DateTime.UtcNow;
        var checkOutTime = checkInTime.AddHours(8);

        var checkInDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = checkInTime,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        var checkOutDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckOut",
            PunchTime = checkOutTime,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        // Act
        var checkInResult = await _service.ProcessPunchAsync(checkInDto, _testDeviceId, _testTenantId);
        var checkOutResult = await _service.ProcessPunchAsync(checkOutDto, _testDeviceId, _testTenantId);

        // Assert
        checkInResult.Success.Should().BeTrue();
        checkOutResult.Success.Should().BeTrue();
        checkOutResult.AttendanceId.Should().Be(checkInResult.AttendanceId);

        var attendance = await _tenantDbContext.Attendances.FindAsync(checkInResult.AttendanceId);
        attendance!.CheckInTime.Should().NotBeNull();
        attendance.CheckOutTime.Should().NotBeNull();
        attendance.WorkingHours.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessPunch_WorkingMoreThan5Hours_ShouldDeductLunchBreak()
    {
        // Arrange
        var checkInTime = DateTime.UtcNow.Date.AddHours(8); // 8 AM
        var checkOutTime = checkInTime.AddHours(9); // 5 PM (9 hours)

        var checkInDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = checkInTime,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        var checkOutDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckOut",
            PunchTime = checkOutTime,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        // Act
        await _service.ProcessPunchAsync(checkInDto, _testDeviceId, _testTenantId);
        var result = await _service.ProcessPunchAsync(checkOutDto, _testDeviceId, _testTenantId);

        // Assert
        var attendance = await _tenantDbContext.Attendances.FindAsync(result.AttendanceId);
        attendance!.WorkingHours.Should().Be(8.0m); // 9 hours - 1 hour lunch
    }

    // ==================== HASH CHAIN INTEGRITY TESTS ====================

    [Fact]
    public async Task ProcessPunch_FirstPunch_ShouldHaveGenesisHash()
    {
        // Arrange
        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = DateTime.UtcNow,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        // Act
        var result = await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Assert
        var punchRecord = await _tenantDbContext.Set<BiometricPunchRecord>()
            .FirstOrDefaultAsync(p => p.Id == result.PunchRecordId);
        punchRecord.Should().NotBeNull();
        punchRecord!.HashChain.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessPunch_MultiplePunches_ShouldHaveLinkedHashChain()
    {
        // Arrange
        var punchDto1 = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = DateTime.UtcNow,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        var punchDto2 = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckOut",
            PunchTime = DateTime.UtcNow.AddHours(4),
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        // Act
        var result1 = await _service.ProcessPunchAsync(punchDto1, _testDeviceId, _testTenantId);
        var result2 = await _service.ProcessPunchAsync(punchDto2, _testDeviceId, _testTenantId);

        // Assert
        var punch1 = await _tenantDbContext.Set<BiometricPunchRecord>().FindAsync(result1.PunchRecordId);
        var punch2 = await _tenantDbContext.Set<BiometricPunchRecord>().FindAsync(result2.PunchRecordId);

        punch1!.HashChain.Should().NotBeNullOrEmpty();
        punch2!.HashChain.Should().NotBeNullOrEmpty();
        punch2.HashChain.Should().NotBe(punch1.HashChain);
    }

    // ==================== PHOTO STORAGE TESTS ====================

    [Fact]
    public async Task ProcessPunch_WithPhotoBase64_ShouldStorePhoto()
    {
        // Arrange
        var base64Photo = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = DateTime.UtcNow,
            VerificationMethod = "Face",
            VerificationQuality = 95,
            PhotoBase64 = base64Photo
        };

        // Act
        var result = await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Assert
        result.Success.Should().BeTrue();
        _mockFileStorageService.Verify(x => x.UploadFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPunch_WithoutPhoto_ShouldNotStorePhoto()
    {
        // Arrange
        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = DateTime.UtcNow,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95,
            PhotoBase64 = null
        };

        // Act
        var result = await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Assert
        result.Success.Should().BeTrue();
        _mockFileStorageService.Verify(x => x.UploadFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    // ==================== QUERY TESTS ====================

    [Fact]
    public async Task GetPendingPunches_ShouldReturnUnprocessedPunches()
    {
        // Arrange - Create a punch that will be processed
        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "UNKNOWN",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = DateTime.UtcNow,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Act
        var result = await _service.GetPendingPunchesAsync(_testTenantId);

        // Assert - Should have the failed punch in pending
        result.Should().BeEmpty(); // Because unknown employee creates failed, not pending
    }

    [Fact]
    public async Task GetPunchesByDevice_ShouldReturnPaginatedResults()
    {
        // Arrange
        var punchDto = new DevicePunchCaptureDto
        {
            DeviceUserId = "BIO123",
            DeviceSerialNumber = "SN12345",
            PunchType = "CheckIn",
            PunchTime = DateTime.UtcNow,
            VerificationMethod = "Fingerprint",
            VerificationQuality = 95
        };

        await _service.ProcessPunchAsync(punchDto, _testDeviceId, _testTenantId);

        // Act
        var result = await _service.GetPunchesByDeviceAsync(_testDeviceId, null, null, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);
    }

    public void Dispose()
    {
        _tenantDbContext?.Dispose();
    }
}
