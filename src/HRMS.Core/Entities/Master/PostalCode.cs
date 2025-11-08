using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Mauritius postal codes lookup table
/// Optimized for fast postal code lookups with denormalized data
/// Contains complete address hierarchy for quick autocomplete
/// Master reference data stored in Master Schema
/// </summary>
public class PostalCode : IntIdBaseEntity
{
    /// <summary>
    /// Postal code (5-digit format)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Village/Town name (denormalized for fast lookup)
    /// </summary>
    public string VillageName { get; set; } = string.Empty;

    /// <summary>
    /// District name (denormalized for fast lookup)
    /// </summary>
    public string DistrictName { get; set; } = string.Empty;

    /// <summary>
    /// Region: North, South, East, West, Central (denormalized)
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to Village (for relational queries)
    /// </summary>
    public int VillageId { get; set; }

    /// <summary>
    /// Foreign key to District (for relational queries)
    /// </summary>
    public int DistrictId { get; set; }

    /// <summary>
    /// Locality type: City, Town, Village
    /// </summary>
    public string? LocalityType { get; set; }

    /// <summary>
    /// Is this the primary postal code for the village?
    /// </summary>
    public bool IsPrimary { get; set; } = true;

    /// <summary>
    /// Additional notes (e.g., "Industrial Zone", "Coastal Area")
    /// </summary>
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual Village? Village { get; set; }
    public virtual District? District { get; set; }
}
