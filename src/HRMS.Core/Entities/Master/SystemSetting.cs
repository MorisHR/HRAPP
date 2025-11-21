using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// System-wide configuration settings
/// FORTUNE 500 PATTERN: Salesforce Setup, AWS Systems Manager Parameter Store
/// </summary>
public class SystemSetting
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Email", "Security", "Features", "Maintenance"
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = "string"; // "string", "boolean", "number", "json"
    public bool IsEncrypted { get; set; }
    public bool IsReadOnly { get; set; }

    // Audit fields
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
