namespace ZoneTree.FullTextSearch.Normalizers;

/// <summary>
/// Defines a contract for normalizing individual characters.
/// </summary>
public interface ICharNormalizer
{
    /// <summary>
    /// Normalizes a single character by removing or modifying diacritical marks.
    /// </summary>
    /// <param name="input">The character to normalize.</param>
    /// <returns>The normalized character.</returns>
    char Normalize(char input);
}
