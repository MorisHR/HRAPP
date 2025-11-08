using HRMS.Application.DTOs.BiometricDeviceDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for managing biometric attendance devices
/// </summary>
public class BiometricDeviceService : IBiometricDeviceService
{
    private readonly TenantDbContext _context;

    public BiometricDeviceService(TenantDbContext context)
    {
        _context = context;
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

        return MapToDeviceDto(device, departmentName, attendanceCount, authorizedEmployeeCount);
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

        return MapToDeviceDto(device, departmentName, attendanceCount, authorizedEmployeeCount);
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

            result.Add(MapToDeviceDto(device, departmentName, attendanceCount, authorizedEmployeeCount));
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

            result.Add(MapToDeviceDto(device, departmentName, attendanceCount, authorizedEmployeeCount));
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

    private BiometricDeviceDto MapToDeviceDto(AttendanceMachine device, string? departmentName, int attendanceCount, int authorizedEmployeeCount)
    {
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
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt,
            CreatedBy = device.CreatedBy,
            UpdatedBy = device.UpdatedBy
        };
    }
}
