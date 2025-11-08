using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Mauritius administrative districts
/// 9 districts based on Mauritius government administrative divisions
/// Master reference data stored in Master Schema
/// </summary>
public class District : IntIdBaseEntity
{
    /// <summary>
    /// District code (e.g., "BL" for Black River, "FL" for Flacq)
    /// </summary>
    public string DistrictCode { get; set; } = string.Empty;

    /// <summary>
    /// Official district name (English)
    /// </summary>
    public string DistrictName { get; set; } = string.Empty;

    /// <summary>
    /// District name in French (official Mauritius language)
    /// </summary>
    public string? DistrictNameFrench { get; set; }

    /// <summary>
    /// Geographic region: North, South, East, West, Central
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Approximate area in square kilometers
    /// </summary>
    public decimal? AreaSqKm { get; set; }

    /// <summary>
    /// Approximate population (for reference)
    /// </summary>
    public int? Population { get; set; }

    /// <summary>
    /// Display order for dropdowns
    /// </summary>
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<Village> Villages { get; set; } = new List<Village>();
}
