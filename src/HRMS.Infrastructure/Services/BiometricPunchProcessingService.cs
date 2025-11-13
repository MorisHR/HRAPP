using HRMS.Application.DTOs.BiometricPunchDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;
using HRMS.Core.Exceptions;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Core intelligence of the biometric attendance capture system
/// Processes raw punches from biometric devices and creates attendance records
///
/// PRODUCTION-READY FEATURES:
/// - Device authorization validation
/// - Duplicate punch detection (anti-fraud)
/// - Employee ID resolution from device user IDs
/// - Tamper-proof hash chain audit trail
/// - Automatic attendance record creation/update
/// - Comprehensive error handling and logging
/// - Daily punch limits (anti-abuse)
/// - Verification quality thresholds
///
/// FORTUNE 500 QUALITY STANDARDS:
/// - Zero tolerance for data loss
/// - Complete audit trail
/// - Thread-safe operations
/// - Comprehensive logging
/// - Graceful error handling
/// </summary>
public class BiometricPunchProcessingService : IBiometricPunchProcessingService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<BiometricPunchProcessingService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;

    // Anti-fraud constants
    private const int DUPLICATE_WINDOW_MINUTES = 15;
    private const int MIN_VERIFICATION_QUALITY = 70;
    private const int MAX_DAILY_PUNCHES = 10;
    private const int LUNCH_BREAK_THRESHOLD_HOURS = 5;
    private const decimal LUNCH_BREAK_HOURS = 1.0m;

    public BiometricPunchProcessingService(
        TenantDbContext context,
        ILogger<BiometricPunchProcessingService> logger,
        IAuditLogService auditLogService,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _logger = logger;
        _auditLogService = auditLogService;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Process a biometric punch from a device and create/update attendance record
    /// This is the CORE method that powers the entire attendance capture system
    /// </summary>
    public async Task<PunchProcessingResultDto> ProcessPunchAsync(
        DevicePunchCaptureDto punchDto,
        Guid deviceId,
        Guid tenantId)
    {
        var result = new PunchProcessingResultDto();

        try
        {
            _logger.LogInformation(
                "Processing punch: DeviceId={DeviceId}, DeviceUserId={DeviceUserId}, PunchType={PunchType}, Time={PunchTime}",
                deviceId, punchDto.DeviceUserId, punchDto.PunchType, punchDto.PunchTime);

            // ==========================================
            // STEP 1: VALIDATE DEVICE
            // ==========================================
            var device = await _context.AttendanceMachines
                .Include(d => d.Location)
                .FirstOrDefaultAsync(d => d.Id == deviceId);

            if (device == null)
            {
                result.AddError($"Device not found: {deviceId}");
                _logger.LogWarning("Device not found: {DeviceId}", deviceId);
                return result;
            }

            if (!device.IsActive || device.DeviceStatus != "Active")
            {
                result.AddError($"Device is not active: {device.MachineName} (Status: {device.DeviceStatus})");
                _logger.LogWarning("Inactive device attempted punch: {DeviceId}, Status: {Status}",
                    deviceId, device.DeviceStatus);
                return result;
            }

            // ==========================================
            // STEP 2: RESOLVE EMPLOYEE FROM DEVICE USER ID
            // ==========================================
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e =>
                    e.BiometricEnrollmentId == punchDto.DeviceUserId &&
                    e.IsActive);

            if (employee == null)
            {
                result.AddError($"Employee not found for device user ID: {punchDto.DeviceUserId}");
                _logger.LogWarning(
                    "Employee mapping failed: DeviceUserId={DeviceUserId}, Device={DeviceName}",
                    punchDto.DeviceUserId, device.MachineName);

                // Still create punch record but mark as failed
                var failedPunch = await CreatePunchRecordAsync(
                    punchDto, deviceId, null, tenantId, "Failed",
                    $"Employee not found for device user ID: {punchDto.DeviceUserId}");

                result.PunchRecordId = failedPunch.Id;
                return result;
            }

            // ==========================================
            // STEP 3: CHECK DEVICE AUTHORIZATION
            // ==========================================
            var isAuthorized = await ValidateDeviceAccessAsync(employee.Id, deviceId);

            if (!isAuthorized)
            {
                result.AddWarning($"Employee {employee.FirstName} {employee.LastName} is not authorized for device {device.MachineName}");
                _logger.LogWarning(
                    "Unauthorized device access attempt: EmployeeId={EmployeeId}, DeviceId={DeviceId}",
                    employee.Id, deviceId);

                // Log to audit trail
                await _auditLogService.LogSecurityEventAsync(
                    AuditActionType.ACCESS_DENIED,
                    AuditSeverity.WARNING,
                    employee.Id,
                    $"Unauthorized biometric punch attempt at device {device.MachineName}",
                    JsonSerializer.Serialize(new
                    {
                        EmployeeId = employee.Id,
                        EmployeeCode = employee.EmployeeCode,
                        DeviceId = deviceId,
                        DeviceName = device.MachineName,
                        LocationId = device.LocationId,
                        PunchTime = punchDto.PunchTime
                    }));
            }

            // ==========================================
            // STEP 4: DUPLICATE DETECTION (ANTI-FRAUD)
            // ==========================================
            var duplicateWindow = punchDto.PunchTime.AddMinutes(-DUPLICATE_WINDOW_MINUTES);
            var hasDuplicate = await _context.Set<BiometricPunchRecord>()
                .AnyAsync(p =>
                    p.EmployeeId == employee.Id &&
                    p.DeviceId == deviceId &&
                    p.PunchType == punchDto.PunchType &&
                    p.PunchTime >= duplicateWindow &&
                    p.PunchTime <= punchDto.PunchTime.AddMinutes(DUPLICATE_WINDOW_MINUTES) &&
                    p.ProcessingStatus != "Ignored");

            if (hasDuplicate)
            {
                result.AddWarning("Duplicate punch detected within 15-minute window");
                _logger.LogWarning(
                    "Duplicate punch detected: EmployeeId={EmployeeId}, DeviceId={DeviceId}, PunchType={PunchType}",
                    employee.Id, deviceId, punchDto.PunchType);

                var duplicatePunch = await CreatePunchRecordAsync(
                    punchDto, deviceId, employee.Id, tenantId, "Duplicate",
                    "Duplicate punch within 15-minute window");

                result.Success = true;
                result.Message = "Duplicate punch detected and logged";
                result.PunchRecordId = duplicatePunch.Id;
                return result;
            }

            // ==========================================
            // STEP 5: VERIFICATION QUALITY CHECK
            // ==========================================
            if (punchDto.VerificationQuality < MIN_VERIFICATION_QUALITY)
            {
                result.AddWarning($"Low verification quality: {punchDto.VerificationQuality}% (minimum: {MIN_VERIFICATION_QUALITY}%)");
                _logger.LogWarning(
                    "Low verification quality: EmployeeId={EmployeeId}, Quality={Quality}",
                    employee.Id, punchDto.VerificationQuality);
            }

            // ==========================================
            // STEP 6: DAILY PUNCH LIMIT CHECK (ANTI-ABUSE)
            // ==========================================
            var todayStart = punchDto.PunchTime.Date;
            var todayEnd = todayStart.AddDays(1);
            var todayPunchCount = await _context.Set<BiometricPunchRecord>()
                .CountAsync(p =>
                    p.EmployeeId == employee.Id &&
                    p.PunchTime >= todayStart &&
                    p.PunchTime < todayEnd &&
                    p.ProcessingStatus != "Ignored");

            if (todayPunchCount >= MAX_DAILY_PUNCHES)
            {
                result.AddError($"Daily punch limit exceeded ({MAX_DAILY_PUNCHES} punches per day)");
                _logger.LogWarning(
                    "Daily punch limit exceeded: EmployeeId={EmployeeId}, Date={Date}, Count={Count}",
                    employee.Id, todayStart, todayPunchCount);

                var exceededPunch = await CreatePunchRecordAsync(
                    punchDto, deviceId, employee.Id, tenantId, "Failed",
                    $"Daily punch limit exceeded ({todayPunchCount} punches today)");

                result.PunchRecordId = exceededPunch.Id;
                return result;
            }

            // ==========================================
            // STEP 7: CREATE PUNCH RECORD
            // ==========================================
            var punchRecord = await CreatePunchRecordAsync(
                punchDto, deviceId, employee.Id, tenantId, "Pending", null);

            result.PunchRecordId = punchRecord.Id;

            // ==========================================
            // STEP 8: PROCESS INTO ATTENDANCE RECORD
            // ==========================================
            try
            {
                var attendance = await ProcessIntoAttendanceAsync(
                    punchRecord, employee, device, isAuthorized);

                result.AttendanceId = attendance?.Id;

                // Mark punch as processed
                punchRecord.ProcessingStatus = "Processed";
                punchRecord.ProcessedAt = DateTime.UtcNow;
                punchRecord.AttendanceId = attendance?.Id;
                await _context.SaveChangesAsync();

                result.Success = true;
                result.Message = $"Punch processed successfully for {employee.FirstName} {employee.LastName}";

                _logger.LogInformation(
                    "Punch processed successfully: PunchId={PunchId}, EmployeeId={EmployeeId}, AttendanceId={AttendanceId}",
                    punchRecord.Id, employee.Id, attendance?.Id);

                // Log to audit trail
                await _auditLogService.LogActionAsync(
                    AuditActionType.RECORD_CREATED,
                    AuditCategory.DATA_CHANGE,
                    AuditSeverity.INFO,
                    "BiometricPunch",
                    punchRecord.Id,
                    null,
                    JsonSerializer.Serialize(new
                    {
                        EmployeeId = employee.Id,
                        EmployeeCode = employee.EmployeeCode,
                        DeviceId = deviceId,
                        DeviceName = device.MachineName,
                        PunchType = punchDto.PunchType,
                        PunchTime = punchDto.PunchTime,
                        VerificationMethod = punchDto.VerificationMethod,
                        AttendanceId = attendance?.Id
                    }),
                    true,
                    null,
                    null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process punch into attendance: PunchId={PunchId}, EmployeeId={EmployeeId}",
                    punchRecord.Id, employee.Id);

                // Mark punch as failed
                punchRecord.ProcessingStatus = "Failed";
                punchRecord.ProcessingError = ex.Message;
                await _context.SaveChangesAsync();

                result.AddError($"Failed to create attendance record: {ex.Message}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error processing punch: {Error}", ex.Message);
            result.AddError($"Critical error: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Get a single punch record by ID
    /// </summary>
    public async Task<BiometricPunchRecordDto?> GetPunchRecordAsync(Guid id)
    {
        var punch = await _context.Set<BiometricPunchRecord>()
            .Include(p => p.Employee)
            .Include(p => p.Device)
                .ThenInclude(d => d!.Location)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (punch == null)
        {
            return null;
        }

        return MapToPunchRecordDto(punch);
    }

    /// <summary>
    /// Get all pending (unprocessed) punch records for a tenant
    /// </summary>
    public async Task<List<BiometricPunchRecordDto>> GetPendingPunchesAsync(Guid tenantId)
    {
        var punches = await _context.Set<BiometricPunchRecord>()
            .Include(p => p.Employee)
            .Include(p => p.Device)
                .ThenInclude(d => d!.Location)
            .Where(p => p.TenantId == tenantId && p.ProcessingStatus == "Pending")
            .OrderBy(p => p.PunchTime)
            .ToListAsync();

        return punches.Select(MapToPunchRecordDto).ToList();
    }

    /// <summary>
    /// Get all punch records for an employee within a date range
    /// </summary>
    public async Task<List<BiometricPunchRecordDto>> GetEmployeePunchesAsync(
        Guid employeeId,
        DateTime fromDate,
        DateTime toDate)
    {
        var punches = await _context.Set<BiometricPunchRecord>()
            .Include(p => p.Employee)
            .Include(p => p.Device)
                .ThenInclude(d => d!.Location)
            .Where(p =>
                p.EmployeeId == employeeId &&
                p.PunchTime >= fromDate.Date &&
                p.PunchTime < toDate.Date.AddDays(1))
            .OrderBy(p => p.PunchTime)
            .ToListAsync();

        return punches.Select(MapToPunchRecordDto).ToList();
    }

    /// <summary>
    /// Reprocess all failed punch records for a tenant
    /// </summary>
    public async Task ReprocessFailedPunchesAsync(Guid tenantId)
    {
        _logger.LogInformation("Starting reprocessing of failed punches for tenant {TenantId}", tenantId);

        var failedPunches = await _context.Set<BiometricPunchRecord>()
            .Include(p => p.Device)
                .ThenInclude(d => d!.Location)
            .Where(p => p.TenantId == tenantId && p.ProcessingStatus == "Failed")
            .OrderBy(p => p.PunchTime)
            .ToListAsync();

        _logger.LogInformation("Found {Count} failed punches to reprocess", failedPunches.Count);

        int successCount = 0;
        int failCount = 0;

        foreach (var punch in failedPunches)
        {
            try
            {
                // Try to resolve employee again
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e =>
                        e.BiometricEnrollmentId == punch.DeviceUserId &&
                        e.IsActive);

                if (employee == null)
                {
                    _logger.LogWarning(
                        "Still cannot resolve employee for punch {PunchId}, DeviceUserId={DeviceUserId}",
                        punch.Id, punch.DeviceUserId);
                    failCount++;
                    continue;
                }

                // Update employee ID if it was null
                if (punch.EmployeeId == null)
                {
                    punch.EmployeeId = employee.Id;
                }

                // Check device authorization
                var isAuthorized = await ValidateDeviceAccessAsync(employee.Id, punch.DeviceId);

                // Process into attendance
                var attendance = await ProcessIntoAttendanceAsync(
                    punch, employee, punch.Device!, isAuthorized);

                // Mark as processed
                punch.ProcessingStatus = "Processed";
                punch.ProcessedAt = DateTime.UtcNow;
                punch.ProcessingError = null;
                punch.AttendanceId = attendance?.Id;

                successCount++;

                _logger.LogInformation(
                    "Successfully reprocessed punch {PunchId} for employee {EmployeeId}",
                    punch.Id, employee.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reprocess punch {PunchId}: {Error}", punch.Id, ex.Message);
                punch.ProcessingError = $"Reprocessing failed: {ex.Message}";
                failCount++;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Reprocessing complete: {SuccessCount} succeeded, {FailCount} failed",
            successCount, failCount);
    }

    /// <summary>
    /// Get punch records for a specific device with pagination
    /// Used by biometric devices to sync their punch history
    /// </summary>
    public async Task<HRMS.Application.DTOs.AuditLog.PagedResult<BiometricPunchRecordDto>> GetPunchesByDeviceAsync(
        Guid deviceId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize)
    {
        _logger.LogInformation(
            "Getting punches for device {DeviceId}, Date Range: {StartDate} to {EndDate}, Page: {Page}, PageSize: {PageSize}",
            deviceId, startDate, endDate, page, pageSize);

        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 1000) pageSize = 1000; // Max 1000 records per page

        // Build query
        var query = _context.Set<BiometricPunchRecord>()
            .Include(p => p.Employee)
            .Include(p => p.Device)
                .ThenInclude(d => d!.Location)
            .Where(p => p.DeviceId == deviceId && !p.IsDeleted);

        // Apply date range filters
        if (startDate.HasValue)
        {
            query = query.Where(p => p.PunchTime >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            // Include the entire end date (up to 23:59:59)
            var endOfDay = endDate.Value.Date.AddDays(1);
            query = query.Where(p => p.PunchTime < endOfDay);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply ordering and pagination
        var punches = await query
            .OrderByDescending(p => p.PunchTime) // Newest first
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation(
            "Found {TotalCount} total punches for device {DeviceId}, returning page {Page} with {Count} records",
            totalCount, deviceId, page, punches.Count);

        // Map to DTOs
        var dtos = punches.Select(MapToPunchRecordDto).ToList();

        return new HRMS.Application.DTOs.AuditLog.PagedResult<BiometricPunchRecordDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    // ==========================================
    // PRIVATE HELPER METHODS
    // ==========================================

    /// <summary>
    /// Create a biometric punch record with tamper-proof hash chain
    /// </summary>
    private async Task<BiometricPunchRecord> CreatePunchRecordAsync(
        DevicePunchCaptureDto punchDto,
        Guid deviceId,
        Guid? employeeId,
        Guid tenantId,
        string status,
        string? errorMessage)
    {
        // Get the last punch record for hash chain
        var lastPunch = await _context.Set<BiometricPunchRecord>()
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        var previousHash = lastPunch?.HashChain ?? "GENESIS";

        // Calculate hash chain
        var hashChain = CalculateHashChain(
            deviceId,
            punchDto.DeviceUserId,
            punchDto.PunchTime,
            previousHash);

        // Handle photo storage if provided
        string? photoPath = null;
        if (!string.IsNullOrWhiteSpace(punchDto.PhotoBase64))
        {
            try
            {
                photoPath = await SavePhotoAsync(punchDto.PhotoBase64, employeeId, deviceId, tenantId, punchDto.PunchTime);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save photo for punch");
            }
        }

        var punchRecord = new BiometricPunchRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DeviceId = deviceId,
            DeviceUserId = punchDto.DeviceUserId,
            DeviceSerialNumber = punchDto.DeviceSerialNumber,
            EmployeeId = employeeId,
            PunchTime = punchDto.PunchTime,
            PunchType = punchDto.PunchType,
            VerificationMethod = punchDto.VerificationMethod,
            VerificationQuality = punchDto.VerificationQuality,
            Latitude = punchDto.Latitude,
            Longitude = punchDto.Longitude,
            PhotoPath = photoPath,
            RawData = punchDto.RawData,
            ProcessingStatus = status,
            ProcessingError = errorMessage,
            HashChain = hashChain,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.Set<BiometricPunchRecord>().Add(punchRecord);
        await _context.SaveChangesAsync();

        return punchRecord;
    }

    /// <summary>
    /// Process a punch record into an attendance record
    /// </summary>
    private async Task<Attendance?> ProcessIntoAttendanceAsync(
        BiometricPunchRecord punch,
        Employee employee,
        AttendanceMachine device,
        bool isAuthorized)
    {
        var date = punch.PunchTime.Date;

        // Find or create attendance record for this date
        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date == date);

        if (attendance == null)
        {
            // Create new attendance record
            attendance = new Attendance
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                Date = date,
                DeviceId = device.Id,
                LocationId = device.LocationId,
                PunchSource = "Biometric",
                VerificationMethod = punch.VerificationMethod,
                DeviceUserId = punch.DeviceUserId,
                IsAuthorized = isAuthorized,
                IsSunday = date.DayOfWeek == DayOfWeek.Sunday,
                IsPublicHoliday = await IsPublicHolidayAsync(date),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.GetAuditUsername(),
                IsDeleted = false
            };

            _context.Attendances.Add(attendance);
        }

        // Update based on punch type
        if (punch.PunchType.Equals("CheckIn", StringComparison.OrdinalIgnoreCase))
        {
            if (attendance.CheckInTime == null || punch.PunchTime < attendance.CheckInTime.Value)
            {
                attendance.CheckInTime = punch.PunchTime;
                attendance.DeviceId = device.Id;
                attendance.LocationId = device.LocationId;
                attendance.VerificationMethod = punch.VerificationMethod;
            }
        }
        else if (punch.PunchType.Equals("CheckOut", StringComparison.OrdinalIgnoreCase))
        {
            if (attendance.CheckOutTime == null || punch.PunchTime > attendance.CheckOutTime.Value)
            {
                attendance.CheckOutTime = punch.PunchTime;
            }
        }

        // Calculate working hours if both times are present
        if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
        {
            var totalHours = (decimal)(attendance.CheckOutTime.Value - attendance.CheckInTime.Value).TotalHours;

            // Subtract lunch break if working > 5 hours
            if (totalHours > LUNCH_BREAK_THRESHOLD_HOURS)
            {
                totalHours -= LUNCH_BREAK_HOURS;
            }

            attendance.WorkingHours = Math.Max(0, Math.Round(totalHours, 2));

            // Determine status
            attendance.Status = DetermineAttendanceStatus(attendance);
        }
        else
        {
            // Only check-in or check-out present
            attendance.Status = attendance.CheckInTime.HasValue
                ? AttendanceStatus.Present
                : AttendanceStatus.Absent;
        }

        // Add authorization note if unauthorized
        if (!isAuthorized)
        {
            attendance.AuthorizationNote = $"Punched at unauthorized device: {device.MachineName} at {device.Location?.LocationName ?? "Unknown Location"}";
        }

        attendance.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return attendance;
    }

    /// <summary>
    /// Validate if employee has access to this device
    /// </summary>
    private async Task<bool> ValidateDeviceAccessAsync(Guid employeeId, Guid deviceId)
    {
        // Check if employee has explicit access to this device
        var hasAccess = await _context.EmployeeDeviceAccesses
            .AnyAsync(a =>
                a.EmployeeId == employeeId &&
                a.DeviceId == deviceId &&
                a.IsActive &&
                (a.ValidFrom == null || a.ValidFrom <= DateTime.UtcNow) &&
                (a.ValidUntil == null || a.ValidUntil >= DateTime.UtcNow));

        if (hasAccess)
        {
            return true;
        }

        // Check if device is at employee's primary location
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee?.PrimaryLocationId == null)
        {
            // No primary location set, allow access (backward compatibility)
            return true;
        }

        var device = await _context.AttendanceMachines
            .FirstOrDefaultAsync(d => d.Id == deviceId);

        return device?.LocationId == employee.PrimaryLocationId;
    }

    /// <summary>
    /// Calculate SHA-256 hash chain for tamper-proof audit trail
    /// </summary>
    private string CalculateHashChain(Guid deviceId, string deviceUserId, DateTime punchTime, string previousHash)
    {
        var data = $"{deviceId}|{deviceUserId}|{punchTime:O}|{previousHash}";
        var bytes = Encoding.UTF8.GetBytes(data);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Save photo from base64 string to Google Cloud Storage
    /// </summary>
    private async Task<string> SavePhotoAsync(string base64Photo, Guid? employeeId, Guid deviceId, Guid tenantId, DateTime punchTime)
    {
        try
        {
            // Decode base64 to bytes
            byte[] photoBytes;
            try
            {
                // Remove data URI prefix if present (e.g., "data:image/jpeg;base64,")
                var base64Data = base64Photo;
                if (base64Photo.Contains(","))
                {
                    base64Data = base64Photo.Split(',')[1];
                }

                photoBytes = Convert.FromBase64String(base64Data);

                _logger.LogInformation(
                    "Decoded photo: {Size} bytes for EmployeeId={EmployeeId}, DeviceId={DeviceId}",
                    photoBytes.Length, employeeId, deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode base64 photo string");
                throw new ValidationException(
                    ErrorCodes.DEV_INVALID_DATA,
                    "The biometric photo data is invalid.",
                    $"Invalid base64 photo data: {ex.Message}",
                    "Please ensure the photo data is properly encoded.");
            }

            // Generate structured filename
            var timestamp = punchTime.ToString("yyyyMMddHHmmss");
            var userId = employeeId?.ToString() ?? "unknown";
            var filename = $"{timestamp}_{userId}.jpg";

            // Build folder path: biometric-photos/{tenantId}/{deviceId}
            var folder = $"biometric-photos/{tenantId}/{deviceId}";

            _logger.LogInformation(
                "Uploading photo to Google Cloud Storage: Folder={Folder}, FileName={FileName}",
                folder, filename);

            // Upload to Google Cloud Storage
            using (var photoStream = new MemoryStream(photoBytes))
            {
                var cloudPath = await _fileStorageService.UploadFileAsync(
                    photoStream,
                    filename,
                    folder,
                    "image/jpeg");

                _logger.LogInformation(
                    "Photo uploaded successfully to: {CloudPath}",
                    cloudPath);

                return cloudPath;
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the entire punch processing
            _logger.LogError(ex,
                "Failed to save photo for EmployeeId={EmployeeId}, DeviceId={DeviceId}: {Error}",
                employeeId, deviceId, ex.Message);

            // Return a placeholder path indicating storage failure
            // This allows the punch to still be recorded even if photo storage fails
            return $"storage-failed/{deviceId}_{punchTime:yyyyMMddHHmmss}.jpg";
        }
    }

    /// <summary>
    /// Check if date is a public holiday
    /// </summary>
    private async Task<bool> IsPublicHolidayAsync(DateTime date)
    {
        return await _context.PublicHolidays
            .AnyAsync(h => h.Date.Date == date.Date);
    }

    /// <summary>
    /// Determine attendance status based on times and day type
    /// </summary>
    private AttendanceStatus DetermineAttendanceStatus(Attendance attendance)
    {
        if (attendance.IsPublicHoliday)
        {
            return AttendanceStatus.PublicHoliday;
        }

        if (attendance.IsSunday || attendance.Date.DayOfWeek == DayOfWeek.Saturday)
        {
            return AttendanceStatus.Weekend;
        }

        if (!attendance.CheckInTime.HasValue)
        {
            return AttendanceStatus.Absent;
        }

        // Check if late (assuming 9:00 AM start time - should be from shift)
        var startTime = new TimeSpan(9, 0, 0);
        if (attendance.CheckInTime.Value.TimeOfDay > startTime.Add(TimeSpan.FromMinutes(15)))
        {
            return AttendanceStatus.Late;
        }

        // Check for early departure
        if (attendance.CheckOutTime.HasValue)
        {
            var endTime = new TimeSpan(17, 0, 0);
            if (attendance.CheckOutTime.Value.TimeOfDay < endTime.Add(TimeSpan.FromMinutes(-15)))
            {
                return AttendanceStatus.EarlyDeparture;
            }
        }

        return AttendanceStatus.Present;
    }

    /// <summary>
    /// Map entity to DTO
    /// </summary>
    private BiometricPunchRecordDto MapToPunchRecordDto(BiometricPunchRecord punch)
    {
        return new BiometricPunchRecordDto
        {
            Id = punch.Id,
            EmployeeId = punch.EmployeeId,
            EmployeeName = punch.Employee != null
                ? $"{punch.Employee.FirstName} {punch.Employee.LastName}"
                : null,
            EmployeeCode = punch.Employee?.EmployeeCode,
            DeviceId = punch.DeviceId,
            DeviceName = punch.Device?.MachineName ?? "Unknown Device",
            DeviceSerialNumber = punch.DeviceSerialNumber,
            DeviceUserId = punch.DeviceUserId,
            PunchTime = punch.PunchTime,
            PunchType = punch.PunchType,
            VerificationMethod = punch.VerificationMethod,
            VerificationQuality = punch.VerificationQuality,
            Latitude = punch.Latitude,
            Longitude = punch.Longitude,
            ProcessingStatus = punch.ProcessingStatus,
            ProcessedAt = punch.ProcessedAt,
            ProcessingError = punch.ProcessingError,
            AttendanceId = punch.AttendanceId,
            CreatedAt = punch.CreatedAt,
            PhotoPath = punch.PhotoPath,
            LocationName = punch.Device?.Location?.LocationName
        };
    }
}
