using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Biometric attendance machines (ZKTeco devices)
/// Prepared for future integration
/// </summary>
public class AttendanceMachine : BaseEntity
{
    public string MachineName { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Location { get; set; }

    public Guid? DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }

    // ZKTeco specific fields
    public string? ZKTecoDeviceId { get; set; }
    public int? Port { get; set; }
    public DateTime? LastSyncAt { get; set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
