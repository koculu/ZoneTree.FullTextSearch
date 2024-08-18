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
    /// Searches for records that match a given set of tokens. The search can be configured to respect the token order,
    /// skip a certain number of records, and limit the number of results returned.
    /// </summary>
    /// <param name="tokens">The tokens to search for within the records.</param>
    /// <param name="firstLookAt">An optional token to prioritize during the search. If null, the first token in the list is used.</param>
    /// <param name="respectTokenOrder">Indicates whether the order of tokens should be respected during the search.</param>
    /// <param name="skip">The number of records to skip in the result set.</param>
    /// <param name="limit">The maximum number of records to return. If set to 0, all matching records are returned.</param>
    /// <returns>An array of records that match the search criteria.</returns>
    public TRecord[] Search(
        ReadOnlySpan<TToken> tokens,
        TToken? firstLookAt = null,
        bool respectTokenOrder = true,
        int skip = 0,
        int limit = 0)
    {
        Index.ThrowIfIndexIsDropped();
        if (tokens.Length == 0)
            return [];
        var recordComparer = Index.RecordComparer;
        var tokenComparer = Index.TokenComparer;
        using var iterator1 = Index.ZoneTree1.CreateIterator();
        using var iterator2 = Index.ZoneTree1.CreateIterator();
        var bestToken = firstLookAt ?? tokens[0];
        var records = FindRecordsMatchingAllTokens(tokens, bestToken, skip, limit);
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

        HashSet<TRecord> FindRecordsMatchingAllTokens(
            ReadOnlySpan<TToken> tokens,
            TToken bestToken,
            int skip,
            int limit)
        {
            var records = new HashSet<TRecord>();
            iterator1.Seek(new CompositeKeyOfTokenRecordPrevious<TRecord, TToken>()
            {
                Token = bestToken,
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
                if (tokenComparer.AreNotEqual(key.Token, bestToken)) break;

                // If the record is already processed, just skip it.
                // Multiple records are common
                // since a token can appear in a document multiple times with
                // different previous token.
                if (records.Contains(record)) continue;

                if (!DoesRecordContainAllTokens(tokens, record))
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
    }
}
