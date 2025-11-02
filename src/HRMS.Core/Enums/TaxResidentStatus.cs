namespace HRMS.Core.Enums;

/// <summary>
/// Tax residency status in Mauritius
/// Determines tax treatment for the employee
/// </summary>
public enum TaxResidentStatus
{
    /// <summary>
    /// Tax resident - Standard Mauritius tax rates apply
    /// </summary>
    Resident = 1,

    /// <summary>
    /// Non-resident - Different tax treatment
    /// May have tax treaty benefits
    /// </summary>
    NonResident = 2,

    /// <summary>
    /// Deemed resident - Specific conditions met for residency
    /// </summary>
    DeemedResident = 3
}
