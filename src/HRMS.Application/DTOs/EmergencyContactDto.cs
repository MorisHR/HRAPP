using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

public class EmergencyContactDto
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Contact name is required")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid alternate phone number format")]
    public string? AlternatePhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Relationship is required")]
    public string Relationship { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact type is required")]
    public string ContactType { get; set; } = "Local"; // Local, HomeCountry

    public string? Address { get; set; }
    public string? Country { get; set; }
    public bool IsPrimary { get; set; }
}
