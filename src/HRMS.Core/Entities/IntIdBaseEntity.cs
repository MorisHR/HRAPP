namespace HRMS.Core.Entities;

/// <summary>
/// Base entity for master reference data with integer IDs
/// Used for lookup tables and reference data
/// </summary>
public abstract class IntIdBaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
