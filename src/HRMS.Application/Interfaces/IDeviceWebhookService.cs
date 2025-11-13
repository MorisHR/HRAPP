using HRMS.Application.DTOs.DeviceWebhookDtos;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Service interface for processing device webhook data
/// Handles push-based data from biometric devices
/// </summary>
public interface IDeviceWebhookService
{
    /// <summary>
    /// Process attendance data pushed from a biometric device
    /// </summary>
    /// <param name="dto">Attendance data from device</param>
    /// <returns>Processing result with success/failure details</returns>
    Task<DeviceWebhookResponseDto> ProcessAttendanceDataAsync(DevicePushAttendanceDto dto);

    /// <summary>
    /// Process heartbeat/status update from a biometric device
    /// </summary>
    /// <param name="dto">Device heartbeat data</param>
    Task ProcessHeartbeatAsync(DeviceHeartbeatDto dto);

    /// <summary>
    /// Validate device API key and get device information
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="apiKey">API key to validate</param>
    /// <returns>Tuple of (IsValid, TenantId, DeviceGuid)</returns>
    Task<(bool IsValid, Guid? TenantId, Guid? DeviceGuid)> ValidateDeviceApiKeyAsync(string deviceId, string apiKey);

    /// <summary>
    /// Generate a new API key for a device
    /// </summary>
    /// <param name="deviceGuid">Device GUID</param>
    /// <param name="updatedBy">User generating the key</param>
    /// <returns>Generated API key</returns>
    Task<string> GenerateDeviceApiKeyAsync(Guid deviceGuid, string updatedBy);
}
