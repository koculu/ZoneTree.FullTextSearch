using ZoneTree.FullTextSearch.Normalizers;

namespace ZoneTree.FullTextSearch.Hashing;

/// <summary>
/// Provides methods to generate a consistent hash code for strings and text spans with support
/// for case sensitivity and character normalization options.
/// </summary>
public sealed class NormalizableHashCodeGenerator : IHashCodeGenerator
{
    /// <summary>
    /// Indicates whether the hash generation is case-sensitive.
    /// </summary>
    readonly bool IsCaseSensitive;

    /// <summary>
    /// Optional character normalizer used to normalize characters before hashing.
    /// </summary>
    readonly ICharNormalizer CharNormalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="NormalizableHashCodeGenerator"/> class.
    /// </summary>
    /// <param name="charNormalizer">An optional character normalizer to apply before hashing.</param>
    /// <param name="caseSensitive">Determines whether the hash generation is case-sensitive. Default is false.</param>
    public NormalizableHashCodeGenerator(
        ICharNormalizer charNormalizer = null,
        bool caseSensitive = false)
    {
        IsCaseSensitive = caseSensitive;
        CharNormalizer = charNormalizer;
    }

    /// <summary>
    /// Generates a hash code for the specified read-only span of characters using a custom algorithm.
    /// The hash code can be case-sensitive or case-insensitive based on initialization and can apply character normalization.
    /// Returns 0 for spans that contain only whitespace.
    /// </summary>
    /// <param name="text">The read-only span of characters to hash.</param>
    /// <returns>A 64-bit unsigned integer representing the hash code of the input span.</returns>
    public ulong GetHashCode(ReadOnlySpan<char> text)
    {
        if (text.IsWhiteSpace()) return 0;

        var hashedValue = 3074457345618258791ul;
        var len = text.Length;
        for (var i = 0; i < len; i++)
        {
            var character = text[i];

            if (CharNormalizer != null)
                character = CharNormalizer.Normalize(character);

            if (!IsCaseSensitive)
                character = char.ToLowerInvariant(character);

            hashedValue += character;
            hashedValue *= 3074457345618258799ul;
        }
        return hashedValue;
    }

    /// <summary>
    /// Generates a hash code for the specified string using a custom algorithm.
    /// The hash code can be case-sensitive or case-insensitive based on initialization and can apply character normalization.
    /// Returns 0 for null or empty strings.
    /// </summary>
    /// <param name="text">The input string to hash.</param>
    /// <returns>A 64-bit unsigned integer representing the hash code of the input string.</returns>
    public ulong GetHashCode(string text)
    {
        return GetHashCode(text.AsSpan());
    }

    /// <summary>
    /// Generates a hash code for the specified read-only memory of characters using a custom algorithm.
    /// The hash code can be case-sensitive or case-insensitive based on initialization and can apply character normalization.
    /// Returns 0 for memory that contains only whitespace.
    /// </summary>
    /// <param name="text">The read-only memory of characters to hash.</param>
    /// <returns>A 64-bit unsigned integer representing the hash code of the input memory.</returns>
    public ulong GetHashCode(ReadOnlyMemory<char> text)
    {
        return GetHashCode(text.Span);
    }
}
