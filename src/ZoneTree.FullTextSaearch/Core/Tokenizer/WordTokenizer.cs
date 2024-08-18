namespace ZoneTree.FullTextSearch.Core.Tokenizer;

/// <summary>
/// A tokenizer that splits text into tokens based on word boundaries. This class implements
/// the <see cref="IWordTokenizer"/> interface, providing methods to retrieve or enumerate slices of text.
/// </summary>
public sealed class WordTokenizer : IWordTokenizer
{
    /// <summary>
    /// Gets the minimum length of a token to be included in the tokenization results.
    /// Tokens shorter than this length are ignored.
    /// </summary>
    public int MimimumTokenLength { get; }

    /// <summary>
    /// Gets a value indicating whether digits should be included in the tokens.
    /// If false, only alphabetic characters are considered as part of tokens.
    /// </summary>
    public bool IncludeDigits { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WordTokenizer"/> class with the specified
    /// minimum token length and an option to include digits in the tokens.
    /// </summary>
    /// <param name="mimimumTokenLength">The minimum length of tokens to include in the results. Must be non-negative.</param>
    /// <param name="includeDigits">Whether to include digits in the tokens. Defaults to false.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="mimimumTokenLength"/> is negative.</exception>
    public WordTokenizer(int mimimumTokenLength = 3, bool includeDigits = false)
    {
        if (mimimumTokenLength < 0)
            throw new ArgumentException($"{nameof(mimimumTokenLength)} can't be negative.");
        MimimumTokenLength = mimimumTokenLength;
        IncludeDigits = includeDigits;
    }

    /// <summary>
    /// Splits the given text into a list of slices, where each slice represents a token. 
    /// Tokens are determined based on the settings for minimum token length and whether digits are included.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>A read-only list of <see cref="Slice"/> objects, each representing a token within the text.</returns>
    public IReadOnlyList<Slice> GetSlices(ReadOnlySpan<char> text)
    {
        var digits = IncludeDigits;
        var diff = MimimumTokenLength;
        if (diff > 0) --diff;
        var len = text.Length;
        var tokens = new List<Slice>(len / 15);
        int tokenStart = 0;
        int tokenEnd = 0;
        for (var i = 0; i < len; i++)
        {
            var currentChar = text[i];
            if (digits && char.IsLetterOrDigit(currentChar) ||
                char.IsLetter(currentChar))
            {
                ++tokenEnd;
                continue;
            }
            if (tokenStart < tokenEnd - diff)
                tokens.Add(new Slice(tokenStart, i - tokenStart));
            tokenStart = i + 1;
            tokenEnd = i + 1;
        }
        if (tokenStart < tokenEnd - diff)
            tokens.Add(new Slice(tokenStart, tokenEnd - tokenStart));
        return tokens;
    }

    /// <summary>
    /// Enumerates the slices of the given text, where each slice represents a token.
    /// Tokens are determined based on the settings for minimum token length and whether digits are included.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>An enumerable collection of <see cref="Slice"/> objects, each representing a token within the text.</returns>
    public IEnumerable<Slice> EnumerateSlices(ReadOnlyMemory<char> text)
    {
        var digits = IncludeDigits;
        var diff = MimimumTokenLength;
        if (diff > 0) --diff;
        var len = text.Length;
        int tokenStart = 0;
        int tokenEnd = 0;
        for (var i = 0; i < len; i++)
        {
            var currentChar = text.Span[i];
            if (digits && char.IsLetterOrDigit(currentChar) ||
                char.IsLetter(currentChar))
            {
                ++tokenEnd;
                continue;
            }
            if (tokenStart < tokenEnd - diff)
                yield return new Slice(tokenStart, i - tokenStart);
            tokenStart = i + 1;
            tokenEnd = i + 1;
        }
        if (tokenStart < tokenEnd - diff)
            yield return new Slice(tokenStart, tokenEnd - tokenStart);
    }
}
