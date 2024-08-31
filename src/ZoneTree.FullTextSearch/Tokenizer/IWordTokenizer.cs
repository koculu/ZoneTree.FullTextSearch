namespace ZoneTree.FullTextSearch.Tokenizer;

/// <summary>
/// Defines an interface for tokenizing a text into slices. Implementations of this interface 
/// provide methods for retrieving slices of text that represent individual tokens.
/// </summary>
public interface IWordTokenizer
{
    /// <summary>
    /// Splits the given text into a list of slices, where each slice represents a token.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>A read-only list of <see cref="Slice"/> objects, each representing a token within the text.</returns>
    IReadOnlyList<Slice> GetSlices(ReadOnlySpan<char> text);

    /// <summary>
    /// Enumerates the slices of the given text, where each slice represents a token.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>An enumerable collection of <see cref="Slice"/> objects, each representing a token within the text.</returns>
    IEnumerable<Slice> EnumerateSlices(ReadOnlyMemory<char> text);
}
