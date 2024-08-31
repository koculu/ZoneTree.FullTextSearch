using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoneTree.FullTextSearch.QueryLanguage;

/// <summary>
/// Tokenizes a query string into a sequence of tokens that can be processed by a parser.
/// </summary>
public sealed class Tokenizer
{
    /// <summary>
    /// The input string to tokenize.
    /// </summary>
    readonly string Input;

    /// <summary>
    /// The current position in the input string.
    /// </summary>
    int Position;

    /// <summary>
    /// A dictionary of recognized operators in the query language.
    /// </summary>
    static readonly Dictionary<string, Token> Operators =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "AND", new Token(TokenType.And, "AND") },
            { "OR", new Token(TokenType.Or, "OR")  },
            { "NOT", new Token(TokenType.Not, "NOT") },
            { "IN", new Token(TokenType.In, "IN") },
            // aliases
            { "&", new Token(TokenType.And, "&") },
            { "|", new Token(TokenType.Or, "|")  },
            { "-", new Token(TokenType.Not, "-")  },
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="Tokenizer"/> class with the specified input string.
    /// </summary>
    /// <param name="input">The input string to tokenize.</param>
    public Tokenizer(string input)
    {
        Input = input;
        Position = 0;
    }

    /// <summary>
    /// Tokenizes the input string into a sequence of tokens.
    /// </summary>
    /// <returns>An enumerable sequence of tokens.</returns>
    public IEnumerable<Token> Tokenize()
    {
        while (Position < Input.Length)
        {
            SkipWhitespace();

            if (Position >= Input.Length)
                yield break;

            var current = Input[Position];

            if (current == '"' || current == '\'')
            {
                yield return TokenizePhrase(current);
            }
            else if (current == ':')
            {
                yield return new Token(TokenType.Colon, ":");
                Position++;
            }
            else if (current == ',')
            {
                yield return new Token(TokenType.Comma, ",");
                Position++;
            }
            else if (current == '[')
            {
                yield return new Token(TokenType.KeywordListOpen, "[");
                Position++;
            }
            else if (current == ']')
            {
                yield return new Token(TokenType.KeywordListClose, "]");
                Position++;
            }
            else if (current == '(')
            {
                yield return new Token(TokenType.OpenParenthesis, "(");
                Position++;
            }
            else if (current == ')')
            {
                yield return new Token(TokenType.CloseParenthesis, ")");
                Position++;
            }
            else if (current == '&')
            {
                yield return Operators["&"];
                Position++;
            }
            else if (current == '|')
            {
                yield return Operators["|"];
                Position++;
            }
            else if (current == '-')
            {
                yield return Operators["-"];
                Position++;
            }
            else
            {
                yield return TokenizeWordOrOperator();
            }
        }
    }

    /// <summary>
    /// Skips over any whitespace characters in the input string.
    /// </summary>
    void SkipWhitespace()
    {
        while (Position < Input.Length && char.IsWhiteSpace(Input[Position]))
        {
            Position++;
        }
    }

    /// <summary>
    /// Tokenizes a phrase enclosed in quotes.
    /// </summary>
    /// <param name="quoteType">The character used to quote the phrase (' or ").</param>
    /// <returns>A <see cref="Token"/> representing the quoted phrase.</returns>
    Token TokenizePhrase(char quoteType)
    {
        var sb = new StringBuilder();
        Position++; // Skip the opening quote

        while (Position < Input.Length)
        {
            var current = Input[Position];

            if (current == '\\' && Position + 1 < Input.Length)
            {
                // Handle escaped character
                Position++;
                sb.Append(Input[Position]);
            }
            else if (current == quoteType)
            {
                // End of phrase
                Position++;
                return new Token(TokenType.Phrase, sb.ToString());
            }
            else
            {
                sb.Append(current);
            }

            Position++;
        }

        // Tolerate unterminated phrase.
        return new Token(TokenType.Phrase, sb.ToString());
    }

    /// <summary>
    /// Tokenizes a word or operator from the input string.
    /// </summary>
    /// <returns>A <see cref="Token"/> representing the word or operator.</returns>
    Token TokenizeWordOrOperator()
    {
        var sb = new StringBuilder();

        while (Position < Input.Length)
        {
            var current = Input[Position];

            if (current == '\\' && Position + 1 < Input.Length)
            {
                // Handle escaped character
                Position++;
                sb.Append(Input[Position]);
            }
            else if (char.IsWhiteSpace(current) || current == ':' || current == ',' ||
                     current == '(' || current == ')' || current == '[' || current == ']' ||
                     current == '&' || current == '|' || current == '-')
            {
                break;
            }
            else
            {
                sb.Append(current);
            }

            Position++;
        }

        var value = sb.ToString();
        if (Operators.TryGetValue(value, out var token))
            return token;

        return new Token(TokenType.Word, value);
    }
}
