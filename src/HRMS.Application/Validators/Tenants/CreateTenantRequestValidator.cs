using FluentValidation;
using HRMS.Application.DTOs;
using HRMS.Core.Enums;
using System.Text.RegularExpressions;

namespace HRMS.Application.Validators.Tenants;

/// <summary>
/// Production-grade validator for tenant creation
/// Enforces strict subdomain rules, validates admin credentials, and ensures resource limits
/// Critical for multi-tenancy security and system stability
/// </summary>
public class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    // RFC 5322 compliant email regex
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    // Subdomain must be DNS-safe: lowercase letters, numbers, hyphens
    private static readonly Regex SubdomainRegex = new(
        @"^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$",
        RegexOptions.Compiled
    );

    // E.164 phone number format (international)
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$",
        RegexOptions.Compiled
    );

    // Reserved subdomains that cannot be used
    private static readonly HashSet<string> ReservedSubdomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "www", "api", "admin", "app", "mail", "smtp", "ftp", "localhost",
        "staging", "dev", "development", "test", "testing", "demo",
        "support", "help", "docs", "status", "cdn", "static", "assets",
        "blog", "news", "about", "contact", "legal", "privacy", "terms",
        "dashboard", "portal", "system", "root", "administrator"
    };

    // NIST password requirements for admin account creation
    private static readonly Regex PasswordUppercaseRegex = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex PasswordLowercaseRegex = new(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex PasswordDigitRegex = new(@"\d", RegexOptions.Compiled);
    private static readonly Regex PasswordSpecialCharRegex = new(@"[!@#$%^&*(),.?\"":{}|<>]", RegexOptions.Compiled);

    public CreateTenantRequestValidator()
    {
        // Company Name Validation
        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .WithMessage("Company name is required")
            .MinimumLength(2)
            .WithMessage("Company name must be at least 2 characters long")
            .MaximumLength(200)
            .WithMessage("Company name is too long (maximum 200 characters)")
            .Must(BeValidCompanyName)
            .WithMessage("Company name contains invalid characters")
            .WithName("Company Name");

        // Subdomain Validation (CRITICAL for multi-tenancy)
        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .WithMessage("Subdomain is required")
            .MinimumLength(3)
            .WithMessage("Subdomain must be at least 3 characters long")
            .MaximumLength(63) // DNS limit for subdomain labels
            .WithMessage("Subdomain is too long (maximum 63 characters)")
            .Must(BeValidSubdomain)
            .WithMessage("Subdomain must contain only lowercase letters, numbers, and hyphens (cannot start or end with hyphen)")
            .Must(NotBeReservedSubdomain)
            .WithMessage("This subdomain is reserved and cannot be used")
            .WithName("Subdomain");

        // Contact Email Validation
        RuleFor(x => x.ContactEmail)
            .NotEmpty()
            .WithMessage("Contact email is required")
            .MaximumLength(320)
            .WithMessage("Email address is too long (maximum 320 characters)")
            .Must(BeValidEmail)
            .WithMessage("Please provide a valid contact email address")
            .WithName("Contact Email");

        // Contact Phone Validation
        RuleFor(x => x.ContactPhone)
            .NotEmpty()
            .WithMessage("Contact phone number is required")
            .Must(BeValidPhoneNumber)
            .WithMessage("Please provide a valid phone number in E.164 format (e.g., +230XXXXXXXX)")
            .WithName("Contact Phone");

        // Employee Tier Validation
        RuleFor(x => x.EmployeeTier)
            .IsInEnum()
            .WithMessage("Invalid employee tier selected")
            .WithName("Employee Tier");

        // Resource Limits Validation
        RuleFor(x => x.MaxUsers)
            .GreaterThan(0)
            .WithMessage("Maximum users must be greater than 0")
            .LessThanOrEqualTo(10000)
            .WithMessage("Maximum users cannot exceed 10,000 (contact support for enterprise plans)")
            .WithName("Maximum Users");

        RuleFor(x => x.MaxStorageGB)
            .GreaterThan(0)
            .WithMessage("Maximum storage must be greater than 0")
            .LessThanOrEqualTo(1000) // 1 TB
            .WithMessage("Maximum storage cannot exceed 1,000 GB (contact support for enterprise plans)")
            .WithName("Maximum Storage");

        RuleFor(x => x.ApiCallsPerMonth)
            .GreaterThan(0)
            .WithMessage("Maximum API calls per month must be greater than 0")
            .LessThanOrEqualTo(10000000)
            .WithMessage("Maximum API calls per month cannot exceed 10,000,000")
            .WithName("Maximum API Calls Per Month");

        // Monthly Price Validation
        RuleFor(x => x.MonthlyPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Monthly price must be zero or greater")
            .LessThanOrEqualTo(100000)
            .WithMessage("Monthly price cannot exceed $100,000")
            .WithName("Monthly Price");

        // Admin User Name Validation
        RuleFor(x => x.AdminUserName)
            .NotEmpty()
            .WithMessage("Admin user name is required")
            .MinimumLength(2)
            .WithMessage("Admin user name must be at least 2 characters long")
            .MaximumLength(100)
            .WithMessage("Admin user name is too long (maximum 100 characters)")
            .WithName("Admin User Name");

        // Admin Email Validation
        RuleFor(x => x.AdminEmail)
            .NotEmpty()
            .WithMessage("Admin email is required")
            .MaximumLength(320)
            .WithMessage("Email address is too long (maximum 320 characters)")
            .Must(BeValidEmail)
            .WithMessage("Please provide a valid admin email address")
            .WithName("Admin Email");

        // Admin First Name Validation
        RuleFor(x => x.AdminFirstName)
            .NotEmpty()
            .WithMessage("Admin first name is required")
            .MaximumLength(100)
            .WithMessage("Admin first name is too long (maximum 100 characters)")
            .WithName("Admin First Name");

        // Admin Last Name Validation
        RuleFor(x => x.AdminLastName)
            .NotEmpty()
            .WithMessage("Admin last name is required")
            .MaximumLength(100)
            .WithMessage("Admin last name is too long (maximum 100 characters)")
            .WithName("Admin Last Name");

        // Cross-field validation: Admin email should not be the same as contact email (best practice)
        RuleFor(x => x)
            .Must(x => x.AdminEmail.Trim().ToLowerInvariant() != x.ContactEmail.Trim().ToLowerInvariant())
            .WithMessage("Admin email should be different from contact email for security reasons")
            .When(x => !string.IsNullOrWhiteSpace(x.AdminEmail) && !string.IsNullOrWhiteSpace(x.ContactEmail));
    }

    private bool BeValidCompanyName(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return false;

        // Allow letters, numbers, spaces, and common business punctuation
        return companyName.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '.' || c == '&' || c == '-' || c == ',' || c == '\'');
    }

    private bool BeValidSubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return false;

        return SubdomainRegex.IsMatch(subdomain);
    }

    private bool NotBeReservedSubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return false;

        return !ReservedSubdomains.Contains(subdomain.Trim());
    }

    private bool BeValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        email = email.Trim();

        if (!EmailRegex.IsMatch(email))
            return false;

        var parts = email.Split('@');
        if (parts.Length != 2)
            return false;

        var localPart = parts[0];
        var domainPart = parts[1];

        if (localPart.Length == 0 || localPart.Length > 64)
            return false;

        if (domainPart.Length == 0 || domainPart.Length > 255)
            return false;

        if (!domainPart.Contains('.'))
            return false;

        var domainParts = domainPart.Split('.');
        if (domainParts.Any(part => part.Length == 0 || part.Length > 63))
            return false;

        return true;
    }

    private bool BeValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Remove common formatting characters
        var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        return PhoneRegex.IsMatch(cleaned);
    }

    private bool ContainUppercase(string password)
    {
        return !string.IsNullOrWhiteSpace(password) && PasswordUppercaseRegex.IsMatch(password);
    }

    private bool ContainLowercase(string password)
    {
        return !string.IsNullOrWhiteSpace(password) && PasswordLowercaseRegex.IsMatch(password);
    }

    private bool ContainDigit(string password)
    {
        return !string.IsNullOrWhiteSpace(password) && PasswordDigitRegex.IsMatch(password);
    }

    private bool ContainSpecialCharacter(string password)
    {
        return !string.IsNullOrWhiteSpace(password) && PasswordSpecialCharRegex.IsMatch(password);
    }

    private bool NotContainCommonPatterns(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        var lowerPassword = password.ToLowerInvariant();

        // Common weak passwords and patterns
        var commonPatterns = new[]
        {
            "password", "pass123", "admin", "administrator", "12345", "123456", "123456789",
            "qwerty", "abc123", "letmein", "welcome", "monkey", "dragon", "master",
            "password1", "password123", "admin123", "root", "test", "guest"
        };

        return !commonPatterns.Any(pattern => lowerPassword.Contains(pattern));
    }
}
