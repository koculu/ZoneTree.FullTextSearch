using System.Drawing;
using ZoneTree.FullTextSearch.Hashing;
using ZoneTree.FullTextSearch.Tokenizer;

namespace ZoneTree.FullTextSearch.Search;

/// <summary>
/// A factory class for creating hashed search queries from string-based search queries.
/// The class provides methods to convert string tokens in a query tree to hashed tokens, 
/// enabling efficient search operations.
/// </summary>
public static class HashedSearchQueryFactory
{
    /// <summary>
    /// Converts a QueryNode<string> into a QueryNode<ulong> by hashing the tokens.
    /// </summary>
    /// <param name="node">The original query node with string tokens.</param>
    /// <param name="hashCodeGenerator">The hash code generator for converting strings to ulong hashes.</param>
    /// <param name="wordTokenizer">The word tokenizer for splitting strings into tokens.</param>
    /// <returns>A new QueryNode<ulong> with hashed tokens.</returns>
    public static QueryNode<ulong> FromStringQueryNode(
        QueryNode<string> node,
        IHashCodeGenerator hashCodeGenerator,
        IWordTokenizer wordTokenizer)
    {
        if (node.HasTokens)
        {
            var tokensOftokens = GetTokensOfTokens(
                node,
                hashCodeGenerator,
                wordTokenizer);

            if (node.NodeType == QueryNodeType.Or)
                return CreateOrNode(node, tokensOftokens);
            else
                return CreateAndOrNotNode(node, tokensOftokens);
        }
        else if (node.HasChildren)
        {
            return CreateNodeWithChildren(node, hashCodeGenerator, wordTokenizer);
        }

        // Return an empty node if no tokens or children
        return new QueryNode<ulong>(
            node.NodeType,
            null,
            null,
            node.RespectTokenOrder,
            node.IsFacetNode);
    }

    /// <summary>
    /// Tokenizes the strings in the given QueryNode and returns a jagged array of hashed tokens.
    /// </summary>
    /// <param name="node">The original query node with string tokens.</param>
    /// <param name="hashCodeGenerator">The hash code generator for converting strings to ulong hashes.</param>
    /// <param name="wordTokenizer">The word tokenizer for splitting strings into tokens.</param>
    /// <returns>A jagged array where each element is an array of hashed tokens corresponding to a string in the original node's tokens.</returns>
    static ulong[][] GetTokensOfTokens(
        QueryNode<string> node,
        IHashCodeGenerator hashCodeGenerator,
        IWordTokenizer wordTokenizer)
    {
        var isFacetNode = node.IsFacetNode;
        return node.Tokens.Select(x =>
        {
            if (isFacetNode)
            {
                return [hashCodeGenerator.GetHashCode(x.AsSpan())];
            }
            var tokens = wordTokenizer
            .GetSlices(x)
            .Select(slice =>
                hashCodeGenerator
                    .GetHashCode(x.AsSpan().Slice(slice)))
            .ToArray();
            return tokens;
        }).ToArray();
    }

    /// <summary>
    /// Converts a QueryNode<string> with child nodes into a QueryNode<ulong> by recursively processing its children.
    /// </summary>
    /// <param name="node">The original query node with string tokens and child nodes.</param>
    /// <param name="hashCodeGenerator">The hash code generator for converting strings to ulong hashes.</param>
    /// <param name="wordTokenizer">The word tokenizer for splitting strings into tokens.</param>
    /// <returns>A new QueryNode<ulong> with hashed tokens, including processed child nodes.</returns>
    static QueryNode<ulong> CreateNodeWithChildren(QueryNode<string> node, IHashCodeGenerator hashCodeGenerator, IWordTokenizer wordTokenizer)
    {
        // Recursively process child nodes
        return new QueryNode<ulong>(
            node.NodeType,
            null,
            node.Children
                .Select(x =>
                    FromStringQueryNode(
                        x, hashCodeGenerator, wordTokenizer)).ToArray(),
            node.RespectTokenOrder,
            node.IsFacetNode);
    }

    /// <summary>
    /// Converts an AND or NOT type QueryNode<string> into a QueryNode<ulong> by hashing its tokens and processing them according to the node type.
    /// </summary>
    /// <param name="node">The original query node with string tokens.</param>
    /// <param name="tokensOftokens">A jagged array of hashed tokens corresponding to the original node's tokens.</param>
    /// <returns>A new QueryNode<ulong> representing the AND or NOT logic with hashed tokens.</returns>
    static QueryNode<ulong> CreateAndOrNotNode(
        QueryNode<string> node, ulong[][] tokensOftokens)
    {
        var hasAnyTokenizedTokensLengthGreatherThanOne =
                            tokensOftokens.Any(x => x.Length > 1);

        if (!hasAnyTokenizedTokensLengthGreatherThanOne ||
            node.RespectTokenOrder || node.IsFacetNode)
        {
            // NOT and AND nodes can be collected into a single node
            return new QueryNode<ulong>(
                node.NodeType,
                tokensOftokens.SelectMany(x => x).ToArray(),
                null,
                node.RespectTokenOrder,
                node.IsFacetNode);
        }

        // If we reach here:
        // node.RespectTokenOrder always false
        // node.IsFacetNode always false
        var children = tokensOftokens
            .Select(tokenizedTokens =>
            {
                return new QueryNode<ulong>(
                    node.NodeType,
                    tokenizedTokens,
                    null,
                    true,
                    false);
            }).ToArray();

        if (children.Length == 1)
        {
            // edge case, simplify tree.
            children[0].NodeType = node.NodeType;
            return children[0];
        }

        return new QueryNode<ulong>(
            QueryNodeType.And,
            null,
            children,
            false,
            false);
    }

    /// <summary>
    /// Converts an OR type QueryNode<string> into a QueryNode<ulong> by hashing its tokens and processing them according to the node type.
    /// </summary>
    /// <param name="node">The original query node with string tokens.</param>
    /// <param name="tokensOftokens">A jagged array of hashed tokens corresponding to the original node's tokens.</param>
    /// <returns>A new QueryNode<ulong> representing the OR logic with hashed tokens.</returns>
    static QueryNode<ulong> CreateOrNode(QueryNode<string> node, ulong[][] tokensOftokens)
    {
        var hasAnyTokenizedTokensLengthGreatherThanOne =
                            tokensOftokens.Any(x => x.Length > 1);

        if (hasAnyTokenizedTokensLengthGreatherThanOne)
        {
            // Group each set of tokenized tokens with AND
            // and assign as children to the OR node
            var children = tokensOftokens
                .Select(tokenizedTokens =>
                {
                    return new QueryNode<ulong>(
                        QueryNodeType.And,
                        tokenizedTokens,
                        null,
                        true, // This should be always true, as tokenized tokens are a combined group.
                        node.IsFacetNode);
                }).ToArray();

            // edge case, simplify tree.
            if (children.Length == 1)
                return children[0];

            return new QueryNode<ulong>(
                QueryNodeType.Or,
                null,
                children,
                node.RespectTokenOrder,
                node.IsFacetNode);
        }
        return new QueryNode<ulong>(
                QueryNodeType.Or,
                tokensOftokens.SelectMany(x => x).ToArray(),
                null,
                node.RespectTokenOrder,
                node.IsFacetNode);
    }

    /// <summary>
    /// Converts a SearchQuery<string> into a SearchQuery<ulong> by tokenizing each word and hashing the tokens in the query nodes.
    /// </summary>
    /// <param name="query">The original search query with string tokens.</param>
    /// <param name="hashCodeGenerator">The hash code generator for converting strings to ulong hashes.</param>
    /// <param name="wordTokenizer">The word tokenizer for splitting strings into tokens.</param>
    /// <returns>A new SearchQuery<ulong> with hashed tokens.</returns>
    public static SearchQuery<ulong> FromStringSearchQuery(
        SearchQuery<string> query,
        IHashCodeGenerator hashCodeGenerator,
        IWordTokenizer wordTokenizer)
    {
        var node = FromStringQueryNode(
            query.QueryNode, hashCodeGenerator, wordTokenizer);
        var result = new SearchQuery<ulong>(node, query.Skip, query.Limit);
        return result;
    }
}
