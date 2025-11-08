using HRMS.Application.DTOs.BiometricDeviceDtos;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Service interface for managing biometric attendance devices
/// </summary>
public interface IBiometricDeviceService
{
    // CRUD Operations
    Task<Guid> CreateDeviceAsync(CreateBiometricDeviceDto dto, string createdBy);
    Task UpdateDeviceAsync(Guid id, UpdateBiometricDeviceDto dto, string updatedBy);
    Task DeleteDeviceAsync(Guid id, string deletedBy);

    // Retrieval
    Task<BiometricDeviceDto?> GetDeviceByIdAsync(Guid id);
    Task<BiometricDeviceDto?> GetDeviceByCodeAsync(string deviceCode);
    Task<List<BiometricDeviceDto>> GetAllDevicesAsync(bool activeOnly = true);
    Task<List<BiometricDeviceDto>> GetDevicesByLocationAsync(Guid locationId, bool activeOnly = true);
    Task<List<BiometricDeviceDropdownDto>> GetDevicesForDropdownAsync(bool activeOnly = true);

    // Sync Status
    Task<List<DeviceSyncStatusDto>> GetDeviceSyncStatusAsync();
    Task<DeviceSyncStatusDto?> GetDeviceSyncStatusByIdAsync(Guid deviceId);
}
