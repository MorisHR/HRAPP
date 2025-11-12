using HRMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HRMS.Infrastructure.Persistence.ValueConverters;

/// <summary>
/// EF Core value converter for encrypting/decrypting decimal properties (salaries, amounts)
///
/// USAGE:
/// modelBuilder.Entity&lt;Employee&gt;()
///     .Property(e => e.BasicSalary)
///     .HasConversion(new EncryptedDecimalConverter(encryptionService));
///
/// IMPLEMENTATION:
/// - Converts decimal to string (invariant culture)
/// - Encrypts the string representation
/// - Stores encrypted data as text in database
/// - Decrypts and parses back to decimal on retrieval
///
/// PRECISION:
/// - Maintains full decimal precision (no rounding)
/// - Uses InvariantCulture to avoid locale issues
/// - Handles null values correctly
///
/// PERFORMANCE:
/// - Slightly slower than native decimal columns
/// - Cannot use database aggregations (SUM, AVG, etc.)
/// - Consider denormalized totals for reporting
/// </summary>
public class EncryptedDecimalConverter : ValueConverter<decimal, string?>
{
    public EncryptedDecimalConverter(IEncryptionService encryptionService)
        : base(
            plainValue => encryptionService.Encrypt(plainValue.ToString(System.Globalization.CultureInfo.InvariantCulture)) ?? string.Empty,
            encryptedValue => string.IsNullOrWhiteSpace(encryptedValue)
                ? 0m
                : decimal.Parse(encryptionService.Decrypt(encryptedValue) ?? "0", System.Globalization.CultureInfo.InvariantCulture))
    {
    }
}
