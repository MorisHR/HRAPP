using FluentValidation;
using HRMS.Application.DTOs;
using HRMS.Core.Enums;
using System.Text.RegularExpressions;

namespace HRMS.Application.Validators.Leave;

/// <summary>
/// Production-grade validator for leave application creation
/// Validates dates, reasons, attachments, and business rules
/// Ensures data integrity and prevents invalid leave requests
/// </summary>
public class CreateLeaveApplicationRequestValidator : AbstractValidator<CreateLeaveApplicationRequest>
{
    // E.164 phone number format (international)
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$",
        RegexOptions.Compiled
    );

    // Common file extensions for attachments (medical certificates, documents)
    private static readonly HashSet<string> AllowedFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"
    };

    // Maximum file size: 5MB (in base64, ~6.67MB actual)
    private const int MaxFileSizeBytes = 5 * 1024 * 1024;

    // Maximum leave duration in days (configurable per organization)
    private const int MaxLeaveDurationDays = 365;

    // Minimum advance notice for leave (in days)
    private const int MinAdvanceNoticeDays = 1;

    public CreateLeaveApplicationRequestValidator()
    {
        // Leave Type ID Validation
        RuleFor(x => x.LeaveTypeId)
            .NotEmpty()
            .WithMessage("Leave type is required")
            .WithName("Leave Type");

        // Start Date Validation
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required")
            .Must(BeValidFutureDate)
            .WithMessage($"Start date must be at least {MinAdvanceNoticeDays} day(s) in the future")
            .Must(NotBeWeekend)
            .WithMessage("Start date cannot be on a weekend (Saturday or Sunday)")
            .WithName("Start Date");

        // End Date Validation
        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("End date is required")
            .Must(NotBeWeekend)
            .WithMessage("End date cannot be on a weekend (Saturday or Sunday)")
            .WithName("End Date");

        // Cross-field validation: End Date must be after or equal to Start Date
        RuleFor(x => x)
            .Must(x => x.EndDate >= x.StartDate)
            .WithMessage("End date must be on or after start date")
            .When(x => x.StartDate != default && x.EndDate != default);

        // Cross-field validation: Leave duration must be reasonable
        RuleFor(x => x)
            .Must(x => (x.EndDate - x.StartDate).TotalDays <= MaxLeaveDurationDays)
            .WithMessage($"Leave duration cannot exceed {MaxLeaveDurationDays} days. Please contact HR for extended leave.")
            .When(x => x.StartDate != default && x.EndDate != default && x.EndDate >= x.StartDate);

        // Reason Validation
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Leave reason is required")
            .MinimumLength(10)
            .WithMessage("Please provide a detailed reason (at least 10 characters)")
            .MaximumLength(1000)
            .WithMessage("Reason is too long (maximum 1000 characters)")
            .Must(BeValidReason)
            .WithMessage("Reason contains invalid or inappropriate content")
            .WithName("Reason");

        // Contact Number Validation (optional, but must be valid if provided)
        RuleFor(x => x.ContactNumber)
            .Must(BeValidPhoneNumber)
            .WithMessage("Please provide a valid phone number in E.164 format (e.g., +230XXXXXXXX)")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactNumber))
            .WithName("Contact Number");

        // Contact Address Validation (optional, but must be valid if provided)
        RuleFor(x => x.ContactAddress)
            .MinimumLength(5)
            .WithMessage("Contact address must be at least 5 characters long")
            .MaximumLength(500)
            .WithMessage("Contact address is too long (maximum 500 characters)")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactAddress))
            .WithName("Contact Address");

        // Calculation Type Validation
        RuleFor(x => x.CalculationType)
            .IsInEnum()
            .WithMessage("Invalid leave calculation type")
            .WithName("Calculation Type");

        // Attachment Validation (if provided)
        RuleFor(x => x.AttachmentFileName)
            .Must(HaveValidFileExtension)
            .WithMessage($"File type not allowed. Allowed types: {string.Join(", ", AllowedFileExtensions)}")
            .When(x => !string.IsNullOrWhiteSpace(x.AttachmentBase64))
            .WithName("Attachment");

        RuleFor(x => x.AttachmentBase64)
            .Must(BeValidBase64)
            .WithMessage("Attachment file is corrupted or invalid")
            .Must(BeWithinSizeLimit)
            .WithMessage($"Attachment size exceeds maximum limit of {MaxFileSizeBytes / (1024 * 1024)}MB")
            .When(x => !string.IsNullOrWhiteSpace(x.AttachmentBase64))
            .WithName("Attachment");

        // Cross-field validation: If filename is provided, base64 must be provided too
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.AttachmentBase64))
            .WithMessage("Attachment file content is missing")
            .When(x => !string.IsNullOrWhiteSpace(x.AttachmentFileName));

        // Cross-field validation: If base64 is provided, filename must be provided too
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.AttachmentFileName))
            .WithMessage("Attachment file name is missing")
            .When(x => !string.IsNullOrWhiteSpace(x.AttachmentBase64));
    }

    /// <summary>
    /// Validates that the date is in the future with minimum advance notice
    /// </summary>
    private bool BeValidFutureDate(DateTime date)
    {
        // Convert to date-only comparison (ignore time component)
        var dateOnly = date.Date;
        var today = DateTime.UtcNow.Date;

        // Must be at least MinAdvanceNoticeDays in the future
        return dateOnly >= today.AddDays(MinAdvanceNoticeDays);
    }

    /// <summary>
    /// Validates that the date is not a weekend (configurable per organization)
    /// </summary>
    private bool NotBeWeekend(DateTime date)
    {
        // Most organizations don't allow leave starting/ending on weekends
        // This can be made configurable based on organization settings
        return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
    }

    /// <summary>
    /// Validates phone number format
    /// </summary>
    private bool BeValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return true; // Optional field

        // Remove common formatting characters
        var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        return PhoneRegex.IsMatch(cleaned);
    }

    /// <summary>
    /// Validates that the reason is appropriate and not spam
    /// </summary>
    private bool BeValidReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return false;

        // Check for excessive repeated characters (spam detection)
        var repeatedCharPattern = new Regex(@"(.)\1{9,}", RegexOptions.Compiled);
        if (repeatedCharPattern.IsMatch(reason))
            return false;

        // Check for minimum word count (at least 3 words)
        var words = reason.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 3)
            return false;

        // Check for inappropriate patterns (can be extended based on requirements)
        var inappropriatePatterns = new[] { "test", "asdf", "qwerty", "lorem ipsum" };
        var lowerReason = reason.ToLowerInvariant();
        if (inappropriatePatterns.Any(pattern => lowerReason.Contains(pattern)))
            return false;

        return true;
    }

    /// <summary>
    /// Validates file extension
    /// </summary>
    private bool HaveValidFileExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName);
        return AllowedFileExtensions.Contains(extension);
    }

    /// <summary>
    /// Validates base64 string format
    /// </summary>
    private bool BeValidBase64(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return false;

        try
        {
            // Remove data URI prefix if present (e.g., "data:image/png;base64,")
            var cleanBase64 = base64;
            if (base64.Contains(","))
            {
                var parts = base64.Split(',');
                cleanBase64 = parts[parts.Length - 1];
            }

            // Try to decode
            Convert.FromBase64String(cleanBase64);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates file size from base64 string
    /// </summary>
    private bool BeWithinSizeLimit(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return false;

        try
        {
            // Remove data URI prefix if present
            var cleanBase64 = base64;
            if (base64.Contains(","))
            {
                var parts = base64.Split(',');
                cleanBase64 = parts[parts.Length - 1];
            }

            // Decode and check size
            var bytes = Convert.FromBase64String(cleanBase64);
            return bytes.Length <= MaxFileSizeBytes;
        }
        catch
        {
            return false;
        }
    }
}
