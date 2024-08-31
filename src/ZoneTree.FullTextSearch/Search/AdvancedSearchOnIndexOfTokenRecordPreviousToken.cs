using System.Threading;
using Tenray.ZoneTree;
using Tenray.ZoneTree.Comparers;
using ZoneTree.FullTextSearch.Index;

namespace ZoneTree.FullTextSearch.Search;

/// <summary>
/// Provides an advanced search algorithm for the <see cref="IndexOfTokenRecordPreviousToken{TRecord, TToken}"/> class.
/// This class is responsible for searching records that match a given set of tokens.
/// </summary>
/// <typeparam name="TRecord">The type of the records being searched. Must be an unmanaged type.</typeparam>
/// <typeparam name="TToken">The type of the tokens used in the search. Must be an unmanaged type.</typeparam>
public sealed class AdvancedSearchOnIndexOfTokenRecordPreviousToken<TRecord, TToken>
    where TRecord : unmanaged
    where TToken : unmanaged
{
    /// <summary>
    /// Gets the index on which the search operations are performed.
    /// </summary>
    public IndexOfTokenRecordPreviousToken<TRecord, TToken> Index { get; }

    /// <summary>
    /// Represents information about a token, including whether it is a facet.
    /// </summary>
    /// <param name="Token">The token associated with this information.</param>
    /// <param name="IsFacet">Indicates whether the token is a facet.</param>
    readonly record struct TokenInfo(TToken Token, bool IsFacet);

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedSearchOnIndexOfTokenRecordPreviousToken{TRecord, TToken}"/> class.
    /// </summary>
    /// <param name="index">The index on which the search operations are performed.</param>
    public AdvancedSearchOnIndexOfTokenRecordPreviousToken(
        IndexOfTokenRecordPreviousToken<TRecord, TToken> index)
    {
        Index = index;
    }

    /// <summary>
    /// Finds and returns the list of tokens that need to be iterated over based on the query node's type and structure.
    /// </summary>
    /// <param name="node">The query node to analyze.</param>
    /// <returns>A read-only list of tokens that need iteration.</returns>    
    IReadOnlyList<TokenInfo> FindTokensNeedIteration(
        QueryNode<TToken> node)
    {
        var result = new List<TokenInfo>();
        if (node.NodeType == QueryNodeType.And)
        {
            if (node.HasTokens)
            {
                result.Add(new TokenInfo(
                    node.FirstLookAt, node.IsFacetNode));
            }
            else if (node.HasChildren)
            {
                var tokens = node.Children
                    .Select(x => FindTokensNeedIteration(x))
                    .Where(x => x.Count > 0)
                    .MinBy(x => x.Count);
                if (tokens != null)
                    result.AddRange(tokens);
            }
        }
        else if (node.NodeType == QueryNodeType.Or)
        {
            if (node.HasTokens)
            {
                result.AddRange(node.Tokens.Select(x => new TokenInfo(
                    x, node.IsFacetNode)));
            }
            else if (node.HasChildren)
            {
                // NOT queries in an OR node ends up with full index scan
                if (node.Children.Any(x => x.NodeType == QueryNodeType.Not))
                    return result;
                var tokensArray = node.Children
                    .Select(x => FindTokensNeedIteration(x)).ToArray();
                foreach (var tokens in tokensArray)
                    result.AddRange(tokens);
            }
        }
        return result;
    }

    /// <summary>
    /// Performs a search based on the specified query and returns the matching records.
    /// </summary>
    /// <param name="query">The search query to execute.</param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. This allows the search operation to be canceled if necessary.
    /// </param>
    /// <returns>An array of records that match the search criteria.</returns>
    public TRecord[] Search(SearchQuery<TToken> query, CancellationToken cancellationToken = default)
    {
        Index.ThrowIfIndexIsDropped();

        if (query.IsEmpty)
            return [];

        var recordComparer = Index.RecordComparer;
        var tokenComparer = Index.TokenComparer;
        using var iterator1 = Index.ZoneTree1.CreateIterator(
            IteratorType.NoRefresh,
            contributeToTheBlockCache: false);
        using var iterator2 = Index.ZoneTree1.CreateIterator(
            IteratorType.NoRefresh,
            contributeToTheBlockCache: false);

        if (!query.HasAnyPositiveCriteria)
        {
            var result = ProcessEntireIndex(query.Skip, query.Limit);
            return result.Count == 0 ? [] : result.ToArray();
        }

        var tokens = FindTokensNeedIteration(query.QueryNode);
        if (tokens.Count == 0)
            return [];

        var records = ProcessAllTokens(query.Skip, query.Limit);
        return records.Count == 0 ? [] : records.ToArray();

        bool DoesRecordContainAllTokens(
            ReadOnlySpan<TToken> tokens,
            TRecord record,
            bool respectTokenOrder,
            bool isFacetNode)
        {
            var len = tokens.Length;
            if (len == 0) return false;
            var previousTokenDoesNotExist = !isFacetNode;
            var previousToken = default(TToken);
            if (isFacetNode) respectTokenOrder = false;
            for (var i = 0; i < len; ++i)
            {
                var token = tokens[i];
                if (isFacetNode)
                    previousToken = token;
                iterator2.Seek(new CompositeKeyOfTokenRecordPrevious<TRecord, TToken>()
                {
                    Token = token,
                    Record = record,
                    PreviousToken = previousToken
                });
                var hasRecordForCurrentToken = false;
                if (iterator2.Next())
                {
                    var key = iterator2.CurrentKey;
                    hasRecordForCurrentToken =
                        tokenComparer.AreEqual(key.Token, token) &&
                        recordComparer.AreEqual(key.Record, record) &&
                        (previousTokenDoesNotExist ||
                        tokenComparer.AreEqual(key.PreviousToken, previousToken));
                }
                if (!hasRecordForCurrentToken)
                    return false;
                if (respectTokenOrder)
                {
                    previousTokenDoesNotExist = false;
                    previousToken = token;
                }
            }
            return true;
        }

        bool DoesRecordContainAnyOfTheTokens(
            ReadOnlySpan<TToken> tokens,
            TRecord record,
            bool isFacet)
        {
            var len = tokens.Length;
            if (len == 0) return false;
            for (var i = 0; i < len; ++i)
            {
                var token = tokens[i];
                iterator2.Seek(new CompositeKeyOfTokenRecordPrevious<TRecord, TToken>()
                {
                    Token = token,
                    Record = record,
                    PreviousToken = isFacet ? token : default
                });
                var hasRecordForCurrentToken = false;
                if (iterator2.Next())
                {
                    var key = iterator2.CurrentKey;
                    hasRecordForCurrentToken =
                        tokenComparer.AreEqual(key.Token, token) &&
                        recordComparer.AreEqual(key.Record, record) &&
                        (!isFacet ||
                        tokenComparer.AreEqual(key.PreviousToken, token));
                }
                if (hasRecordForCurrentToken)
                    return true;
            }
            return false;
        }

        bool DoesRecordMatchesTheQuery(
            QueryNode<TToken> node,
            TRecord record)
        {
            if (node.NodeType == QueryNodeType.And)
            {
                if (node.HasTokens)
                {
                    // if is facet node, process facets.
                    return DoesRecordContainAllTokens(
                        node.Tokens, record, node.RespectTokenOrder, node.IsFacetNode);
                }
                else if (node.HasChildren)
                {
                    return node.Children
                        .All(x => DoesRecordMatchesTheQuery(x, record));
                }
            }
            else if (node.NodeType == QueryNodeType.Or)
            {
                if (node.HasTokens)
                {
                    return DoesRecordContainAnyOfTheTokens(
                        node.Tokens, record, node.IsFacetNode);
                }
                else if (node.HasChildren)
                {
                    return node.Children
                        .Any(x => DoesRecordMatchesTheQuery(x, record));
                }
            }
            else if (node.NodeType == QueryNodeType.Not)
            {
                if (node.HasTokens)
                {
                    if (node.IsFacetNode)
                    {
                        return !DoesRecordContainAnyOfTheTokens(node.Tokens, record, true);
                    }
                    else if (node.RespectTokenOrder)
                    {
                        return !DoesRecordContainAllTokens(node.Tokens, record, true, false);
                    }
                    else
                    {
                        return !DoesRecordContainAnyOfTheTokens(node.Tokens, record, false);
                    }
                }
                else if (node.HasChildren)
                {
                    return node.Children
                        .All(x => !DoesRecordMatchesTheQuery(x, record));
                }
            }
            return false;
        }

        HashSet<TRecord> ProcessAllTokens(
            int skip,
            int limit)
        {
            var skipRecords = new HashSet<TRecord>();
            var records = new HashSet<TRecord>();
            var len = tokens.Count;
            for (var i = 0; i < len; ++i)
            {
                (var token, var isFacet) = tokens[i];
                iterator1.Seek(new CompositeKeyOfTokenRecordPrevious<TRecord, TToken>()
                {
                    Token = token,
                });

                var off = 0;
                if (limit != 0)
                    limit += skip;
                while (iterator1.Next())
                {
                    if (cancellationToken.IsCancellationRequested) return records;
                    var key = iterator1.CurrentKey;
                    var record = key.Record;
                    if (tokenComparer.AreNotEqual(key.Token, token)) break;
                    if (isFacet && tokenComparer.AreNotEqual(key.PreviousToken, token)) continue;
                    if (skipRecords.Contains(record)) continue;

                    // If the record is already processed, just skip it.
                    if (records.Contains(record)) continue;

                    if (!DoesRecordMatchesTheQuery(query.QueryNode, record))
                        continue;

                    if (off >= skip)
                    {
                        records.Add(record);
                    }
                    else
                    {
                        // if the current offset is skipped, we have to skip
                        // all records in the index to ensure
                        // the previously skipped records are excluded from the result.
                        skipRecords.Add(record);
                    }
                    ++off;
                    if (limit > 0 && off == limit) break;
                }
                if (limit > 0 && off == limit) break;
            }
            return records;
        }

        HashSet<TRecord> ProcessEntireIndex(
           int skip,
           int limit)
        {
            var skipRecords = new HashSet<TRecord>();
            var records = new HashSet<TRecord>();
            var off = 0;
            if (limit != 0)
                limit += skip;
            while (iterator1.Next())
            {
                if (cancellationToken.IsCancellationRequested) return records;
                var key = iterator1.CurrentKey;
                var record = key.Record;
                if (skipRecords.Contains(record)) continue;

                // If the record is already processed, just skip it.
                if (records.Contains(record)) continue;

                if (!DoesRecordMatchesTheQuery(query.QueryNode, record))
                    continue;

                if (off >= skip)
                {
                    records.Add(record);
                }
                else
                {
                    // if the current offset is skipped, we have to skip
                    // all records in the index to ensure
                    // the previously skipped records are excluded from the result.
                    skipRecords.Add(record);
                }
                ++off;
                if (limit > 0 && off == limit) break;
            }
            return records;
        }
    }
}