namespace HRMS.Core.Enums;

/// <summary>
/// Visa/Permit types for expatriate workers in Mauritius
/// </summary>
public enum VisaType
{
    /// <summary>
    /// Work Permit - For specific skilled positions
    /// </summary>
    WorkPermit = 1,

    /// <summary>
    /// Occupation Permit - For professionals, investors, self-employed
    /// </summary>
    OccupationPermit = 2,

    /// <summary>
    /// Residence Permit - Long-term residence authorization
    /// </summary>
    ResidencePermit = 3,

    /// <summary>
    /// Permanent Residence Permit
    /// </summary>
    PermanentResidencePermit = 4,

    /// <summary>
    /// Dependent Visa - For family members of permit holders
    /// </summary>
    DependentVisa = 5,

    /// <summary>
    /// Student Visa - For educational purposes (if working part-time)
    /// </summary>
    StudentVisa = 6,

    /// <summary>
    /// Other visa types
    /// </summary>
    Other = 99
}
