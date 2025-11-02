using HRMS.Application.DTOs.AttendanceDtos;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Biometric attendance machine management service
/// </summary>
public interface IAttendanceMachineService
{
    Task<Guid> CreateMachineAsync(CreateAttendanceMachineDto dto, string createdBy);
    Task<List<AttendanceMachineDto>> GetMachinesAsync(bool activeOnly = true);
    Task<AttendanceMachineDto?> GetMachineByIdAsync(Guid id);
    Task UpdateMachineAsync(Guid id, UpdateAttendanceMachineDto dto, string updatedBy);
    Task DeleteMachineAsync(Guid id);
}
