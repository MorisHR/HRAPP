namespace HRMS.Application.DTOs.LocationDtos;

/// <summary>
/// Lightweight location data for dropdown lists
/// </summary>
public class LocationDropdownDto
{
    public Guid Id { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string? LocationType { get; set; }
    public bool IsActive { get; set; }
}
