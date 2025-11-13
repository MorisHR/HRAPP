using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.AttendanceDtos;

/// <summary>
/// ✅ FORTUNE 500: Employee check-out request
/// </summary>
public class CheckOutRequestDto
{
    [Required(ErrorMessage = "Employee ID is required")]
    public Guid EmployeeId { get; set; }
}
