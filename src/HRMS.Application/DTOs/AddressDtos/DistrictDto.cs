namespace HRMS.Application.DTOs.AddressDtos;

/// <summary>
/// District information for API responses
/// </summary>
public class DistrictDto
{
    public int Id { get; set; }
    public string DistrictCode { get; set; } = string.Empty;
    public string DistrictName { get; set; } = string.Empty;
    public string? DistrictNameFrench { get; set; }
    public string Region { get; set; } = string.Empty;
    public decimal? AreaSqKm { get; set; }
    public int? Population { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
