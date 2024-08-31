namespace ZoneTree.FullTextSearch.QueryLanguage;

/// <summary>
/// Represents a token in the query language.
/// </summary>
public sealed class Token
{
    /// <summary>
    /// Gets the type of the token.
    /// </summary>
    public TokenType Type { get; }

    /// <summary>
    /// Gets the value of the token.
    /// </summary
    public string Value { get; }

    /// <summary>
    /// Determines whether the token is an operator (AND, OR, NOT).
    /// </summary>
    public bool IsOperator =>
        Type == TokenType.And ||
        Type == TokenType.Or ||
        Type == TokenType.Not;

    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> class with the specified type and value.
    /// </summary>
    /// <param name="type">The type of the token.</param>
    /// <param name="value">The value of the token.</param>
    public Token(TokenType type, string value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>
    /// Returns a string that represents the current token.
    /// </summary>
    /// <returns>A string in the format "Type: Value".</returns>
    public override string ToString()
    {
        return $"{Type}: {Value}";
    }
}
