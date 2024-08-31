namespace ZoneTree.FullTextSearch.Normalizers;

/// <summary>
/// Defines a contract for normalizing strings or spans of characters.
/// </summary>
public interface IStringNormalizer
{
    /// <summary>
    /// Normalizes a span of characters, returning a string with normalized characters.
    /// </summary>
    /// <param name="input">The span of characters to normalize.</param>
    /// <returns>A normalized string.</returns>
    string Normalize(ReadOnlySpan<char> input);
}