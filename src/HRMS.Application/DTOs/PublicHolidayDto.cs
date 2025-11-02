using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class PublicHolidayDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Year { get; set; }
    public HolidayType Type { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
