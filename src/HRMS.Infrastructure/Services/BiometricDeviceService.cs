using HRMS.Application.DTOs.BiometricDeviceDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Diagnostics;
using HRMS.Core.Interfaces;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for managing biometric attendance devices
/// </summary>
public class BiometricDeviceService : IBiometricDeviceService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<BiometricDeviceService> _logger;
    private readonly IDeviceApiKeyService _deviceApiKeyService;
    private readonly IDeviceWebhookService _deviceWebhookService;

    public BiometricDeviceService(
        TenantDbContext context,
        ILogger<BiometricDeviceService> logger,
        IDeviceApiKeyService deviceApiKeyService,
        IDeviceWebhookService deviceWebhookService)
    {
        _context = context;
        _logger = logger;
        _deviceApiKeyService = deviceApiKeyService;
        _deviceWebhookService = deviceWebhookService;
    }

    public async Task<Guid> CreateDeviceAsync(CreateBiometricDeviceDto dto, string createdBy)
    {
        // Check for duplicate device code
        var existingDevice = await _context.AttendanceMachines
            .FirstOrDefaultAsync(d => d.DeviceCode == dto.DeviceCode && !d.IsDeleted);

        if (existingDevice != null)
        {
            throw new InvalidOperationException($"Device with code '{dto.DeviceCode}' already exists");
        }

        // Verify location exists
        var locationExists = await _context.Locations
            .AnyAsync(l => l.Id == dto.LocationId && !l.IsDeleted);

        if (!locationExists)
        {
            throw new KeyNotFoundException($"Location with ID {dto.LocationId} not found");
        }

        var device = new AttendanceMachine
        {
            Id = Guid.NewGuid(),
            DeviceCode = dto.DeviceCode,
            MachineName = dto.MachineName,
            MachineId = dto.MachineId,
            DeviceType = dto.DeviceType,
            Model = dto.Model,
            LocationId = dto.LocationId,
            DepartmentId = dto.DepartmentId,
            IpAddress = dto.IpAddress,
            Port = dto.Port,
            MacAddress = dto.MacAddress,
            SerialNumber = dto.SerialNumber,
            FirmwareVersion = dto.FirmwareVersion,
            SyncEnabled = dto.SyncEnabled,
            SyncIntervalMinutes = dto.SyncIntervalMinutes,
            ConnectionMethod = dto.ConnectionMethod,
            ConnectionTimeoutSeconds = dto.ConnectionTimeoutSeconds,
            DeviceConfigJson = dto.DeviceConfigJson,
            DeviceStatus = dto.DeviceStatus,
            IsActive = dto.IsActive,
            OfflineAlertEnabled = dto.OfflineAlertEnabled,
            OfflineThresholdMinutes = dto.OfflineThresholdMinutes,
            ZKTecoDeviceId = dto.ZKTecoDeviceId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.AttendanceMachines.Add(device);
        await _context.SaveChangesAsync();

        return device.Id;
    }

    public async Task UpdateDeviceAsync(Guid id, UpdateBiometricDeviceDto dto, string updatedBy)
    {
        var device = await _context.AttendanceMachines
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (device == null)
        {
            throw new KeyNotFoundException($"Device with ID {id} not found");
        }

        // Check for duplicate device code if changed
        if (dto.DeviceCode != device.DeviceCode)
        {
            var existingDevice = await _context.AttendanceMachines
                .FirstOrDefaultAsync(d => d.DeviceCode == dto.DeviceCode && d.Id != id && !d.IsDeleted);

            if (existingDevice != null)
            {
                throw new InvalidOperationException($"Device with code '{dto.DeviceCode}' already exists");
            }
        }

        // Verify location exists
        var locationExists = await _context.Locations
            .AnyAsync(l => l.Id == dto.LocationId && !l.IsDeleted);

        if (!locationExists)
        {
            throw new KeyNotFoundException($"Location with ID {dto.LocationId} not found");
        }

        // Update fields
        device.DeviceCode = dto.DeviceCode;
        device.MachineName = dto.MachineName;
        device.DeviceType = dto.DeviceType;
        device.Model = dto.Model;
        device.LocationId = dto.LocationId;
        device.DepartmentId = dto.DepartmentId;
        device.IpAddress = dto.IpAddress;
        device.Port = dto.Port;
        device.MacAddress = dto.MacAddress;
        device.SerialNumber = dto.SerialNumber;
        device.FirmwareVersion = dto.FirmwareVersion;
        device.SyncEnabled = dto.SyncEnabled;
        device.SyncIntervalMinutes = dto.SyncIntervalMinutes;
        device.ConnectionMethod = dto.ConnectionMethod;
        device.ConnectionTimeoutSeconds = dto.ConnectionTimeoutSeconds;
        device.DeviceConfigJson = dto.DeviceConfigJson;
        device.DeviceStatus = dto.DeviceStatus;
        device.IsActive = dto.IsActive;
        device.OfflineAlertEnabled = dto.OfflineAlertEnabled;
        device.OfflineThresholdMinutes = dto.OfflineThresholdMinutes;
        device.UpdatedAt = DateTime.UtcNow;
        device.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteDeviceAsync(Guid id, string deletedBy)
    {
        var device = await _context.AttendanceMachines
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (device == null)
        {
            throw new KeyNotFoundException($"Device with ID {id} not found");
        }

        // Soft delete
        device.IsDeleted = true;
        device.DeletedAt = DateTime.UtcNow;
        device.DeletedBy = deletedBy;

        await _context.SaveChangesAsync();
    }

    public async Task<BiometricDeviceDto?> GetDeviceByIdAsync(Guid id)
    {
        var device = await _context.AttendanceMachines
            .Include(d => d.Location)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (device == null)
        {
            return null;
        }

        var attendanceCount = await _context.Attendances
            .CountAsync(a => a.DeviceId == id && !a.IsDeleted);

        var authorizedEmployeeCount = await _context.EmployeeDeviceAccesses
            .CountAsync(a => a.DeviceId == id && a.IsActive && !a.IsDeleted);

        string? departmentName = null;
        if (device.DepartmentId.HasValue)
        {
            var department = await _context.Departments.FindAsync(device.DepartmentId.Value);
            departmentName = department?.Name;
        }

        return await MapToDeviceDtoAsync(device, departmentName, attendanceCount, authorizedEmployeeCount);
    }

    public async Task<BiometricDeviceDto?> GetDeviceByCodeAsync(string deviceCode)
    {
        var device = await _context.AttendanceMachines
            .Include(d => d.Location)
            .FirstOrDefaultAsync(d => d.DeviceCode == deviceCode && !d.IsDeleted);

        if (device == null)
        {
            return null;
        }

        var attendanceCount = await _context.Attendances
            .CountAsync(a => a.DeviceId == device.Id && !a.IsDeleted);

        var authorizedEmployeeCount = await _context.EmployeeDeviceAccesses
            .CountAsync(a => a.DeviceId == device.Id && a.IsActive && !a.IsDeleted);

        string? departmentName = null;
        if (device.DepartmentId.HasValue)
        {
            var department = await _context.Departments.FindAsync(device.DepartmentId.Value);
            departmentName = department?.Name;
        }

        return await MapToDeviceDtoAsync(device, departmentName, attendanceCount, authorizedEmployeeCount);
    }

    public async Task<List<BiometricDeviceDto>> GetAllDevicesAsync(bool activeOnly = true)
    {
        var query = _context.AttendanceMachines
            .Include(d => d.Location)
            .Where(d => !d.IsDeleted);

        if (activeOnly)
        {
            query = query.Where(d => d.IsActive);
        }

        var devices = await query
            .OrderBy(d => d.MachineName)
            .ToListAsync();

        var result = new List<BiometricDeviceDto>();

        foreach (var device in devices)
        {
            var attendanceCount = await _context.Attendances
                .CountAsync(a => a.DeviceId == device.Id && !a.IsDeleted);

            var authorizedEmployeeCount = await _context.EmployeeDeviceAccesses
                .CountAsync(a => a.DeviceId == device.Id && a.IsActive && !a.IsDeleted);

            string? departmentName = null;
            if (device.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(device.DepartmentId.Value);
                departmentName = department?.Name;
            }

            result.Add(await MapToDeviceDtoAsync(device, departmentName, attendanceCount, authorizedEmployeeCount));
        }

        return result;
    }

    public async Task<List<BiometricDeviceDto>> GetDevicesByLocationAsync(Guid locationId, bool activeOnly = true)
    {
        var query = _context.AttendanceMachines
            .Include(d => d.Location)
            .Where(d => d.LocationId == locationId && !d.IsDeleted);

        if (activeOnly)
        {
            query = query.Where(d => d.IsActive);
        }

        var devices = await query
            .OrderBy(d => d.MachineName)
            .ToListAsync();

        var result = new List<BiometricDeviceDto>();

        foreach (var device in devices)
        {
            var attendanceCount = await _context.Attendances
                .CountAsync(a => a.DeviceId == device.Id && !a.IsDeleted);

            var authorizedEmployeeCount = await _context.EmployeeDeviceAccesses
                .CountAsync(a => a.DeviceId == device.Id && a.IsActive && !a.IsDeleted);

            string? departmentName = null;
            if (device.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(device.DepartmentId.Value);
                departmentName = department?.Name;
            }

            result.Add(await MapToDeviceDtoAsync(device, departmentName, attendanceCount, authorizedEmployeeCount));
        }

        return result;
    }

    public async Task<List<BiometricDeviceDropdownDto>> GetDevicesForDropdownAsync(bool activeOnly = true)
    {
        var query = _context.AttendanceMachines
            .Include(d => d.Location)
            .Where(d => !d.IsDeleted);

        if (activeOnly)
        {
            query = query.Where(d => d.IsActive);
        }

        return await query
            .OrderBy(d => d.MachineName)
            .Select(d => new BiometricDeviceDropdownDto
            {
                Id = d.Id,
                DeviceCode = d.DeviceCode,
                MachineName = d.MachineName,
                DeviceType = d.DeviceType,
                LocationId = d.LocationId,
                LocationName = d.Location != null ? d.Location.LocationName : null,
                DeviceStatus = d.DeviceStatus,
                IsActive = d.IsActive
            })
            .ToListAsync();
    }

    public async Task<List<DeviceSyncStatusDto>> GetDeviceSyncStatusAsync()
    {
        var devices = await _context.AttendanceMachines
            .Include(d => d.Location)
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.MachineName)
            .ToListAsync();

        var result = new List<DeviceSyncStatusDto>();

        foreach (var device in devices)
        {
            var syncStatus = await GetDeviceSyncStatusByIdAsync(device.Id);
            if (syncStatus != null)
            {
                result.Add(syncStatus);
            }
        }

        return result;
    }

    public async Task<DeviceSyncStatusDto?> GetDeviceSyncStatusByIdAsync(Guid deviceId)
    {
        var device = await _context.AttendanceMachines
            .Include(d => d.Location)
            .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

        if (device == null)
        {
            return null;
        }

        // Get latest sync log
        var latestSyncLog = await _context.DeviceSyncLogs
            .Where(l => l.DeviceId == deviceId)
            .OrderByDescending(l => l.SyncStartTime)
            .FirstOrDefaultAsync();

        // Calculate minutes since last sync
        int? minutesSinceLastSync = null;
        if (device.LastSyncTime.HasValue)
        {
            minutesSinceLastSync = (int)(DateTime.UtcNow - device.LastSyncTime.Value).TotalMinutes;
        }

        // Check if device is online (synced within threshold)
        bool isOnline = device.LastSyncTime.HasValue &&
                       (DateTime.UtcNow - device.LastSyncTime.Value).TotalMinutes < device.OfflineThresholdMinutes;

        bool isOfflineAlertTriggered = device.OfflineAlertEnabled && !isOnline;

        // Get sync statistics
        var totalSyncCount = await _context.DeviceSyncLogs
            .CountAsync(l => l.DeviceId == deviceId);

        var successfulSyncCount = await _context.DeviceSyncLogs
            .CountAsync(l => l.DeviceId == deviceId && l.SyncStatus == "Success");

        var failedSyncCount = await _context.DeviceSyncLogs
            .CountAsync(l => l.DeviceId == deviceId && l.SyncStatus == "Failed");

        decimal syncSuccessRate = totalSyncCount > 0 ? (decimal)successfulSyncCount / totalSyncCount * 100 : 0;

        return new DeviceSyncStatusDto
        {
            DeviceId = device.Id,
            DeviceCode = device.DeviceCode,
            MachineName = device.MachineName,
            LocationName = device.Location?.LocationName,
            SyncEnabled = device.SyncEnabled,
            SyncIntervalMinutes = device.SyncIntervalMinutes,
            LastSyncTime = device.LastSyncTime,
            LastSyncStatus = device.LastSyncStatus,
            LastSyncRecordCount = device.LastSyncRecordCount,
            MinutesSinceLastSync = minutesSinceLastSync,
            DeviceStatus = device.DeviceStatus,
            IsOnline = isOnline,
            IsOfflineAlertTriggered = isOfflineAlertTriggered,
            LatestSyncLogId = latestSyncLog?.Id,
            LatestSyncStartTime = latestSyncLog?.SyncStartTime,
            LatestSyncEndTime = latestSyncLog?.SyncEndTime,
            LatestSyncDurationSeconds = latestSyncLog?.SyncDurationSeconds,
            LatestSyncError = latestSyncLog?.ErrorMessage,
            TotalSyncCount = totalSyncCount,
            SuccessfulSyncCount = successfulSyncCount,
            FailedSyncCount = failedSyncCount,
            SyncSuccessRate = syncSuccessRate
        };
    }

    private async Task<BiometricDeviceDto> MapToDeviceDtoAsync(
        AttendanceMachine device,
        string? departmentName,
        int attendanceCount,
        int authorizedEmployeeCount)
    {
        // Get API key statistics
        var apiKeys = await _context.DeviceApiKeys
            .Where(k => k.DeviceId == device.Id && !k.IsDeleted)
            .ToListAsync();

        var totalApiKeys = apiKeys.Count;
        var activeApiKeys = apiKeys.Count(k => k.IsActive && (!k.ExpiresAt.HasValue || k.ExpiresAt.Value > DateTime.UtcNow));
        var lastApiKeyUsedAt = apiKeys
            .Where(k => k.LastUsedAt.HasValue)
            .OrderByDescending(k => k.LastUsedAt)
            .Select(k => k.LastUsedAt)
            .FirstOrDefault();

        return new BiometricDeviceDto
        {
            Id = device.Id,
            DeviceCode = device.DeviceCode,
            MachineName = device.MachineName,
            MachineId = device.MachineId,
            DeviceType = device.DeviceType,
            Model = device.Model,
            LocationId = device.LocationId,
            LocationName = device.Location?.LocationName,
            LocationCode = device.Location?.LocationCode,
            DepartmentId = device.DepartmentId,
            DepartmentName = departmentName,
            LegacyLocation = device.LegacyLocation,
            IpAddress = device.IpAddress,
            Port = device.Port,
            MacAddress = device.MacAddress,
            SerialNumber = device.SerialNumber,
            FirmwareVersion = device.FirmwareVersion,
            SyncEnabled = device.SyncEnabled,
            SyncIntervalMinutes = device.SyncIntervalMinutes,
            LastSyncTime = device.LastSyncTime,
            LastSyncStatus = device.LastSyncStatus,
            LastSyncRecordCount = device.LastSyncRecordCount,
            ConnectionMethod = device.ConnectionMethod,
            ConnectionTimeoutSeconds = device.ConnectionTimeoutSeconds,
            DeviceConfigJson = device.DeviceConfigJson,
            DeviceStatus = device.DeviceStatus,
            IsActive = device.IsActive,
            OfflineAlertEnabled = device.OfflineAlertEnabled,
            OfflineThresholdMinutes = device.OfflineThresholdMinutes,
            ZKTecoDeviceId = device.ZKTecoDeviceId,
            LastSyncAt = device.LastSyncAt,
            TotalAttendanceRecords = attendanceCount,
            AuthorizedEmployeeCount = authorizedEmployeeCount,
            TotalApiKeys = totalApiKeys,
            ActiveApiKeys = activeApiKeys,
            LastApiKeyUsedAt = lastApiKeyUsedAt,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt,
            CreatedBy = device.CreatedBy,
            UpdatedBy = device.UpdatedBy
        };
    }

    /// <summary>
    /// Test connection to a biometric device
    /// </summary>
    /// <remarks>
    /// PRODUCTION NOTE: This is a basic TCP/IP connectivity test.
    /// For production deployment with actual devices, integrate with:
    /// - ZKTeco SDK (zkemkeeper.dll) for ZKTeco devices
    /// - Device-specific APIs/SDKs for other manufacturers
    /// - SOAP/REST endpoints if devices support HTTP protocols
    /// </remarks>
    public async Task<ConnectionTestResultDto> TestConnectionAsync(TestConnectionDto dto)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ConnectionTestResultDto
        {
            TestedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Testing connection to device at {IpAddress}:{Port}", dto.IpAddress, dto.Port);

            // Basic TCP/IP connectivity test
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(dto.IpAddress, dto.Port);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(dto.ConnectionTimeoutSeconds));

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Connection timed out
                stopwatch.Stop();
                result.Success = false;
                result.Message = $"Connection timeout after {dto.ConnectionTimeoutSeconds} seconds";
                result.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                result.ErrorDetails = $"Device at {dto.IpAddress}:{dto.Port} did not respond within the timeout period";
                result.Diagnostics = "Possible causes: Device offline, incorrect IP/port, network firewall blocking connection";

                _logger.LogWarning("Connection test failed: Timeout for {IpAddress}:{Port}", dto.IpAddress, dto.Port);
                return result;
            }

            // Check if connection succeeded
            if (connectTask.IsFaulted)
            {
                stopwatch.Stop();
                result.Success = false;
                result.Message = "Connection failed";
                result.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                result.ErrorDetails = connectTask.Exception?.InnerException?.Message ?? "Unknown connection error";
                result.Diagnostics = "Device may be offline or unreachable. Verify IP address, port, and network connectivity.";

                _logger.LogWarning("Connection test failed: {Error}", result.ErrorDetails);
                return result;
            }

            stopwatch.Stop();

            // Connection successful
            result.Success = true;
            result.Message = $"Successfully connected to device at {dto.IpAddress}:{dto.Port}";
            result.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
            result.Diagnostics = $"TCP connection established in {result.ResponseTimeMs}ms";

            // Note: In production with actual device SDKs, you would:
            // 1. Query device info (model, firmware version)
            // 2. Check available records count
            // 3. Verify device authentication if required
            result.DeviceInfo = "Connection established (Full device info requires SDK integration)";

            _logger.LogInformation(
                "Connection test successful: {IpAddress}:{Port} responded in {ResponseTime}ms",
                dto.IpAddress,
                dto.Port,
                result.ResponseTimeMs
            );

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.Message = "Connection test failed with exception";
            result.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
            result.ErrorDetails = ex.Message;
            result.Diagnostics = $"Exception type: {ex.GetType().Name}. Check network configuration and device status.";

            _logger.LogError(ex, "Error testing connection to {IpAddress}:{Port}", dto.IpAddress, dto.Port);
            return result;
        }
    }

    /// <summary>
    /// Manually trigger a device sync operation
    /// </summary>
    /// <remarks>
    /// PRODUCTION NOTE: This method queues a background sync job.
    /// For production deployment:
    /// - Ensure Hangfire is properly configured
    /// - Implement BiometricDeviceSyncJob with actual device SDK integration
    /// - Add job status tracking and monitoring
    /// - Implement retry logic for failed syncs
    /// </remarks>
    public async Task<ManualSyncResultDto> TriggerManualSyncAsync(Guid deviceId)
    {
        var result = new ManualSyncResultDto
        {
            DeviceId = deviceId,
            QueuedAt = DateTime.UtcNow
        };

        try
        {
            // Verify device exists and is active
            var device = await _context.AttendanceMachines
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

            if (device == null)
            {
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");
            }

            result.DeviceName = device.MachineName;

            if (!device.IsActive)
            {
                throw new InvalidOperationException($"Device '{device.MachineName}' is not active and cannot be synced");
            }

            if (!device.SyncEnabled)
            {
                throw new InvalidOperationException($"Sync is disabled for device '{device.MachineName}'");
            }

            // Check if device has required connection info
            if (string.IsNullOrWhiteSpace(device.IpAddress))
            {
                throw new InvalidOperationException($"Device '{device.MachineName}' has no IP address configured");
            }

            // Check if a sync is already in progress
            var recentSyncLog = await _context.DeviceSyncLogs
                .Where(l => l.DeviceId == deviceId && l.SyncStatus == "InProgress")
                .OrderByDescending(l => l.SyncStartTime)
                .FirstOrDefaultAsync();

            if (recentSyncLog != null)
            {
                result.Success = false;
                result.Message = $"A sync is already in progress for device '{device.MachineName}'";
                result.SyncAlreadyInProgress = true;
                result.ErrorDetails = $"Sync started at {recentSyncLog.SyncStartTime:yyyy-MM-dd HH:mm:ss} UTC";

                _logger.LogWarning("Manual sync skipped: Sync already in progress for device {DeviceId}", deviceId);
                return result;
            }

            // PRODUCTION NOTE: Queue Hangfire background job
            // For now, we'll create a sync log entry to simulate job queuing
            // In production, replace this with:
            // var jobId = BackgroundJob.Enqueue<BiometricDeviceSyncJob>(job => job.ExecuteAsync(deviceId));

            var syncLog = new DeviceSyncLog
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                SyncStartTime = DateTime.UtcNow,
                SyncStatus = "Queued",
                SyncMethod = "Manual",
                CreatedAt = DateTime.UtcNow
            };

            _context.DeviceSyncLogs.Add(syncLog);
            await _context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"Sync job queued successfully for device '{device.MachineName}'";
            result.JobId = syncLog.Id.ToString(); // In production, use Hangfire job ID
            result.EstimatedDurationSeconds = 30; // Estimate based on device type/history

            _logger.LogInformation(
                "Manual sync triggered for device {DeviceId} ({DeviceName}). Job ID: {JobId}",
                deviceId,
                device.MachineName,
                result.JobId
            );

            return result;
        }
        catch (KeyNotFoundException ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            result.ErrorDetails = ex.Message;
            throw; // Re-throw to be handled by controller
        }
        catch (InvalidOperationException ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            result.ErrorDetails = ex.Message;
            throw; // Re-throw to be handled by controller
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Failed to trigger device sync";
            result.ErrorDetails = ex.Message;

            _logger.LogError(ex, "Error triggering manual sync for device {DeviceId}", deviceId);
            throw; // Re-throw to be handled by controller
        }
    }

    // ==========================================
    // API KEY MANAGEMENT
    // ==========================================

    /// <summary>
    /// Get all API keys for a specific device
    /// </summary>
    public async Task<List<DeviceApiKeyDto>> GetDeviceApiKeysAsync(Guid deviceId)
    {
        try
        {
            _logger.LogInformation("Retrieving API keys for device {DeviceId}", deviceId);

            // Verify device exists
            var device = await _context.AttendanceMachines
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

            if (device == null)
            {
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");
            }

            // Get all API keys for this device
            var apiKeys = await _deviceApiKeyService.GetDeviceApiKeysAsync(deviceId);

            // Map to DTOs
            var result = apiKeys.Select(k => new DeviceApiKeyDto
            {
                Id = k.Id,
                DeviceId = k.DeviceId,
                Description = k.Description,
                IsActive = k.IsActive,
                ExpiresAt = k.ExpiresAt,
                LastUsedAt = k.LastUsedAt,
                UsageCount = k.UsageCount,
                AllowedIpAddresses = k.AllowedIpAddresses,
                RateLimitPerMinute = k.RateLimitPerMinute,
                CreatedAt = k.CreatedAt,
                CreatedBy = k.CreatedBy,
                UpdatedAt = k.UpdatedAt,
                UpdatedBy = k.UpdatedBy
            }).ToList();

            _logger.LogInformation(
                "Retrieved {Count} API keys for device {DeviceId}",
                result.Count,
                deviceId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API keys for device {DeviceId}", deviceId);
            throw;
        }
    }

    /// <summary>
    /// Generate a new API key for a device
    /// </summary>
    public async Task<GenerateApiKeyResponse> GenerateApiKeyAsync(
        Guid deviceId,
        string description,
        string createdBy,
        DateTime? expiresAt = null,
        string? allowedIpAddresses = null,
        int rateLimitPerMinute = 60)
    {
        try
        {
            _logger.LogInformation(
                "Generating new API key for device {DeviceId} by {CreatedBy}",
                deviceId,
                createdBy);

            // Verify device exists
            var device = await _context.AttendanceMachines
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

            if (device == null)
            {
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");
            }

            // Generate API key using DeviceApiKeyService (NOT DeviceWebhookService)
            var (apiKey, plaintextKey) = await _deviceApiKeyService.GenerateApiKeyAsync(
                deviceId,
                description,
                expiresAt,
                allowedIpAddresses,
                rateLimitPerMinute);

            // Update createdBy field
            apiKey.CreatedBy = createdBy;
            apiKey.UpdatedBy = createdBy;
            await _context.SaveChangesAsync();

            var response = new GenerateApiKeyResponse
            {
                ApiKeyId = apiKey.Id,
                PlaintextKey = plaintextKey,
                Description = apiKey.Description,
                ExpiresAt = apiKey.ExpiresAt,
                IsActive = apiKey.IsActive,
                CreatedAt = apiKey.CreatedAt,
                RateLimitPerMinute = apiKey.RateLimitPerMinute,
                AllowedIpAddresses = apiKey.AllowedIpAddresses
            };

            _logger.LogInformation(
                "Successfully generated API key {ApiKeyId} for device {DeviceId}",
                response.ApiKeyId,
                deviceId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating API key for device {DeviceId}", deviceId);
            throw;
        }
    }

    /// <summary>
    /// Revoke an API key
    /// </summary>
    public async Task RevokeApiKeyAsync(Guid deviceId, Guid apiKeyId, string revokedBy)
    {
        try
        {
            _logger.LogInformation(
                "Revoking API key {ApiKeyId} for device {DeviceId} by {RevokedBy}",
                apiKeyId,
                deviceId,
                revokedBy);

            // Verify device exists
            var device = await _context.AttendanceMachines
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

            if (device == null)
            {
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");
            }

            // Verify API key belongs to this device
            var apiKey = await _deviceApiKeyService.GetApiKeyByIdAsync(apiKeyId);

            if (apiKey == null)
            {
                throw new KeyNotFoundException($"API key with ID {apiKeyId} not found");
            }

            if (apiKey.DeviceId != deviceId)
            {
                throw new InvalidOperationException(
                    $"API key {apiKeyId} does not belong to device {deviceId}");
            }

            // Revoke the API key
            var success = await _deviceApiKeyService.RevokeApiKeyAsync(apiKeyId);

            if (!success)
            {
                throw new InvalidOperationException($"Failed to revoke API key {apiKeyId}");
            }

            _logger.LogInformation(
                "Successfully revoked API key {ApiKeyId} for device {DeviceId}",
                apiKeyId,
                deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error revoking API key {ApiKeyId} for device {DeviceId}",
                apiKeyId,
                deviceId);
            throw;
        }
    }

    /// <summary>
    /// Rotate an API key (revoke old, generate new)
    /// </summary>
    public async Task<GenerateApiKeyResponse> RotateApiKeyAsync(
        Guid deviceId,
        Guid apiKeyId,
        string rotatedBy)
    {
        try
        {
            _logger.LogInformation(
                "Rotating API key {ApiKeyId} for device {DeviceId} by {RotatedBy}",
                apiKeyId,
                deviceId,
                rotatedBy);

            // Verify device exists
            var device = await _context.AttendanceMachines
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

            if (device == null)
            {
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");
            }

            // Verify API key belongs to this device
            var oldApiKey = await _deviceApiKeyService.GetApiKeyByIdAsync(apiKeyId);

            if (oldApiKey == null)
            {
                throw new KeyNotFoundException($"API key with ID {apiKeyId} not found");
            }

            if (oldApiKey.DeviceId != deviceId)
            {
                throw new InvalidOperationException(
                    $"API key {apiKeyId} does not belong to device {deviceId}");
            }

            // Rotate the API key
            var (newApiKey, newPlaintextKey) = await _deviceApiKeyService.RotateApiKeyAsync(apiKeyId);

            var response = new GenerateApiKeyResponse
            {
                ApiKeyId = newApiKey.Id,
                PlaintextKey = newPlaintextKey,
                Description = newApiKey.Description,
                ExpiresAt = newApiKey.ExpiresAt,
                IsActive = newApiKey.IsActive,
                CreatedAt = newApiKey.CreatedAt,
                RateLimitPerMinute = newApiKey.RateLimitPerMinute
            };

            _logger.LogInformation(
                "Successfully rotated API key. Old: {OldApiKeyId}, New: {NewApiKeyId}",
                apiKeyId,
                newApiKey.Id);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error rotating API key {ApiKeyId} for device {DeviceId}",
                apiKeyId,
                deviceId);
            throw;
        }
    }
}
