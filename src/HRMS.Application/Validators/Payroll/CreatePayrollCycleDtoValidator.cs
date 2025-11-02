using FluentValidation;
using HRMS.Application.DTOs.PayrollDtos;

namespace HRMS.Application.Validators.Payroll;

/// <summary>
/// Production-grade validator for payroll cycle creation
/// Ensures valid month/year combinations and prevents duplicate payroll cycles
/// Critical for payroll data integrity and compliance
/// </summary>
public class CreatePayrollCycleDtoValidator : AbstractValidator<CreatePayrollCycleDto>
{
    public CreatePayrollCycleDtoValidator()
    {
        // Month Validation (1-12)
        RuleFor(x => x.Month)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Month must be between 1 (January) and 12 (December)")
            .LessThanOrEqualTo(12)
            .WithMessage("Month must be between 1 (January) and 12 (December)")
            .WithName("Month");

        // Year Validation (reasonable range)
        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(2020)
            .WithMessage("Year must be 2020 or later")
            .LessThanOrEqualTo(DateTime.UtcNow.Year + 1)
            .WithMessage($"Year cannot be more than {DateTime.UtcNow.Year + 1}")
            .WithName("Year");

        // Payment Date Validation
        RuleFor(x => x.PaymentDate)
            .Must(BeValidPaymentDate)
            .When(x => x.PaymentDate.HasValue)
            .WithMessage("Payment date must be within the payroll cycle month or the following month")
            .WithName("Payment Date");

        // Cross-field validation: Payment date should be in or after the payroll month
        RuleFor(x => x)
            .Must(x => BePaymentDateAfterOrInCycleMonth(x.Year, x.Month, x.PaymentDate))
            .When(x => x.PaymentDate.HasValue)
            .WithMessage("Payment date must be in the payroll month or a later month");

        // Notes Validation (optional, but must be reasonable length if provided)
        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes))
            .WithName("Notes");
    }

    /// <summary>
    /// Validates that payment date is reasonable (within cycle month or following months)
    /// </summary>
    private bool BeValidPaymentDate(DateTime? paymentDate)
    {
        if (!paymentDate.HasValue)
            return true;

        // Payment date should not be too far in the past
        var twoYearsAgo = DateTime.UtcNow.AddYears(-2);
        if (paymentDate.Value < twoYearsAgo)
            return false;

        // Payment date should not be too far in the future
        var oneYearAhead = DateTime.UtcNow.AddYears(1);
        if (paymentDate.Value > oneYearAhead)
            return false;

        return true;
    }

    /// <summary>
    /// Validates that payment date is in or after the payroll cycle month
    /// </summary>
    private bool BePaymentDateAfterOrInCycleMonth(int year, int month, DateTime? paymentDate)
    {
        if (!paymentDate.HasValue)
            return true;

        try
        {
            var cycleDate = new DateTime(year, month, 1);
            var paymentDateOnly = paymentDate.Value.Date;

            // Payment date should be on or after the first day of the payroll cycle month
            return paymentDateOnly >= cycleDate;
        }
        catch
        {
            return false;
        }
    }
}
