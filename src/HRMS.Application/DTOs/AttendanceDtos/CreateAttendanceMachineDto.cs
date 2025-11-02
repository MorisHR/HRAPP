using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.AttendanceDtos;

/// <summary>
/// Create new biometric attendance machine
/// </summary>
public class CreateAttendanceMachineDto
{
    [Required]
    [StringLength(100)]
    public string MachineName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string MachineId { get; set; } = string.Empty;

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public Guid? DepartmentId { get; set; }

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [StringLength(100)]
    public string? Model { get; set; }

    [StringLength(50)]
    public string? ZKTecoDeviceId { get; set; }

    public int? Port { get; set; }

    public bool IsActive { get; set; } = true;
}
