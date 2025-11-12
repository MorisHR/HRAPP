using HRMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HRMS.Infrastructure.Persistence.ValueConverters;

/// <summary>
/// EF Core value converter for encrypting/decrypting string properties
///
/// USAGE:
/// Apply to sensitive columns in DbContext.OnModelCreating():
///
/// modelBuilder.Entity&lt;Employee&gt;()
///     .Property(e => e.BankAccountNumber)
///     .HasConversion(new EncryptedStringConverter(encryptionService));
///
/// FEATURES:
/// - Transparent encryption/decryption during save/load
/// - Handles null values gracefully
/// - No changes required to entity classes
/// - Column remains text type (nvarchar/varchar)
///
/// LIMITATIONS:
/// - Encrypted columns cannot be indexed efficiently
/// - WHERE clauses on encrypted columns won't use indexes
/// - LIKE queries won't work on encrypted data
/// - Consider using hash columns for searchable fields
/// </summary>
public class EncryptedStringConverter : ValueConverter<string?, string?>
{
    public EncryptedStringConverter(IEncryptionService encryptionService)
        : base(
            plaintext => encryptionService.Encrypt(plaintext),
            ciphertext => encryptionService.Decrypt(ciphertext))
    {
    }
}
