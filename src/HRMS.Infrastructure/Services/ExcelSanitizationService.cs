using System.Text.RegularExpressions;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for sanitizing Excel/CSV data to prevent formula injection attacks
/// SECURITY: Protects against CSV/Excel injection (=cmd, @sum, +cmd, -cmd attacks)
/// COMPLIANCE: OWASP Top 10 - Injection Prevention
/// </summary>
public static class ExcelSanitizationService
{
    /// <summary>
    /// Characters that could start a formula injection attack
    /// = Formula (most common)
    /// + Plus formula
    /// - Minus formula
    /// @ Function call
    /// \t Tab (can be used in attacks)
    /// \r Carriage return (can be used in attacks)
    /// </summary>
    private static readonly char[] FormulaStartCharacters = { '=', '+', '-', '@', '\t', '\r' };

    /// <summary>
    /// Sanitizes a string value for safe Excel export
    /// Prevents formula injection by prefixing dangerous characters with single quote
    /// </summary>
    /// <param name="value">The value to sanitize</param>
    /// <returns>Sanitized value safe for Excel export</returns>
    public static string SanitizeForExcel(string? value)
    {
        // Null or empty strings are safe
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Trim whitespace that could hide malicious content
        value = value.Trim();

        // Check if value is empty after trimming
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Check if starts with dangerous character
        if (FormulaStartCharacters.Contains(value[0]))
        {
            // Prefix with single quote to force Excel to treat as text
            // Single quote is Excel's escape mechanism for formulas
            return "'" + value;
        }

        // Check for potential command execution patterns (additional protection)
        // Pattern: @SUM(, @cmd|, DDE, etc.
        if (ContainsDangerousPattern(value))
        {
            return "'" + value;
        }

        return value;
    }

    /// <summary>
    /// Sanitizes a decimal value for Excel export
    /// Converts to string with 2 decimal places
    /// </summary>
    public static string SanitizeForExcel(decimal? value)
    {
        if (!value.HasValue)
            return "0.00";

        return value.Value.ToString("F2");
    }

    /// <summary>
    /// Sanitizes an integer value for Excel export
    /// </summary>
    public static string SanitizeForExcel(int? value)
    {
        return value?.ToString() ?? "0";
    }

    /// <summary>
    /// Sanitizes a date value for Excel export
    /// Uses ISO 8601 format (yyyy-MM-dd) for clarity
    /// </summary>
    public static string SanitizeForExcel(DateTime? value)
    {
        return value?.ToString("yyyy-MM-dd") ?? string.Empty;
    }

    /// <summary>
    /// Sanitizes a boolean value for Excel export
    /// </summary>
    public static string SanitizeForExcel(bool value)
    {
        return value ? "Yes" : "No";
    }

    /// <summary>
    /// Checks for dangerous patterns that could indicate formula injection
    /// Even if not starting with formula character, some patterns are suspicious
    /// </summary>
    private static bool ContainsDangerousPattern(string value)
    {
        // Convert to uppercase for case-insensitive matching
        var upperValue = value.ToUpperInvariant();

        // Check for command execution patterns
        var dangerousPatterns = new[]
        {
            "CMD|",           // Command execution
            "DDE",            // Dynamic Data Exchange
            "@SUM(",          // Function calls (sometimes hidden in middle)
            "@AVERAGE(",
            "@COUNT(",
            "MSEXCEL|",       // DDE to Excel
            "EXEC|",          // Execute command
            "|'/C",           // Command shell execution
            "POWERSHELL",     // PowerShell execution
            "MSHTA",          // HTML application execution
            "REGSVR32"        // DLL registration/execution
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (upperValue.Contains(pattern))
                return true;
        }

        // Check for Unicode tricks (zero-width characters, etc.)
        // These can hide malicious content
        if (ContainsUnicodeTricks(value))
            return true;

        return false;
    }

    /// <summary>
    /// Detects Unicode tricks that could hide malicious content
    /// Zero-width spaces, right-to-left override, etc.
    /// </summary>
    private static bool ContainsUnicodeTricks(string value)
    {
        // Zero-width characters
        if (value.Contains("\u200B") ||  // Zero-width space
            value.Contains("\u200C") ||  // Zero-width non-joiner
            value.Contains("\u200D") ||  // Zero-width joiner
            value.Contains("\u2060") ||  // Word joiner
            value.Contains("\uFEFF"))    // Zero-width no-break space
        {
            return true;
        }

        // Directional formatting (RTL override)
        if (value.Contains("\u202E") ||  // Right-to-left override
            value.Contains("\u202D"))    // Left-to-right override
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sanitizes an entire row of data for Excel export
    /// Handles mixed types (strings, numbers, dates, booleans)
    /// </summary>
    /// <param name="values">Array of values to sanitize</param>
    /// <returns>Array of sanitized strings</returns>
    public static string[] SanitizeRow(params object?[] values)
    {
        var sanitized = new string[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            sanitized[i] = values[i] switch
            {
                null => string.Empty,
                string s => SanitizeForExcel(s),
                decimal d => SanitizeForExcel(d),
                int num => SanitizeForExcel(num),
                DateTime dt => SanitizeForExcel(dt),
                bool b => SanitizeForExcel(b),
                _ => SanitizeForExcel(values[i]?.ToString())
            };
        }

        return sanitized;
    }

    /// <summary>
    /// Validates if a string is safe for Excel without modification
    /// Useful for performance - only sanitize if needed
    /// </summary>
    public static bool IsSafeForExcel(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        var trimmed = value.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return true;

        // Check first character
        if (FormulaStartCharacters.Contains(trimmed[0]))
            return false;

        // Check for dangerous patterns
        if (ContainsDangerousPattern(trimmed))
            return false;

        return true;
    }

    /// <summary>
    /// Sanitizes a collection of strings for bulk operations
    /// Uses LINQ for efficient processing
    /// </summary>
    public static IEnumerable<string> SanitizeCollection(IEnumerable<string?> values)
    {
        return values.Select(SanitizeForExcel);
    }

    /// <summary>
    /// Creates a sanitized CSV line from values
    /// Handles proper CSV escaping (quotes, commas, newlines)
    /// </summary>
    public static string CreateSafeCSVLine(params object?[] values)
    {
        var sanitized = SanitizeRow(values);
        return string.Join(",", sanitized.Select(EscapeCSVField));
    }

    /// <summary>
    /// Escapes a field for CSV format
    /// Wraps in quotes if contains comma, quote, or newline
    /// </summary>
    private static string EscapeCSVField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Need quotes if contains comma, quote, or newline
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            // Escape quotes by doubling them
            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        return value;
    }
}
