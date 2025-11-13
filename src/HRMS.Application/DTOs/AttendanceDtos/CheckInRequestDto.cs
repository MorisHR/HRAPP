using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.AttendanceDtos;

/// <summary>
/// ✅ FORTUNE 500: Employee check-in request
/// </summary>
public class CheckInRequestDto
{
    [Required(ErrorMessage = "Employee ID is required")]
    public Guid EmployeeId { get; set; }
}
