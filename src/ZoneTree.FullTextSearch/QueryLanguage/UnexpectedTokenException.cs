namespace ZoneTree.FullTextSearch.QueryLanguage;

/// <summary>
/// Exception thrown when an unexpected token is encountered during parsing.
/// </summary>
public sealed class UnexpectedTokenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedTokenException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UnexpectedTokenException(string message) : base(message)
    {
    }
}
