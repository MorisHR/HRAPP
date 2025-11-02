using HRMS.Application.DTOs.AttendanceDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Biometric attendance machine management service
/// Prepared for ZKTeco device integration
/// </summary>
public class AttendanceMachineService : IAttendanceMachineService
{
    private readonly TenantDbContext _context;

    public AttendanceMachineService(TenantDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Create new attendance machine/device
    /// </summary>
    public async Task<Guid> CreateMachineAsync(CreateAttendanceMachineDto dto, string createdBy)
    {
        // Check for duplicate IP address
        if (!string.IsNullOrEmpty(dto.IpAddress))
        {
            var existingMachine = await _context.AttendanceMachines
                .FirstOrDefaultAsync(m => m.IpAddress == dto.IpAddress && !m.IsDeleted);

            if (existingMachine != null)
            {
                throw new InvalidOperationException($"Machine with IP address {dto.IpAddress} already exists");
            }
        }

        // Check for duplicate ZKTeco Device ID if provided
        if (!string.IsNullOrEmpty(dto.ZKTecoDeviceId))
        {
            var existingDevice = await _context.AttendanceMachines
                .FirstOrDefaultAsync(m => m.ZKTecoDeviceId == dto.ZKTecoDeviceId && !m.IsDeleted);

            if (existingDevice != null)
            {
                throw new InvalidOperationException($"Machine with ZKTeco Device ID {dto.ZKTecoDeviceId} already exists");
            }
        }

        var machine = new AttendanceMachine
        {
            Id = Guid.NewGuid(),
            DepartmentId = dto.DepartmentId,
            MachineName = dto.MachineName,
            MachineId = dto.MachineId,
            IpAddress = dto.IpAddress,
            Location = dto.Location,
            Port = dto.Port ?? 4370, // ZKTeco default port
            ZKTecoDeviceId = dto.ZKTecoDeviceId,
            SerialNumber = dto.SerialNumber,
            Model = dto.Model,
            IsActive = dto.IsActive,
            LastSyncAt = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.AttendanceMachines.Add(machine);
        await _context.SaveChangesAsync();

        return machine.Id;
    }

    /// <summary>
    /// Get all machines with optional filtering
    /// </summary>
    public async Task<List<AttendanceMachineDto>> GetMachinesAsync(bool activeOnly = true)
    {
        var query = _context.AttendanceMachines
            .Where(m => !m.IsDeleted);

        if (activeOnly)
        {
            query = query.Where(m => m.IsActive);
        }

        var machines = await query
            .OrderBy(m => m.MachineName)
            .ToListAsync();

        // Get department names if needed
        var departmentIds = machines.Where(m => m.DepartmentId.HasValue).Select(m => m.DepartmentId!.Value).Distinct().ToList();
        var departments = await _context.Departments
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name);

        return machines.Select(m => new AttendanceMachineDto
        {
            Id = m.Id,
            DepartmentId = m.DepartmentId,
            DepartmentName = m.DepartmentId.HasValue && departments.ContainsKey(m.DepartmentId.Value)
                ? departments[m.DepartmentId.Value]
                : null,
            MachineName = m.MachineName,
            MachineId = m.MachineId,
            IpAddress = m.IpAddress,
            Location = m.Location,
            Port = m.Port,
            ZKTecoDeviceId = m.ZKTecoDeviceId,
            SerialNumber = m.SerialNumber,
            Model = m.Model,
            IsActive = m.IsActive,
            LastSyncAt = m.LastSyncAt,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        }).ToList();
    }

    /// <summary>
    /// Get machine by ID
    /// </summary>
    public async Task<AttendanceMachineDto?> GetMachineByIdAsync(Guid id)
    {
        var machine = await _context.AttendanceMachines
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (machine == null)
        {
            return null;
        }

        string? departmentName = null;
        if (machine.DepartmentId.HasValue)
        {
            var department = await _context.Departments.FindAsync(machine.DepartmentId.Value);
            departmentName = department?.Name;
        }

        return new AttendanceMachineDto
        {
            Id = machine.Id,
            DepartmentId = machine.DepartmentId,
            DepartmentName = departmentName,
            MachineName = machine.MachineName,
            MachineId = machine.MachineId,
            IpAddress = machine.IpAddress,
            Location = machine.Location,
            Port = machine.Port,
            ZKTecoDeviceId = machine.ZKTecoDeviceId,
            SerialNumber = machine.SerialNumber,
            Model = machine.Model,
            IsActive = machine.IsActive,
            LastSyncAt = machine.LastSyncAt,
            CreatedAt = machine.CreatedAt,
            UpdatedAt = machine.UpdatedAt
        };
    }

    /// <summary>
    /// Update machine details
    /// </summary>
    public async Task UpdateMachineAsync(Guid id, UpdateAttendanceMachineDto dto, string updatedBy)
    {
        var machine = await _context.AttendanceMachines
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (machine == null)
        {
            throw new KeyNotFoundException($"Machine with ID {id} not found");
        }

        // Check for duplicate IP if changed
        if (!string.IsNullOrEmpty(dto.IpAddress) && dto.IpAddress != machine.IpAddress)
        {
            var existingMachine = await _context.AttendanceMachines
                .FirstOrDefaultAsync(m => m.IpAddress == dto.IpAddress && m.Id != id && !m.IsDeleted);

            if (existingMachine != null)
            {
                throw new InvalidOperationException($"Machine with IP address {dto.IpAddress} already exists");
            }
        }

        // Update fields
        machine.MachineName = dto.MachineName;
        machine.IpAddress = dto.IpAddress;
        machine.Location = dto.Location;
        machine.DepartmentId = dto.DepartmentId;
        machine.Model = dto.Model;
        machine.Port = dto.Port;
        machine.IsActive = dto.IsActive;
        machine.UpdatedAt = DateTime.UtcNow;
        machine.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Delete (soft delete) machine
    /// </summary>
    public async Task DeleteMachineAsync(Guid id)
    {
        var machine = await _context.AttendanceMachines
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (machine == null)
        {
            throw new KeyNotFoundException($"Machine with ID {id} not found");
        }

        // Soft delete
        machine.IsDeleted = true;
        machine.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
