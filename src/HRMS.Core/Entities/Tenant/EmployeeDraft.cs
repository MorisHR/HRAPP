namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Employee Draft entity - stores in-progress employee registrations
/// Allows shared editing across admins and auto-deletes after 30 days
/// </summary>
public class EmployeeDraft : BaseEntity
{
    /// <summary>
    /// Form data stored as JSON
    /// </summary>
    public string FormDataJson { get; set; } = "{}";

    /// <summary>
    /// Draft name for easy identification (e.g., "John Doe - Manufacturing")
    /// </summary>
    public string DraftName { get; set; } = string.Empty;

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int CompletionPercentage { get; set; } = 0;

    // ==========================================
    // TRACKING
    // ==========================================

    /// <summary>
    /// User ID who created this draft
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Name of user who created this draft
    /// </summary>
    public string CreatedByName { get; set; } = string.Empty;

    /// <summary>
    /// When the draft was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who last edited this draft
    /// </summary>
    public Guid? LastEditedBy { get; set; }

    /// <summary>
    /// Name of user who last edited this draft
    /// </summary>
    public string? LastEditedByName { get; set; }

    /// <summary>
    /// When the draft was last modified
    /// </summary>
    public DateTime LastEditedAt { get; set; } = DateTime.UtcNow;

    // ==========================================
    // AUTO-DELETION
    // ==========================================

    /// <summary>
    /// Draft expires 30 days after creation
    /// Background job will delete expired drafts
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// When the draft was deleted (if soft deleted)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Check if draft has expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Days remaining until expiry
    /// </summary>
    public int DaysUntilExpiry => (ExpiresAt - DateTime.UtcNow).Days;
}
