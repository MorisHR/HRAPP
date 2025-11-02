namespace HRMS.Core.Enums;

/// <summary>
/// Employee classification type
/// </summary>
public enum EmployeeType
{
    /// <summary>
    /// Local Mauritian employee
    /// </summary>
    Local = 1,

    /// <summary>
    /// Expatriate worker (non-Mauritian)
    /// Requires additional documentation: passport, visa/work permit, etc.
    /// </summary>
    Expatriate = 2
}
