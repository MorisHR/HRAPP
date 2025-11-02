using FluentValidation;
using HRMS.Application.DTOs;
using HRMS.Core.Enums;
using System.Text.RegularExpressions;

namespace HRMS.Application.Validators.Employees;

/// <summary>
/// Production-grade validator for employee creation
/// Comprehensive validation for all employee fields including identification, tax, visa, and employment details
/// Ensures compliance with Mauritius labour laws and data integrity
/// </summary>
public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    // RFC 5322 compliant email regex
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    // E.164 phone number format
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$",
        RegexOptions.Compiled
    );

    // Employee code pattern (alphanumeric, hyphens, underscores)
    private static readonly Regex EmployeeCodeRegex = new(
        @"^[A-Z0-9_-]+$",
        RegexOptions.Compiled
    );

    // Valid currency codes (ISO 4217)
    private static readonly HashSet<string> ValidCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "MUR", "USD", "EUR", "GBP", "ZAR", "INR", "CNY", "JPY"
    };

    // Minimum age for employment (typically 18)
    private const int MinEmploymentAge = 18;

    // Maximum age for employment validation (reasonable upper bound)
    private const int MaxEmploymentAge = 80;

    public CreateEmployeeRequestValidator()
    {
        // ===== BASIC INFORMATION =====

        RuleFor(x => x.EmployeeCode)
            .NotEmpty()
            .WithMessage("Employee code is required")
            .MinimumLength(2)
            .WithMessage("Employee code must be at least 2 characters long")
            .MaximumLength(50)
            .WithMessage("Employee code cannot exceed 50 characters")
            .Must(BeValidEmployeeCode)
            .WithMessage("Employee code must contain only uppercase letters, numbers, hyphens, or underscores")
            .WithName("Employee Code");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MinimumLength(2)
            .WithMessage("First name must be at least 2 characters long")
            .MaximumLength(100)
            .WithMessage("First name cannot exceed 100 characters")
            .Must(BeValidName)
            .WithMessage("First name contains invalid characters")
            .WithName("First Name");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MinimumLength(2)
            .WithMessage("Last name must be at least 2 characters long")
            .MaximumLength(100)
            .WithMessage("Last name cannot exceed 100 characters")
            .Must(BeValidName)
            .WithMessage("Last name contains invalid characters")
            .WithName("Last Name");

        RuleFor(x => x.MiddleName)
            .MaximumLength(100)
            .WithMessage("Middle name cannot exceed 100 characters")
            .Must(BeValidName)
            .When(x => !string.IsNullOrWhiteSpace(x.MiddleName))
            .WithMessage("Middle name contains invalid characters")
            .WithName("Middle Name");

        // ===== CONTACT INFORMATION =====

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .MaximumLength(320)
            .WithMessage("Email address is too long (maximum 320 characters)")
            .Must(BeValidEmail)
            .WithMessage("Please provide a valid email address")
            .WithName("Email");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required")
            .Must(BeValidPhoneNumber)
            .WithMessage("Please provide a valid phone number in E.164 format (e.g., +230XXXXXXXX)")
            .WithName("Phone Number");

        RuleFor(x => x.PersonalEmail)
            .MaximumLength(320)
            .WithMessage("Personal email address is too long (maximum 320 characters)")
            .Must(BeValidEmail)
            .When(x => !string.IsNullOrWhiteSpace(x.PersonalEmail))
            .WithMessage("Please provide a valid personal email address")
            .WithName("Personal Email");

        // ===== PERSONAL DETAILS =====

        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .WithMessage("Date of birth is required")
            .Must(BeValidAge)
            .WithMessage($"Employee must be between {MinEmploymentAge} and {MaxEmploymentAge} years old")
            .WithName("Date of Birth");

        RuleFor(x => x.Gender)
            .IsInEnum()
            .WithMessage("Invalid gender selected")
            .WithName("Gender");

        RuleFor(x => x.MaritalStatus)
            .IsInEnum()
            .WithMessage("Invalid marital status selected")
            .WithName("Marital Status");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Address is required")
            .MinimumLength(10)
            .WithMessage("Please provide a complete address (at least 10 characters)")
            .MaximumLength(500)
            .WithMessage("Address is too long (maximum 500 characters)")
            .WithName("Address");

        RuleFor(x => x.City)
            .MaximumLength(100)
            .WithMessage("City name is too long (maximum 100 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.City))
            .WithName("City");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20)
            .WithMessage("Postal code is too long (maximum 20 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.PostalCode))
            .WithName("Postal Code");

        // ===== EMPLOYEE TYPE & NATIONALITY =====

        RuleFor(x => x.EmployeeType)
            .IsInEnum()
            .WithMessage("Invalid employee type selected")
            .WithName("Employee Type");

        RuleFor(x => x.Nationality)
            .NotEmpty()
            .WithMessage("Nationality is required")
            .MaximumLength(100)
            .WithMessage("Nationality is too long (maximum 100 characters)")
            .WithName("Nationality");

        RuleFor(x => x.CountryOfOrigin)
            .MaximumLength(100)
            .WithMessage("Country of origin is too long (maximum 100 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.CountryOfOrigin))
            .WithName("Country of Origin");

        // ===== IDENTIFICATION =====

        RuleFor(x => x.NationalIdCard)
            .MaximumLength(50)
            .WithMessage("National ID card number is too long (maximum 50 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.NationalIdCard))
            .WithName("National ID Card");

        RuleFor(x => x.PassportNumber)
            .MaximumLength(50)
            .WithMessage("Passport number is too long (maximum 50 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.PassportNumber))
            .WithName("Passport Number");

        // Passport expiry must be in the future if provided
        RuleFor(x => x.PassportExpiryDate)
            .Must(BeValidFutureDate)
            .When(x => x.PassportExpiryDate.HasValue)
            .WithMessage("Passport has expired or will expire soon")
            .WithName("Passport Expiry Date");

        // Passport issue date must be before expiry date
        RuleFor(x => x)
            .Must(x => x.PassportIssueDate < x.PassportExpiryDate)
            .When(x => x.PassportIssueDate.HasValue && x.PassportExpiryDate.HasValue)
            .WithMessage("Passport issue date must be before expiry date");

        // ===== VISA/WORK PERMIT (Expatriates) =====

        RuleFor(x => x.VisaType)
            .IsInEnum()
            .When(x => x.VisaType.HasValue)
            .WithMessage("Invalid visa type selected")
            .WithName("Visa Type");

        RuleFor(x => x.VisaNumber)
            .NotEmpty()
            .WithMessage("Visa number is required when visa type is specified")
            .When(x => x.VisaType.HasValue)
            .MaximumLength(100)
            .WithMessage("Visa number is too long (maximum 100 characters)")
            .WithName("Visa Number");

        RuleFor(x => x.VisaExpiryDate)
            .NotEmpty()
            .WithMessage("Visa expiry date is required when visa type is specified")
            .When(x => x.VisaType.HasValue)
            .Must(BeValidFutureDate)
            .When(x => x.VisaExpiryDate.HasValue)
            .WithMessage("Visa has expired or will expire soon")
            .WithName("Visa Expiry Date");

        // Visa issue date must be before expiry date
        RuleFor(x => x)
            .Must(x => x.VisaIssueDate < x.VisaExpiryDate)
            .When(x => x.VisaIssueDate.HasValue && x.VisaExpiryDate.HasValue)
            .WithMessage("Visa issue date must be before expiry date");

        RuleFor(x => x.WorkPermitNumber)
            .MaximumLength(100)
            .WithMessage("Work permit number is too long (maximum 100 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.WorkPermitNumber))
            .WithName("Work Permit Number");

        RuleFor(x => x.WorkPermitExpiryDate)
            .Must(BeValidFutureDate)
            .When(x => x.WorkPermitExpiryDate.HasValue)
            .WithMessage("Work permit has expired or will expire soon")
            .WithName("Work Permit Expiry Date");

        // ===== TAX & STATUTORY (Mauritius Specific) =====

        RuleFor(x => x.TaxResidentStatus)
            .IsInEnum()
            .WithMessage("Invalid tax resident status selected")
            .WithName("Tax Resident Status");

        RuleFor(x => x.TaxIdNumber)
            .MaximumLength(50)
            .WithMessage("Tax ID number is too long (maximum 50 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.TaxIdNumber))
            .WithName("Tax ID Number");

        RuleFor(x => x.NPFNumber)
            .MaximumLength(50)
            .WithMessage("NPF number is too long (maximum 50 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.NPFNumber))
            .WithName("NPF Number");

        RuleFor(x => x.NSFNumber)
            .MaximumLength(50)
            .WithMessage("NSF number is too long (maximum 50 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.NSFNumber))
            .WithName("NSF Number");

        RuleFor(x => x.PRGFNumber)
            .MaximumLength(50)
            .WithMessage("PRGF number is too long (maximum 50 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.PRGFNumber))
            .WithName("PRGF Number");

        // ===== EMPLOYMENT DETAILS =====

        RuleFor(x => x.JobTitle)
            .NotEmpty()
            .WithMessage("Job title is required")
            .MinimumLength(2)
            .WithMessage("Job title must be at least 2 characters long")
            .MaximumLength(200)
            .WithMessage("Job title is too long (maximum 200 characters)")
            .WithName("Job Title");

        RuleFor(x => x.DepartmentId)
            .NotEmpty()
            .WithMessage("Department is required")
            .WithName("Department");

        RuleFor(x => x.JoiningDate)
            .NotEmpty()
            .WithMessage("Joining date is required")
            .Must(BeReasonableJoiningDate)
            .WithMessage("Joining date must be within the last 100 years and not more than 1 year in the future")
            .WithName("Joining Date");

        // Probation end date must be after joining date
        RuleFor(x => x)
            .Must(x => x.ProbationEndDate > x.JoiningDate)
            .When(x => x.ProbationEndDate.HasValue && x.JoiningDate != default)
            .WithMessage("Probation end date must be after joining date");

        // Contract end date must be after joining date
        RuleFor(x => x)
            .Must(x => x.ContractEndDate > x.JoiningDate)
            .When(x => x.ContractEndDate.HasValue && x.JoiningDate != default)
            .WithMessage("Contract end date must be after joining date");

        // ===== SALARY & BANK DETAILS =====

        RuleFor(x => x.BasicSalary)
            .NotEmpty()
            .WithMessage("Basic salary is required")
            .GreaterThan(0)
            .WithMessage("Basic salary must be greater than zero")
            .LessThanOrEqualTo(10000000) // 10 million (reasonable upper bound)
            .WithMessage("Basic salary exceeds reasonable limits (maximum 10,000,000)")
            .WithName("Basic Salary");

        RuleFor(x => x.SalaryCurrency)
            .NotEmpty()
            .WithMessage("Salary currency is required")
            .MaximumLength(10)
            .WithMessage("Currency code is too long (maximum 10 characters)")
            .Must(BeValidCurrency)
            .WithMessage("Invalid currency code. Valid currencies: " + string.Join(", ", ValidCurrencies))
            .WithName("Salary Currency");

        RuleFor(x => x.BankName)
            .MaximumLength(200)
            .WithMessage("Bank name is too long (maximum 200 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.BankName))
            .WithName("Bank Name");

        RuleFor(x => x.BankAccountNumber)
            .MaximumLength(100)
            .WithMessage("Bank account number is too long (maximum 100 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.BankAccountNumber))
            .WithName("Bank Account Number");

        RuleFor(x => x.BankBranch)
            .MaximumLength(200)
            .WithMessage("Bank branch is too long (maximum 200 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.BankBranch))
            .WithName("Bank Branch");

        RuleFor(x => x.BankSwiftCode)
            .MaximumLength(50)
            .WithMessage("Bank SWIFT code is too long (maximum 50 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.BankSwiftCode))
            .WithName("Bank SWIFT Code");

        // ===== LEAVE BALANCES =====

        RuleFor(x => x.AnnualLeaveBalance)
            .GreaterThanOrEqualTo(0)
            .When(x => x.AnnualLeaveBalance.HasValue)
            .WithMessage("Annual leave balance cannot be negative")
            .LessThanOrEqualTo(365)
            .When(x => x.AnnualLeaveBalance.HasValue)
            .WithMessage("Annual leave balance cannot exceed 365 days")
            .WithName("Annual Leave Balance");

        RuleFor(x => x.SickLeaveBalance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Sick leave balance cannot be negative")
            .LessThanOrEqualTo(365)
            .WithMessage("Sick leave balance cannot exceed 365 days")
            .WithName("Sick Leave Balance");

        RuleFor(x => x.CasualLeaveBalance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Casual leave balance cannot be negative")
            .LessThanOrEqualTo(365)
            .WithMessage("Casual leave balance cannot exceed 365 days")
            .WithName("Casual Leave Balance");

        // ===== EMERGENCY CONTACTS =====

        RuleFor(x => x.EmergencyContacts)
            .Must(contacts => contacts.Count <= 10)
            .WithMessage("Maximum of 10 emergency contacts allowed")
            .WithName("Emergency Contacts");
    }

    private bool BeValidEmployeeCode(string code)
    {
        return !string.IsNullOrWhiteSpace(code) && EmployeeCodeRegex.IsMatch(code);
    }

    private bool BeValidName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Allow letters, spaces, hyphens, apostrophes (for names like O'Brien, Jean-Claude)
        return name.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '\'' || c == '.');
    }

    private bool BeValidEmail(string? email)
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

        return true;
    }

    private bool BeValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        return PhoneRegex.IsMatch(cleaned);
    }

    private bool BeValidAge(DateTime dateOfBirth)
    {
        var age = DateTime.UtcNow.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > DateTime.UtcNow.AddYears(-age))
            age--;

        return age >= MinEmploymentAge && age <= MaxEmploymentAge;
    }

    private bool BeValidFutureDate(DateTime? date)
    {
        if (!date.HasValue)
            return true;

        // Must be at least 30 days in the future (grace period for renewals)
        return date.Value.Date >= DateTime.UtcNow.Date.AddDays(30);
    }

    private bool BeReasonableJoiningDate(DateTime joiningDate)
    {
        var today = DateTime.UtcNow.Date;
        var hundredYearsAgo = today.AddYears(-100);
        var oneYearInFuture = today.AddYears(1);

        return joiningDate.Date >= hundredYearsAgo && joiningDate.Date <= oneYearInFuture;
    }

    private bool BeValidCurrency(string currency)
    {
        return ValidCurrencies.Contains(currency);
    }
}
