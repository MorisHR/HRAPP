using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.BiometricDeviceDtos;

/// <summary>
/// DTO for testing biometric device connection
/// </summary>
public class TestConnectionDto
{
    [Required]
    [StringLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 4370;

    [StringLength(50)]
    public string ConnectionMethod { get; set; } = "TCP/IP";

    [Range(5, 300)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Optional username for device authentication
    /// </summary>
    [StringLength(50)]
    public string? Username { get; set; }

    /// <summary>
    /// Optional password for device authentication
    /// </summary>
    [StringLength(100)]
    public string? Password { get; set; }
}
