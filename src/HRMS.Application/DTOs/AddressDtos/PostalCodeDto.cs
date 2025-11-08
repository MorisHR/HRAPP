namespace HRMS.Application.DTOs.AddressDtos;

/// <summary>
/// Postal code lookup information for API responses
/// Includes complete address hierarchy for quick autocomplete
/// </summary>
public class PostalCodeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string VillageName { get; set; } = string.Empty;
    public string DistrictName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int VillageId { get; set; }
    public int DistrictId { get; set; }
    public string? LocalityType { get; set; }
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}
