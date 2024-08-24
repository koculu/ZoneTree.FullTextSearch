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
    /// Gets a value indicating whether stop words should be used during tokenization.
    /// If true, tokens matching stop words will be excluded from the results.
    /// </summary>
    public bool UseStopWords { get; }

    /// <summary>
    /// A set of hash codes representing stop words to be excluded from tokenization results when <see cref="UseStopWords"/> is true.
    /// </summary>
    HashSet<ulong> StopWords { get; } = new();

    /// <summary>
    /// The hash code generator used to generate hash codes for tokens.
    /// </summary>
    readonly IHashCodeGenerator HashCodeGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="WordTokenizer"/> class with the specified
    /// minimum token length, an option to include digits, and an option to use stop words.
    /// </summary>
    /// <param name="mimimumTokenLength">The minimum length of tokens to include in the results. Must be non-negative.</param>
    /// <param name="includeDigits">Whether to include digits in the tokens. Defaults to false.</param>
    /// <param name="useStopWords">Whether to filter out stop words from the tokens. Defaults to false.</param>
    /// <param name="hashCodeGenerator">The hash code generator used to generate hash codes for the tokens. If null, a default generator is used.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="mimimumTokenLength"/> is negative.</exception>
    public WordTokenizer(
        int mimimumTokenLength = 3,
        bool includeDigits = false,
        bool useStopWords = false,
        IHashCodeGenerator hashCodeGenerator = null)
    {
        if (mimimumTokenLength < 0)
            throw new ArgumentException($"{nameof(mimimumTokenLength)} can't be negative.");
        HashCodeGenerator = hashCodeGenerator ?? new DefaultHashCodeGenerator();
        MimimumTokenLength = mimimumTokenLength;
        IncludeDigits = includeDigits;
        UseStopWords = useStopWords;
        if (useStopWords)
            AddStopWords(DefaultStopWords);
    }

    /// <summary>
    /// The default list of stop words used when <see cref="UseStopWords"/> is enabled.
    /// </summary>
    static readonly string[] DefaultStopWords = new string[] {
            "a", "an", "and", "are", "as", "at", "be", "but", "by",
            "for", "if", "in", "into", "is", "it",
            "no", "not", "of", "on", "or", "such",
            "that", "the", "their", "then", "there", "these",
            "they", "this", "to", "was", "will", "with"};

    /// <summary>
    /// Adds an array of stop words to the internal stop words set. Each word is hashed
    /// and stored in the <see cref="StopWords"/> set.
    /// </summary>
    /// <param name="stopWords">The array of stop words to add.</param>
    public void AddStopWords(string[] stopWords)
    {
        var len = stopWords.Length;
        for (var i = 0; i < len; i++)
        {
            string stopWord = stopWords[i];
            StopWords.Add(HashCodeGenerator.GetHashCode(stopWord));
        }
    }

    /// <summary>
    /// Splits the given text into a list of slices, where each slice represents a token. 
    /// Tokens are determined based on the settings for minimum token length and whether digits are included.
    /// Optionally filters out tokens that match stop words.
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
        var useStopWords = UseStopWords;
        var stopWords = StopWords;
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
            {
                var slice = new Slice(tokenStart, i - tokenStart);
                if (!useStopWords ||
                    !stopWords.Contains(
                        HashCodeGenerator.GetHashCode(
                            text.Slice(slice))))
                    tokens.Add(slice);
            }
            tokenStart = i + 1;
            tokenEnd = i + 1;
        }
        if (tokenStart < tokenEnd - diff)
        {
            var slice = new Slice(tokenStart, tokenEnd - tokenStart);
            if (!useStopWords ||
                !stopWords.Contains(
                    HashCodeGenerator.GetHashCode(
                        text.Slice(slice))))
                tokens.Add(slice);
        }
        return tokens;
    }

    /// <summary>
    /// Enumerates the slices of the given text, where each slice represents a token.
    /// Tokens are determined based on the settings for minimum token length and whether digits are included.
    /// Optionally filters out tokens that match stop words.
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
        var useStopWords = UseStopWords;
        var stopWords = StopWords;
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
            {
                var slice = new Slice(tokenStart, i - tokenStart);
                if (!useStopWords ||
                    !stopWords.Contains(
                        HashCodeGenerator.GetHashCode(
                            text.Slice(slice))))
                    yield return slice;
            }
            tokenStart = i + 1;
            tokenEnd = i + 1;
        }
        if (tokenStart < tokenEnd - diff)
        {
            var slice = new Slice(tokenStart, tokenEnd - tokenStart);
            if (!useStopWords ||
                !stopWords.Contains(
                    HashCodeGenerator.GetHashCode(
                        text.Slice(slice))))
                yield return slice;
        }
    }
}
