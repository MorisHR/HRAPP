using FluentValidation;
using HRMS.Application.DTOs;
using System.Text.RegularExpressions;

namespace HRMS.Application.Validators.Auth;

/// <summary>
/// Production-grade validator for login requests
/// Implements NIST password guidelines and email validation
/// Provides detailed, user-friendly error messages
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    // RFC 5322 compliant email regex (simplified)
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public LoginRequestValidator()
    {
        // Email validation
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required")
            .MaximumLength(320) // RFC 5321 maximum email length
            .WithMessage("Email address is too long (maximum 320 characters)")
            .Must(BeValidEmail)
            .WithMessage("Please provide a valid email address")
            .WithName("Email");

        // Password validation
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8) // Production minimum (was 6 in DataAnnotations)
            .WithMessage("Password must be at least 8 characters long")
            .MaximumLength(128) // Prevent DoS attacks with extremely long passwords
            .WithMessage("Password is too long (maximum 128 characters)")
            .WithName("Password");
    }

    /// <summary>
    /// Validates email format using RFC 5322 compliant regex
    /// Additional server-side validation beyond client-side checks
    /// </summary>
    private bool BeValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // Trim whitespace
        email = email.Trim();

        // Check basic format
        if (!EmailRegex.IsMatch(email))
            return false;

        // Additional checks
        var parts = email.Split('@');
        if (parts.Length != 2)
            return false;

        var localPart = parts[0];
        var domainPart = parts[1];

        // Local part validation (before @)
        if (localPart.Length == 0 || localPart.Length > 64)
            return false;

        // Domain part validation (after @)
        if (domainPart.Length == 0 || domainPart.Length > 255)
            return false;

        // Domain must have at least one dot
        if (!domainPart.Contains('.'))
            return false;

        // Domain parts validation
        var domainParts = domainPart.Split('.');
        if (domainParts.Any(part => part.Length == 0 || part.Length > 63))
            return false;

        return true;
    }
}
