namespace ZoneTree.FullTextSearch.QueryLanguage;

/// <summary>
/// Defines the ypes of tokens that can be recognized by the tokenizer.
/// </summary>
public enum TokenType
{
    Word,
    Phrase,
    And,
    Or,
    Not,
    In,
    OpenParenthesis,
    CloseParenthesis,
    Comma,
    Colon,
    KeywordListOpen,
    KeywordListClose
}
