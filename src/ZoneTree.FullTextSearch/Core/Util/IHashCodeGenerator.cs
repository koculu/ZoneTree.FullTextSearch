namespace ZoneTree.FullTextSearch.Core;

/// <summary>
/// Interface for generating hash codes from various text inputs.
/// </summary>
public interface IHashCodeGenerator
{
    /// <summary>
    /// Generates a hash code for a read-only span of characters.
    /// </summary>
    /// <param name="text">The span of characters to hash.</param>
    /// <returns>A 64-bit unsigned integer representing the hash code.</returns>
    ulong GetHashCode(ReadOnlySpan<char> text);

    /// <summary>
    /// Generates a hash code for a string.
    /// </summary>
    /// <param name="text">The string to hash.</param>
    /// <returns>A 64-bit unsigned integer representing the hash code.</returns>
    ulong GetHashCode(string text);

    /// <summary>
    /// Generates a hash code for a read-only memory of characters.
    /// </summary>
    /// <param name="text">The memory of characters to hash.</param>
    /// <returns>A 64-bit unsigned integer representing the hash code.</returns>
    ulong GetHashCode(ReadOnlyMemory<char> text);
}