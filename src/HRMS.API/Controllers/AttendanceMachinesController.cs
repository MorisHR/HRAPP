using HRMS.Application.DTOs.AttendanceDtos;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Biometric attendance machine management
/// Prepared for ZKTeco device integration
/// </summary>
[ApiController]
[Route("api/attendance-machines")]
[Authorize(Roles = "HR,Admin")]
public class AttendanceMachinesController : ControllerBase
{
    private readonly IAttendanceMachineService _machineService;
    private readonly ILogger<AttendanceMachinesController> _logger;

    public AttendanceMachinesController(
        IAttendanceMachineService machineService,
        ILogger<AttendanceMachinesController> logger)
    {
        _machineService = machineService;
        _logger = logger;
    }

    /// <summary>
    /// Create new attendance machine/device
    /// </summary>
    /// <remarks>
    /// POST /api/attendance-machines
    /// {
    ///   "departmentId": "guid",
    ///   "locationId": "guid",
    ///   "machineName": "Main Entrance - ZKTeco F18",
    ///   "machineType": "ZKTeco",
    ///   "ipAddress": "192.168.1.100",
    ///   "port": 4370,
    ///   "zkTecoDeviceId": "12345",
    ///   "serialNumber": "SN123456789",
    ///   "isActive": true
    /// }
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> CreateMachine([FromBody] CreateAttendanceMachineDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token");

            var machineId = await _machineService.CreateMachineAsync(dto, userId);

            return CreatedAtAction(
                nameof(GetMachineById),
                new { id = machineId },
                new { id = machineId, message = "Attendance machine created successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating machine");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attendance machine");
            return StatusCode(500, new { error = "An error occurred while creating attendance machine" });
        }
    }

    /// <summary>
    /// Get all machines (active only by default)
    /// </summary>
    /// <remarks>
    /// GET /api/attendance-machines?activeOnly=true
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = "HR,Manager,Admin")]
    public async Task<IActionResult> GetMachines([FromQuery] bool activeOnly = true)
    {
        try
        {
            var machines = await _machineService.GetMachinesAsync(activeOnly);

            return Ok(new
            {
                total = machines.Count,
                data = machines
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance machines");
            return StatusCode(500, new { error = "An error occurred while retrieving attendance machines" });
        }
    }

    /// <summary>
    /// Get machine by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "HR,Manager,Admin")]
    public async Task<IActionResult> GetMachineById(Guid id)
    {
        try
        {
            var machine = await _machineService.GetMachineByIdAsync(id);

            if (machine == null)
            {
                return NotFound(new { error = "Attendance machine not found" });
            }

            return Ok(machine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance machine {MachineId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving attendance machine" });
        }
    }

    /// <summary>
    /// Update machine details
    /// </summary>
    /// <remarks>
    /// PUT /api/attendance-machines/{id}
    /// {
    ///   "machineName": "Updated Name",
    ///   "isActive": false
    /// }
    /// </remarks>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateMachine(Guid id, [FromBody] UpdateAttendanceMachineDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token");

            await _machineService.UpdateMachineAsync(id, dto, userId);

            return Ok(new { message = "Attendance machine updated successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating machine");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attendance machine {MachineId}", id);
            return StatusCode(500, new { error = "An error occurred while updating attendance machine" });
        }
    }

    /// <summary>
    /// Delete machine (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMachine(Guid id)
    {
        try
        {
            await _machineService.DeleteMachineAsync(id);

            return Ok(new { message = "Attendance machine deleted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attendance machine {MachineId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting attendance machine" });
        }
    }
}
