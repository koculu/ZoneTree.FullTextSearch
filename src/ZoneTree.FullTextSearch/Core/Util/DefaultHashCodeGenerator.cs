namespace ZoneTree.FullTextSearch.Core;

/// <summary>
/// Provides methods to generate a consistent hash code for strings and text spans.
/// The hash code is computed using a custom algorithm that is case-insensitive and 
/// designed to work with both string and span-based inputs.
/// </summary>
public sealed class DefaultHashCodeGenerator : IHashCodeGenerator
{
    /// <summary>
    /// Generates a hash code for the specified read-only span of characters using a custom algorithm.
    /// The hash code is case-insensitive and returns 0 for spans that contain only whitespace.
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
            hashedValue += char.ToLowerInvariant(text[i]);
            hashedValue *= 3074457345618258799ul;
        }
        return hashedValue;
    }

    /// <summary>
    /// Generates a hash code for the specified string using a custom algorithm.
    /// The hash code is case-insensitive and returns 0 for null or empty strings.
    /// </summary>
    /// <param name="text">The input string to hash.</param>
    /// <returns>A 64-bit unsigned integer representing the hash code of the input string.</returns>
    public ulong GetHashCode(string text)
    {
        return GetHashCode(text.AsSpan());
    }

    /// <summary>
    /// Generates a hash code for the specified read-only memory of characters using a custom algorithm.
    /// The hash code is case-insensitive and returns 0 for memory that contains only whitespace.
    /// </summary>
    /// <param name="text">The read-only memory of characters to hash.</param>
    /// <returns>A 64-bit unsigned integer representing the hash code of the input memory.</returns>
    public ulong GetHashCode(ReadOnlyMemory<char> text)
    {
        return GetHashCode(text.Span);
    }
}
