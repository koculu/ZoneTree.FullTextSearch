using Tenray.ZoneTree.Comparers;
using ZoneTree.FullTextSearch.Core.Index;

namespace ZoneTree.FullTextSearch.Core.Search;

/// <summary>
/// Provides a search algorithm for the <see cref="IndexOfTokenRecordPreviousToken{TRecord, TToken}"/> class.
/// This class is responsible for searching records that match a given set of tokens.
/// </summary>
/// <typeparam name="TRecord">The type of the records being searched. Must be an unmanaged type.</typeparam>
/// <typeparam name="TToken">The type of the tokens used in the search. Must be an unmanaged type.</typeparam>
public sealed class SearchOnIndexOfTokenRecordPreviousToken<TRecord, TToken>
    where TRecord : unmanaged
    where TToken : unmanaged
{
    /// <summary>
    /// Gets the index associated with this search algorithm, which is used to perform the search operations.
    /// </summary>
    public IndexOfTokenRecordPreviousToken<TRecord, TToken> Index { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchOnIndexOfTokenRecordPreviousToken{TRecord, TToken}"/> class,
    /// associating it with a specific <see cref="IndexOfTokenRecordPreviousToken{TRecord, TToken}"/>.
    /// </summary>
    /// <param name="index">The index to use for searching records.</param>
    public SearchOnIndexOfTokenRecordPreviousToken(
        IndexOfTokenRecordPreviousToken<TRecord, TToken> index)
    {
        Index = index;
    }

    /// <summary>
    /// Searches the index for records that match the specified tokens, with optional support for facets, token order respect, and pagination.
    /// </summary>
    /// <param name="tokens">
    /// A read-only span of tokens that the records must contain. This parameter is mandatory unless facets are provided.
    /// The tokens are logically grouped using "AND", meaning all tokens must be present in the matching records.
    /// If both the tokens span and the facets span are empty, the result will be an empty array, as searching without tokens and facets is not supported.
    /// Tokens can be empty if facets are provided; in this case, the search will be based solely on the facets.
    /// To retrieve records without specific search tokens or facets, consider fetching them from the actual record source instead of using the search index.
    /// </param>
    /// <param name="firstLookAt">
    /// An optional token that the search will prioritize when searching. 
    /// If not specified, the first token in the tokens span is used.
    /// </param>
    /// <param name="respectTokenOrder">
    /// A boolean indicating whether the search should respect the order of tokens in the record.
    /// If true, the records must contain the tokens in the specified order.
    /// </param>
    /// <param name="facets">
    /// An optional read-only span of tokens that can be used to filter the search results.
    /// If any facets are provided, records must contain at least one of these facet tokens to be included in the results.
    /// If the span is empty or not provided, no facet filtering is applied, and all matching records are returned regardless of facet values.
    /// </param>
    /// <param name="skip">
    /// The number of matching records to skip in the result set, useful for pagination.
    /// Defaults to 0.
    /// </param>
    /// <param name="limit">
    /// The maximum number of records to return, useful for limiting the result set size.
    /// Defaults to 0, which indicates no limit.
    /// </param>
    /// <returns>
    /// An array of records that match the specified tokens and facets, respecting the token order if specified.
    /// The array may be empty if no matching records are found.
    /// </returns>
    /// <remarks>
    /// The search process begins by identifying records that match the specified tokens. 
    /// If a `firstLookAt` token is provided, it prioritizes that token in the search. 
    /// It then filters these records based on whether they contain all the specified tokens and, if facets are provided, 
    /// whether they contain any of the facet tokens. 
    /// Pagination is supported through the `skip` and `limit` parameters.
    /// </remarks>
    public TRecord[] Search(
        ReadOnlySpan<TToken> tokens,
        TToken? firstLookAt = null,
        bool respectTokenOrder = true,
        ReadOnlySpan<TToken> facets = default,
        int skip = 0,
        int limit = 0)
    {
        Index.ThrowIfIndexIsDropped();
        if (tokens.Length == 0 && facets.Length == 0)
            return [];

        var hasTokens = tokens.Length > 0;
        var recordComparer = Index.RecordComparer;
        var tokenComparer = Index.TokenComparer;
        using var iterator1 = Index.ZoneTree1.CreateIterator();
        using var iterator2 = Index.ZoneTree1.CreateIterator();
        var facet = firstLookAt ?? (hasTokens ? tokens[0] : facets[0]);
        TToken facetPreviousToken = Index.FacetPreviousToken;
        var records = hasTokens ?
            FindRecordsMatchingAllTokens(tokens, facets, skip, limit) :
            FindRecordsMatchingAnyOfTheFacets(facets, skip, limit);
        return records.Count == 0 ? [] : records.ToArray();

        bool DoesRecordContainAllTokens(ReadOnlySpan<TToken> tokens, TRecord record)
        {
            var len = tokens.Length;
            var previousTokenDoesNotExist = true;
            var previousToken = default(TToken);
            for (var i = 0; i < len; ++i)
            {
                var token = tokens[i];
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

        bool DoesRecordContainAnyOfTheFacets(ReadOnlySpan<TToken> facets, TRecord record)
        {
            var len = facets.Length;
            if (len == 0) return true;
            var previousToken = default(TToken);
            for (var i = 0; i < len; ++i)
            {
                var token = facets[i];
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
                        tokenComparer.AreEqual(key.PreviousToken, facetPreviousToken);
                }
                if (hasRecordForCurrentToken)
                    return true;
            }
            return false;
        }

        HashSet<TRecord> FindRecordsMatchingAllTokens(
            ReadOnlySpan<TToken> tokens,
            ReadOnlySpan<TToken> facets,
            int skip,
            int limit)
        {
            var records = new HashSet<TRecord>();
            iterator1.Seek(new CompositeKeyOfTokenRecordPrevious<TRecord, TToken>()
            {
                Token = facet,
            });

            var off = 0;
            if (limit != 0)
                limit += skip;
            TRecord skipRecord = default;
            while (iterator1.Next())
            {
                var key = iterator1.CurrentKey;
                var record = key.Record;
                if (recordComparer.AreEqual(skipRecord, record)) continue;
                if (tokenComparer.AreNotEqual(key.Token, facet)) break;

                // If the record is already processed, just skip it.
                // Multiple records are common
                // since a token can appear in a document multiple times with
                // different previous token.
                if (records.Contains(record)) continue;

                if (!DoesRecordContainAllTokens(tokens, record))
                    continue;

                if (!DoesRecordContainAnyOfTheFacets(facets, record))
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
                    skipRecord = record;
                }
                ++off;
                if (limit > 0 && off == limit) break;
            }
            return records;
        }

        HashSet<TRecord> FindRecordsMatchingAnyOfTheFacets(
            ReadOnlySpan<TToken> facets,
            int skip,
            int limit)
        {
            var skipRecords = new HashSet<TRecord>();
            var records = new HashSet<TRecord>();
            var len = facets.Length;
            for (var i = 0; i < len; ++i)
            {
                var facet = facets[i];
                iterator1.Seek(new CompositeKeyOfTokenRecordPrevious<TRecord, TToken>()
                {
                    Token = facet,
                });

                var off = 0;
                if (limit != 0)
                    limit += skip;
                while (iterator1.Next())
                {
                    var key = iterator1.CurrentKey;
                    var record = key.Record;
                    if (tokenComparer.AreNotEqual(key.Token, facet)) break;
                    if (tokenComparer.AreNotEqual(key.PreviousToken, facetPreviousToken)) continue;
                    if (skipRecords.Contains(record)) continue;

                    // If the record is already processed, just skip it.
                    if (records.Contains(record)) continue;

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
    }
}
