namespace HRMS.Application.DTOs.AttendanceDtos;

/// <summary>
/// Attendance machine details
/// </summary>
public class AttendanceMachineDto
{
    public Guid Id { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Location { get; set; }

    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    public bool IsActive { get; set; }
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }

    public string? ZKTecoDeviceId { get; set; }
    public int? Port { get; set; }
    public DateTime? LastSyncAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
