namespace HRMS.Application.DTOs.GeographicLocationDtos;

/// <summary>
/// Result of address validation for Mauritius addresses
/// Used to validate district, village, and postal code combinations
/// </summary>
public class AddressValidationResult
{
    /// <summary>
    /// Is the address valid?
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation error messages
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();

    /// <summary>
    /// Validated district (if found)
    /// </summary>
    public DistrictDto? District { get; set; }

    /// <summary>
    /// Validated village (if found)
    /// </summary>
    public VillageDto? Village { get; set; }

    /// <summary>
    /// Validated postal code (if found)
    /// </summary>
    public PostalCodeDto? PostalCode { get; set; }

    /// <summary>
    /// Suggested corrections (if any)
    /// </summary>
    public List<string> Suggestions { get; set; } = new List<string>();

    /// <summary>
    /// Create a valid result
    /// </summary>
    public static AddressValidationResult Valid(DistrictDto? district = null, VillageDto? village = null, PostalCodeDto? postalCode = null)
    {
        return new AddressValidationResult
        {
            IsValid = true,
            District = district,
            Village = village,
            PostalCode = postalCode
        };
    }

    /// <summary>
    /// Create an invalid result with error messages
    /// </summary>
    public static AddressValidationResult Invalid(params string[] errors)
    {
        return new AddressValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }

    /// <summary>
    /// Add an error message
    /// </summary>
    public void AddError(string error)
    {
        IsValid = false;
        Errors.Add(error);
    }

    /// <summary>
    /// Add a suggestion
    /// </summary>
    public void AddSuggestion(string suggestion)
    {
        Suggestions.Add(suggestion);
    }
}
