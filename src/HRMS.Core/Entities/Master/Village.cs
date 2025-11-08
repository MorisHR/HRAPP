using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Mauritius villages, towns, and localities
/// Approximately 300-400 populated places across 9 districts
/// Master reference data stored in Master Schema
/// </summary>
public class Village : IntIdBaseEntity
{
    /// <summary>
    /// Village/Town code (e.g., "PLOU" for Port Louis, "CURE" for Curepipe)
    /// </summary>
    public string VillageCode { get; set; } = string.Empty;

    /// <summary>
    /// Official village/town name (English)
    /// </summary>
    public string VillageName { get; set; } = string.Empty;

    /// <summary>
    /// Village name in French (official Mauritius language)
    /// </summary>
    public string? VillageNameFrench { get; set; }

    /// <summary>
    /// Primary postal code for this village
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to District
    /// </summary>
    public int DistrictId { get; set; }

    /// <summary>
    /// Locality type: City, Town, Village, Hamlet
    /// </summary>
    public string? LocalityType { get; set; }

    /// <summary>
    /// Approximate latitude for mapping (optional)
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Approximate longitude for mapping (optional)
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Display order within district
    /// </summary>
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual District? District { get; set; }
}
