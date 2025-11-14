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

    // Device Connection & Sync Operations
    /// <summary>
    /// Test connection to a biometric device
    /// </summary>
    Task<ConnectionTestResultDto> TestConnectionAsync(TestConnectionDto dto);

    /// <summary>
    /// Manually trigger a device sync operation
    /// </summary>
    Task<ManualSyncResultDto> TriggerManualSyncAsync(Guid deviceId);

    // API Key Management
    /// <summary>
    /// Get all API keys for a specific device
    /// </summary>
    Task<List<DeviceApiKeyDto>> GetDeviceApiKeysAsync(Guid deviceId);

    /// <summary>
    /// Generate a new API key for a device
    /// Returns the plaintext key ONLY ONCE - it cannot be retrieved later
    /// </summary>
    Task<GenerateApiKeyResponse> GenerateApiKeyAsync(
        Guid deviceId,
        string description,
        string createdBy,
        DateTime? expiresAt = null,
        string? allowedIpAddresses = null,
        int rateLimitPerMinute = 60);

    /// <summary>
    /// Revoke an API key (mark as inactive)
    /// </summary>
    Task RevokeApiKeyAsync(Guid deviceId, Guid apiKeyId, string revokedBy);

    /// <summary>
    /// Rotate an API key (revoke old, generate new)
    /// </summary>
    Task<GenerateApiKeyResponse> RotateApiKeyAsync(Guid deviceId, Guid apiKeyId, string rotatedBy);
}
