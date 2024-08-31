using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoneTree.FullTextSearch.Search;

namespace ZoneTree.FullTextSearch.QueryLanguage;

/// <summary>
/// Parses a sequence of tokens into a structured search query.
/// </summary>
public sealed class Parser
{
    /// <summary>
    /// The tokens to parse.
    /// </summary>
    readonly Token[] Tokens;

    /// <summary>
    /// The current position in the token list.
    /// </summary>
    int Position;

    /// <summary>
    /// Initializes a new instance of the <see cref="Parser"/> class with a sequence of tokens.
    /// </summary>
    /// <param name="tokens">The sequence of tokens to parse.</param>
    public Parser(IEnumerable<Token> tokens)
    {
        Tokens = tokens.ToArray();
        Position = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Parser"/> class with a query string.
    /// </summary>
    /// <param name="query">The query string to tokenize and parse.</param>
    public Parser(string query)
    {
        var tokenizer = new Tokenizer(query);
        Tokens = tokenizer.Tokenize().ToArray();
    }

    /// <summary>
    /// Parses the tokens into a <see cref="SearchQuery{T}"/> object.
    /// </summary>
    /// <returns>A <see cref="SearchQuery{T}"/> representing the parsed query.</returns>
    public SearchQuery<string> Parse()
    {
        if (IsAtEnd())
            return new SearchQuery<string>(new QueryNode<string>(QueryNodeType.And));
        var rootNode = ParseExpression();
        return new SearchQuery<string>(rootNode);
    }

    /// <summary>
    /// Parses an expression starting from the current token.
    /// </summary>
    /// <param name="precedence">The minimum precedence level for the expression.</param>
    /// <returns>A <see cref="QueryNode{T}"/> representing the parsed expression.</returns>
    QueryNode<string> ParseExpression(int precedence = 0)
    {
        var left = ParseTerm();

        while (true)
        {
            if (IsAtEnd()) break;

            var operatorToken = Peek();
            var opType = operatorToken.Type;

            if (!operatorToken.IsOperator)
            {
                if (opType == TokenType.CloseParenthesis)
                    break;

                var right2 = ParseExpression();
                left = new QueryNode<string>(QueryNodeType.And, children: new[] { left, right2 });
                break;
            }

            int currentPrecedence = GetPrecedence(opType);

            if (currentPrecedence < precedence) break;

            Advance();

            if (IsAtEnd()) break; // tolerate operator in the end.

            var right = ParseExpression(currentPrecedence + 1);

            if (opType == TokenType.Not)
            {
                right = new QueryNode<string>(QueryNodeType.Not, children: new[] { right });
                left = new QueryNode<string>(QueryNodeType.And, children: new[] { left, right });
                continue;
            }

            var nodeType = opType == TokenType.And ? QueryNodeType.And : QueryNodeType.Or;
            left = new QueryNode<string>(nodeType, children: new[] { left, right });
        }

        return left;
    }

    /// <summary>
    /// Gets the precedence level for a given operator token type.
    /// </summary>
    /// <param name="tokenType">The type of the operator token.</param>
    /// <returns>The precedence level, where a higher number indicates higher precedence.</returns>
    int GetPrecedence(TokenType tokenType)
    {
        // Define precedence levels: higher number means higher precedence
        return tokenType switch
        {
            TokenType.And => 2,
            TokenType.Or => 1,
            _ => 0,
        };
    }

    /// <summary>
    /// Parses a term, which may be an IN expression, a NOT expression, or a simple factor.
    /// </summary>
    /// <returns>A <see cref="QueryNode{T}"/> representing the parsed term.</returns>
    QueryNode<string> ParseTerm()
    {
        if (Check(TokenType.In))
        {
            return ParseInExpression();
        }
        if (Match(TokenType.Not))
        {
            if (Check(TokenType.In))
            {
                return ParseNotInExpression();
            }
            var factor = ParseFactor();
            return new QueryNode<string>(QueryNodeType.Not, children: [factor]);
        }
        return ParseFactor();
    }

    /// <summary>
    /// Parses a factor, which may be a simple keyword, a phrase, a facet expression, or a nested expression.
    /// </summary>
    /// <returns>A <see cref="QueryNode{T}"/> representing the parsed factor.</returns>
    QueryNode<string> ParseFactor()
    {
        if (Match(TokenType.OpenParenthesis))
        {
            if (IsAtEnd()) throw new UnexpectedTokenException("OpenParenthesis found at the end of the query.");
            var expression = ParseExpression();
            if (IsAtEnd()) return expression; // tolerate not properly closed parenthesis.
            Consume(TokenType.CloseParenthesis, "Expect ')' after expression.");
            return expression;
        }
        else if (IsFacetExpression())
        {
            return ParseFacetExpression();
        }
        else if (IsFacetInExpression())
        {
            return ParseFacetInExpression();
        }
        else if (IsFacetNotInExpression())
        {
            return ParseFacetNotInExpression();
        }
        else if (Match(TokenType.Word, TokenType.Phrase))
        {
            var list = new List<string>();
            list.Add(Previous().Value);
            while (!IsFacetExpression() && Match(TokenType.Word, TokenType.Phrase))
            {
                list.Add(Previous().Value);
            }
            return new QueryNode<string>(QueryNodeType.And, tokens: list.ToArray(), respectTokenOrder: false);
        }
        else if (Match(TokenType.KeywordListOpen))
        {
            return ParseKeywordListExpression();
        }
        else if (Match(TokenType.Comma))
        {
            // skip comma.
            return ParseFactor();
        }
        throw new UnexpectedTokenException($"Unexpected token: {Peek().Type}");
    }

    /// <summary>
    /// Checks if the current token is the start of a facet expression.
    /// </summary>
    /// <returns><c>true</c> if the current token starts a facet expression; otherwise, <c>false</c>.</returns>
    bool IsFacetExpression()
    {
        return (Check(TokenType.Phrase) || Check(TokenType.Word)) && LookAhead(TokenType.Colon);
    }

    /// <summary>
    /// Checks if the current token is the start of a facet IN expression.
    /// </summary>
    /// <returns><c>true</c> if the current token starts a facet IN expression; otherwise, <c>false</c>.</returns>
    bool IsFacetInExpression()
    {
        return (Check(TokenType.Phrase) || Check(TokenType.Word)) && LookAhead(TokenType.In);
    }

    /// <summary>
    /// Checks if the current token is the start of a facet NOT IN expression.
    /// </summary>
    /// <returns><c>true</c> if the current token starts a facet NOT IN expression; otherwise, <c>false</c>.</returns>
    bool IsFacetNotInExpression()
    {
        return (Check(TokenType.Phrase) || Check(TokenType.Word)) &&
            LookAhead(TokenType.Not) &&
            LookAhead2(TokenType.In);
    }

    /// <summary>
    /// Determines whether the current token is a phrase or a word.
    /// </summary>
    /// <returns>The type of the token (Phrase or Word).</returns>
    TokenType PhraseOrWord()
    {
        return Check(TokenType.Phrase) ? TokenType.Phrase : TokenType.Word;
    }

    /// <summary>
    /// Parses an IN expression.
    /// </summary>
    /// <returns>A <see cref="QueryNode{T}"/> representing the parsed IN expression.</returns>
    QueryNode<string> ParseInExpression()
    {
        Consume(TokenType.In, "Expect 'IN'.");
        Consume(TokenType.KeywordListOpen, "Expect '[' after IN operator.");
        return ParseKeywordListExpression();
    }

    /// <summary>
    /// Parses a NOT IN expression.
    /// </summary>
    /// <returns>A <see cref="QueryNode{T}"/> representing the parsed NOT IN expression.</returns>
    QueryNode<string> ParseNotInExpression()
    {
        Consume(TokenType.In, "Expect 'IN' after 'NOT'.");
        Consume(TokenType.KeywordListOpen, "Expect '[' after IN operator.");
        var result = ParseKeywordListExpression();
        result.NodeType = QueryNodeType.Not;
        result.RespectTokenOrder = false;
        return result;
    }

    /// <summary>
    /// Parses a facet expression.
    /// </summary>
    /// <returns>A <see cref="QueryNode{T}"/> representing the parsed facet expression.</returns>
    QueryNode<string> ParseFacetExpression()
    {
        var name = Consume(PhraseOrWord(), "Expect a facet name.").Value;
        Consume(TokenType.Colon, "Expect ':' after facet name.");
        var value = Consume(PhraseOrWord(), "Expect a facet value.").Value;
        var facet = $"{name}:{value}";
        return new QueryNode<string>(QueryNodeType.And, tokens: [facet], isFacetNode: true);
    }

    /// <summary>
    /// Parses a facet IN expression.
    /// </summary>
    /// <returns>A <see cref="QueryNode{T}"/> representing the parsed facet IN expression.</returns>
    QueryNode<string> ParseFacetInExpression()
    {
        var name = Consume(PhraseOrWord(), "Expect a facet name.").Value;
        Consume(TokenType.In, "Expect 'IN' after facet name.");
        Consume(TokenType.KeywordListOpen, "Expect '[' after IN operator.");
        return ParseFacetValuesListExpresison(name);
    }

    /// <summary>
    /// Parses a facet NOT IN expression.
    /// </summary>
    /// <returns>A <see cref="QueryNode{T}"/> representing the parsed facet NOT IN expression.</returns>
    QueryNode<string> ParseFacetNotInExpression()
    {
        var name = Consume(PhraseOrWord(), "Expect a facet name.").Value;
        Consume(TokenType.Not, "Expect 'NOT' after facet name.");
        Consume(TokenType.In, "Expect 'IN' after facet name.");
        Consume(TokenType.KeywordListOpen, "Expect '[' after IN operator.");
        var result = ParseFacetValuesListExpresison(name);
        result.NodeType = QueryNodeType.Not;
        result.RespectTokenOrder = false;
        return result;
    }

    /// <summary>
    /// Parses a list of facet values in an IN or NOT IN expression.
    /// </summary>
    /// <param name="facetName">The name of the facet.</param>
    /// <returns>A <see cref="QueryNode{T}"/> representing the parsed facet values list.</returns>
    QueryNode<string> ParseFacetValuesListExpresison(string facetName)
    {
        var facetValues = new List<string>();
        do
        {
            var keyword = Consume(PhraseOrWord(), "Expect a word or phrase in the list.").Value;
            facetValues.Add($"{facetName}:{keyword}");
        } while (Match(TokenType.Comma));

        if (!IsAtEnd()) // Tolerate not properly closed keyword list.
            Consume(TokenType.KeywordListClose, "Expect ']' after the keyword list.");

        return new QueryNode<string>(QueryNodeType.Or, tokens: facetValues.ToArray(), isFacetNode: true);
    }

    /// <summary>
    /// Parses a list of keywords.
    /// </summary>
    /// <returns>A <see cref="QueryNode{T}"/> representing the parsed keyword list.</returns>
    QueryNode<string> ParseKeywordListExpression()
    {
        var keywords = new List<string>();
        do
        {
            var keyword = Consume(PhraseOrWord(), "Expect a word or phrase in the list.").Value;
            keywords.Add(keyword);
        } while (Match(TokenType.Comma));

        if (!IsAtEnd()) // Tolerate not properly closed keyword list.
            Consume(TokenType.KeywordListClose, "Expect ']' after the keyword list.");

        return new QueryNode<string>(QueryNodeType.Or, tokens: keywords.ToArray());
    }

    /// <summary>
    /// Consumes the current token if it matches the expected type, otherwise throws an exception.
    /// </summary>
    /// <param name="type">The expected token type.</param>
    /// <param name="errorMessage">The error message to include if the token does not match.</param>
    /// <returns>The consumed token.</returns>
    /// <exception cref="UnexpectedTokenException">Thrown if the current token does not match the expected type.</exception>
    Token Consume(TokenType type, string errorMessage)
    {
        if (Check(type)) return Advance();
        throw new UnexpectedTokenException(errorMessage);
    }

    /// <summary>
    /// Advances the parser if the current token matches any of the given types.
    /// </summary>
    /// <param name="types">The token types to match.</param>
    /// <returns><c>true</c> if a matching token was found; otherwise, <c>false</c>.</returns>
    bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the current token matches the specified type.
    /// </summary>
    /// <param name="type">The type of token to check for.</param>
    /// <returns><c>true</c> if the current token matches the specified type; otherwise, <c>false</c>.</returns>
    bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().Type == type;
    }

    /// <summary>
    /// Looks ahead to check if the next token matches the specified type.
    /// </summary>
    /// <param name="type">The type of token to check for.</param>
    /// <returns><c>true</c> if the next token matches the specified type; otherwise, <c>false</c>.</returns>
    bool LookAhead(TokenType type)
    {
        if (Position + 1 >= Tokens.Length) return false;
        return Tokens[Position + 1].Type == type;
    }

    /// <summary>
    /// Looks ahead two tokens to check if the second next token matches the specified type.
    /// </summary>
    /// <param name="type">The type of token to check for.</param>
    /// <returns><c>true</c> if the second next token matches the specified type; otherwise, <c>false</c>.</returns>
    bool LookAhead2(TokenType type)
    {
        if (Position + 2 >= Tokens.Length) return false;
        return Tokens[Position + 2].Type == type;
    }

    /// <summary>
    /// Advances to the next token.
    /// </summary>
    /// <returns>The token that was just consumed.</returns>
    Token Advance()
    {
        if (!IsAtEnd()) Position++;
        return Previous();
    }

    /// <summary>
    /// Checks if the parser has reached the end of the token list.
    /// </summary>
    /// <returns><c>true</c> if there are no more tokens to parse; otherwise, <c>false</c>.</returns>
    bool IsAtEnd()
    {
        return Position >= Tokens.Length;
    }

    /// <summary>
    /// Peeks at the current token without advancing the parser.
    /// </summary>
    /// <returns>The current token.</returns>
    Token Peek()
    {
        return Tokens[Position];
    }

    /// <summary>
    /// Gets the previous token in the token list.
    /// </summary>
    /// <returns>The previous token.</returns>
    Token Previous()
    {
        return Tokens[Position - 1];
    }
}
