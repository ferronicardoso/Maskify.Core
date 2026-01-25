using System.Runtime.CompilerServices;

namespace Maskify.Core.Cnpj;

/// <summary>
/// Facade for CNPJ masking operations.
/// Provides a unified API for masking both legacy numeric and new alphanumeric CNPJ formats.
/// </summary>
/// <remarks>
/// <para>
/// This facade automatically detects the CNPJ format and applies the appropriate masking strategy.
/// </para>
/// <para>
/// <strong>Supported formats:</strong>
/// <list type="bullet">
///   <item><description>Numeric CNPJ: 14 digits (e.g., 12345678000195)</description></item>
///   <item><description>Alphanumeric CNPJ: 14 alphanumeric characters (e.g., 12ABC678DEF195)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Masking behavior:</strong> Characters at positions 2-9 are masked (8 characters total).
/// </para>
/// <para>
/// <strong>Performance:</strong> This implementation is optimized for zero heap allocations
/// except for the final string result. Uses Span&lt;T&gt; and stackalloc throughout.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Numeric CNPJ
/// string masked = CnpjMaskFacade.Mask("12345678000195");
/// // Result: "12.***.***/****-95"
/// 
/// // Alphanumeric CNPJ
/// string masked = CnpjMaskFacade.Mask("12ABC678DEF195");
/// // Result: "12.***.***/****-95"
/// 
/// // Custom mask character
/// string masked = CnpjMaskFacade.Mask("12345678000195", '#');
/// // Result: "12.###.###/####-95"
/// </code>
/// </example>
public static class CnpjMaskFacade
{
    /// <summary>
    /// Expected length of a valid CNPJ (alphanumeric characters only).
    /// </summary>
    private const int CnpjLength = 14;

    /// <summary>
    /// Formatted CNPJ output length: XX.XXX.XXX/XXXX-XX
    /// </summary>
    private const int FormattedCnpjLength = 18;

    /// <summary>
    /// Start index for masking (inclusive).
    /// </summary>
    private const int MaskStartIndex = 2;

    /// <summary>
    /// End index for masking (exclusive).
    /// </summary>
    private const int MaskEndIndex = 10;

    /// <summary>
    /// Masks a CNPJ string by hiding characters at positions 2-9.
    /// Automatically detects whether the CNPJ is numeric or alphanumeric.
    /// </summary>
    /// <param name="cnpj">
    /// The CNPJ to mask. Can be formatted (with dots, slashes, dashes) or unformatted.
    /// Supports both numeric (14 digits) and alphanumeric (14 characters) formats.
    /// </param>
    /// <param name="maskCharacter">
    /// The character to use for masking. Defaults to '*'.
    /// </param>
    /// <returns>
    /// The masked CNPJ in standard format: XX.XXX.XXX/XXXX-XX
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cnpj"/> is null or whitespace.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="cnpj"/> does not contain exactly 14 valid characters.
    /// </exception>
    /// <remarks>
    /// <para><strong>Performance notes:</strong></para>
    /// <list type="bullet">
    ///   <item>Single pass extraction and validation (no duplicate scans)</item>
    ///   <item>Zero heap allocations except final string result</item>
    ///   <item>Uses stackalloc for intermediate buffers</item>
    ///   <item>Inlined hot path methods for reduced call overhead</item>
    /// </list>
    /// </remarks>
    public static string Mask(string cnpj, char maskCharacter = '*')
    {
        // Fast null/empty check - string.IsNullOrWhiteSpace is already optimized
        if (string.IsNullOrWhiteSpace(cnpj))
        {
            throw new ArgumentNullException(nameof(cnpj), "CNPJ not provided.");
        }

        ReadOnlySpan<char> cnpjSpan = cnpj.AsSpan();

        // PERFORMANCE: Single-pass extraction, validation, and normalization
        // Avoids multiple iterations over the input that were present in the Strategy pattern
        Span<char> extracted = stackalloc char[CnpjLength];
        int index = 0;

        // Single loop: extract alphanumeric chars and normalize case
        for (int i = 0; i < cnpjSpan.Length; i++)
        {
            char c = cnpjSpan[i];

            // PERFORMANCE: Use bit manipulation for faster letter/digit detection
            // char.IsLetterOrDigit has overhead from Unicode category lookup
            if (IsAsciiLetterOrDigit(c))
            {
                if (index >= CnpjLength)
                {
                    // Too many characters - will fail validation below
                    index++;
                    continue;
                }

                // PERFORMANCE: Fast uppercase conversion for ASCII only
                // Avoids char.ToUpperInvariant which handles full Unicode
                if (IsAsciiLower(c))
                {
                    extracted[index++] = (char)(c & ~0x20); // Clear bit 5 to uppercase
                }
                else if (IsAsciiUpper(c))
                {
                    extracted[index++] = c;
                }
                else // IsDigit
                {
                    extracted[index++] = c;
                }
            }
            // Non-alphanumeric characters (formatting) are skipped
        }

        // Validate extracted length
        if (index != CnpjLength)
        {
            throw new ArgumentException(
                $"CNPJ must have exactly {CnpjLength} alphanumeric characters. Found: {index}.",
                nameof(cnpj));
        }

        // PERFORMANCE: Apply mask in-place on the extracted buffer
        // Loop unrolling candidate, but 8 iterations is small enough
        for (int i = MaskStartIndex; i < MaskEndIndex; i++)
        {
            extracted[i] = maskCharacter;
        }

        // PERFORMANCE: Inline formatting to avoid method call overhead
        // Format directly into the final buffer
        return FormatCnpj(extracted);
    }

    /// <summary>
    /// Formats the 14-character CNPJ into standard format: XX.XXX.XXX/XXXX-XX
    /// </summary>
    /// <remarks>
    /// PERFORMANCE: Inlined to avoid method call overhead on hot path.
    /// Uses direct indexing instead of loop for predictable branch elimination.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string FormatCnpj(ReadOnlySpan<char> cnpj)
    {
        // PERFORMANCE: stackalloc is cheap and stays on stack
        // 18 chars is well under the stack safety threshold
        Span<char> formatted = stackalloc char[FormattedCnpjLength];

        // Direct assignment is faster than loop with offset calculations
        // Compiler can optimize this into efficient memory moves
        formatted[0] = cnpj[0];
        formatted[1] = cnpj[1];
        formatted[2] = '.';
        formatted[3] = cnpj[2];
        formatted[4] = cnpj[3];
        formatted[5] = cnpj[4];
        formatted[6] = '.';
        formatted[7] = cnpj[5];
        formatted[8] = cnpj[6];
        formatted[9] = cnpj[7];
        formatted[10] = '/';
        formatted[11] = cnpj[8];
        formatted[12] = cnpj[9];
        formatted[13] = cnpj[10];
        formatted[14] = cnpj[11];
        formatted[15] = '-';
        formatted[16] = cnpj[12];
        formatted[17] = cnpj[13];

        // PERFORMANCE: Only heap allocation in the entire method
        return new string(formatted);
    }

    /// <summary>
    /// Fast ASCII letter or digit check using bit manipulation.
    /// </summary>
    /// <remarks>
    /// PERFORMANCE: Avoids char.IsLetterOrDigit which performs Unicode category lookup.
    /// This library focuses on CNPJ which uses ASCII characters only.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAsciiLetterOrDigit(char c)
    {
        // Check digit: '0' (48) to '9' (57)
        // Check letter: 'A' (65) to 'Z' (90) or 'a' (97) to 'z' (122)
        return (uint)(c - '0') <= 9 ||
               (uint)(c - 'A') <= 25 ||
               (uint)(c - 'a') <= 25;
    }

    /// <summary>
    /// Fast ASCII lowercase check.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAsciiLower(char c) => (uint)(c - 'a') <= 25;

    /// <summary>
    /// Fast ASCII uppercase check.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAsciiUpper(char c) => (uint)(c - 'A') <= 25;
}
