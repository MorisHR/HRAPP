namespace HRMS.Application.DTOs.BiometricDeviceDtos;

/// <summary>
/// Lightweight biometric device data for dropdown lists
/// </summary>
public class BiometricDeviceDropdownDto
{
    public Guid Id { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "ZKTeco";
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string DeviceStatus { get; set; } = "Active";
    public bool IsActive { get; set; }
}
