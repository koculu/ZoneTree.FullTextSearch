using System.Threading;
using Tenray.ZoneTree;
using Tenray.ZoneTree.Comparers;
using Tenray.ZoneTree.PresetTypes;
using Tenray.ZoneTree.Serializers;
using ZoneTree.FullTextSearch.Model;
using ZoneTree.FullTextSearch.QueryLanguage;
using ZoneTree.FullTextSearch.Search;

namespace ZoneTree.FullTextSearch.Index;

/// <summary>
/// Represents an index structure for managing records associated with hashed tokens,
/// with support for an optional secondary index and token-based searching.
/// </summary>
/// <typeparam name="TRecord">The type of the records managed by the index. Must be an unmanaged type.</typeparam>
/// <typeparam name="TToken">The type of the tokens used for indexing. Must be an unmanaged type.</typeparam>
public sealed class IndexOfTokenRecordPreviousToken<TRecord, TToken>
    : IDisposable
    where TRecord : unmanaged
    where TToken : unmanaged
{
    readonly bool useSecondaryIndex;

    bool isDropped;

    bool isDisposed;

    readonly SearchOnIndexOfTokenRecordPreviousToken<TRecord, TToken>
        searchAlgorithm;

    readonly AdvancedSearchOnIndexOfTokenRecordPreviousToken<TRecord, TToken>
        advancedSearchAlgorithm;

    /// <summary>
    /// Gets the primary zone tree used to store and retrieve records by token and previous token.
    /// </summary>
    public readonly IZoneTree<
        CompositeKeyOfTokenRecordPrevious<TRecord, TToken>,
        byte> ZoneTree1;

    /// <summary>
    /// Gets the maintainer for managing the primary zone tree, including background tasks.
    /// </summary>
    public readonly IMaintainer Maintainer1;

    /// <summary>
    /// Gets the secondary zone tree used to store and retrieve records by record and token,
    /// if a secondary index is enabled.
    /// </summary>
    public readonly IZoneTree<
        CompositeKeyOfRecordToken<TRecord, TToken>,
        byte> ZoneTree2;

    /// <summary>
    /// Gets the maintainer for managing the secondary zone tree, including background tasks.
    /// </summary>
    public readonly IMaintainer Maintainer2;

    /// <summary>
    /// Gets the ref comparer of record.
    /// </summary>
    public IRefComparer<TRecord> RecordComparer { get; }

    /// <summary>
    /// Gets the ref comparer of token.
    /// </summary>
    public IRefComparer<TToken> TokenComparer { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the index is read-only.
    /// When set to true, both the primary and secondary zone trees (if applicable) become read-only.
    /// </summary>
    public bool IsReadOnly
    {
        get => ZoneTree1.IsReadOnly || (ZoneTree2 != null && ZoneTree2.IsReadOnly);
        set
        {
            ZoneTree1.IsReadOnly = value;
            if (ZoneTree2 != null)
                ZoneTree2.IsReadOnly = value;
        }
    }

    /// <summary>
    /// Returns true if the index is dropped, otherwise false.
    /// </summary>
    public bool IsIndexDropped { get => isDropped; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexOfTokenRecordPreviousToken{TRecord, TToken}"/> class,
    /// with the option to configure primary and secondary zone trees.
    /// </summary>
    /// <param name="recordComparer">The comparer of record.</param>
    /// <param name="tokenComparer">The comparer of token.</param>
    /// <param name="useSecondaryIndex">Indicates whether a secondary index should be used to perform faster deletion.</param>
    /// <param name="dataPath">The path to the data storage, defaulting to "data".</param>
    /// <param name="configure1">Optional configuration action for the primary zone tree.</param>
    /// <param name="configure2">Optional configuration action for the secondary zone tree.</param>
    /// <param name="blockCacheLifeTimeInMilliseconds">Defines the life time of cached blocks. Default is 1 minute.</param>
    public IndexOfTokenRecordPreviousToken(
        string dataPath = "data",
        IRefComparer<TRecord> recordComparer = null,
        IRefComparer<TToken> tokenComparer = null,
        bool useSecondaryIndex = false,
        Action<
            ZoneTreeFactory<
                CompositeKeyOfTokenRecordPrevious<TRecord, TToken>,
                byte>> configure1 = null,
        Action<
            ZoneTreeFactory<
                CompositeKeyOfRecordToken<TRecord, TToken>,
                byte>> configure2 = null,
        long blockCacheLifeTimeInMilliseconds = 60_000)
    {
        if (recordComparer == null)
            recordComparer = ComponentsForKnownTypes.GetComparer<TRecord>();
        if (tokenComparer == null)
            tokenComparer = ComponentsForKnownTypes.GetComparer<TToken>();
        var factory1 = new ZoneTreeFactory<CompositeKeyOfTokenRecordPrevious<TRecord, TToken>, byte>()
            .SetDataDirectory($"{dataPath}/index1")
            .SetIsDeletedDelegate(
                (in CompositeKeyOfTokenRecordPrevious<TRecord, TToken> key, in byte value) => value == 1)
            .SetMarkValueDeletedDelegate((ref byte x) => x = 1)
            .SetKeySerializer(new StructSerializer<CompositeKeyOfTokenRecordPrevious<TRecord, TToken>>())
            .SetComparer(
                new CompositeKeyOfTokenRecordPreviousComparer<TRecord, TToken>(
                    recordComparer,
                    tokenComparer));

        configure1?.Invoke(factory1);

        ZoneTree1 = factory1.OpenOrCreate();
        Maintainer1 = ZoneTree1.CreateMaintainer();
        Maintainer1.InactiveBlockCacheCleanupInterval = TimeSpan.FromSeconds(30);
        Maintainer1.BlockCacheLifeTime =
            TimeSpan.FromMilliseconds(blockCacheLifeTimeInMilliseconds);
        Maintainer1.EnableJobForCleaningInactiveCaches = true;
        RecordComparer = recordComparer;
        TokenComparer = tokenComparer;
        this.useSecondaryIndex = useSecondaryIndex;
        if (useSecondaryIndex)
        {
            var factory2 = new ZoneTreeFactory<CompositeKeyOfRecordToken<TRecord, TToken>, byte>()
                .SetDataDirectory($"{dataPath}/index2")
                .SetIsDeletedDelegate(
                    (in CompositeKeyOfRecordToken<TRecord, TToken> key, in byte value) => value == 1)
                .SetMarkValueDeletedDelegate((ref byte x) => x = 1)
                .SetKeySerializer(new StructSerializer<CompositeKeyOfRecordToken<TRecord, TToken>>())
                .SetComparer(
                    new CompositeKeyOfRecordTokenComparer<TRecord, TToken>(
                        recordComparer,
                        tokenComparer));

            configure2?.Invoke(factory2);

            ZoneTree2 = factory2.OpenOrCreate();
            Maintainer2 = ZoneTree2.CreateMaintainer();
            Maintainer2.InactiveBlockCacheCleanupInterval = TimeSpan.FromSeconds(30);
            Maintainer2.BlockCacheLifeTime =
                TimeSpan.FromMilliseconds(blockCacheLifeTimeInMilliseconds);
            Maintainer2.EnableJobForCleaningInactiveCaches = true;
        }
        searchAlgorithm = new(this);
        advancedSearchAlgorithm = new(this);
    }

    /// <summary>
    /// Throws an exception if the index has been dropped, preventing further operations on a dropped index.
    /// </summary>
    public void ThrowIfIndexIsDropped()
    {
        if (isDropped) throw new Exception($"{nameof(
            IndexOfTokenRecordPreviousToken<TRecord, TToken>)} is dropped.");
    }

    /// <summary>
    /// Evicts data from memory to disk in both primary and secondary zone trees.
    /// </summary>
    public void EvictToDisk()
    {
        ThrowIfIndexIsDropped();
        Maintainer1.EvictToDisk();
        Maintainer2?.EvictToDisk();
    }

    /// <summary>
    /// Attempts to cancel any background threads associated with maintenance tasks for both zone trees.
    /// </summary>
    public void TryCancelBackgroundThreads()
    {
        ThrowIfIndexIsDropped();
        Maintainer1.TryCancelBackgroundThreads();
        Maintainer2?.TryCancelBackgroundThreads();
    }

    /// <summary>
    /// Waits for all background threads associated with maintenance tasks to complete for both zone trees.
    /// </summary>
    public void WaitForBackgroundThreads()
    {
        ThrowIfIndexIsDropped();
        Maintainer1.WaitForBackgroundThreads();
        Maintainer2?.WaitForBackgroundThreads();
    }

    /// <summary>
    /// Drops the index by canceling and waiting for background threads, and then destroying the zone trees.
    /// </summary>
    public void Drop()
    {
        ThrowIfIndexIsDropped();
        TryCancelBackgroundThreads();
        WaitForBackgroundThreads();
        isDropped = true;
        IsReadOnly = true;
        ZoneTree1.Maintenance.Drop();
        ZoneTree2?.Maintenance.Drop();
        ZoneTree1.Dispose();
        ZoneTree2?.Dispose();
    }

    /// <summary>
    /// Upserts a record in the primary zone tree, and optionally in the secondary zone tree if enabled.
    /// </summary>
    /// <param name="token">The token associated with the record.</param>
    /// <param name="record">The record to be upserted.</param>
    /// <param name="previousToken">The token that precedes the current token in the record.</param>
    public void UpsertRecord(TToken token, TRecord record, TToken previousToken)
    {
        ThrowIfIndexIsDropped();
        ZoneTree1.Upsert(new CompositeKeyOfTokenRecordPrevious<TRecord, TToken>()
        {
            Token = token,
            Record = record,
            PreviousToken = previousToken
        }, new());
        if (!useSecondaryIndex) return;
        var key = new CompositeKeyOfRecordToken<TRecord, TToken>()
        {
            Record = record,
            Token = token,
        };
        ZoneTree2.TryAdd(key, new(), out _);
    }

    /// <summary>
    /// Deletes a record from the primary zone tree, and optionally from the secondary zone tree if a secondary index is enabled.
    /// </summary>
    /// <param name="token">The token associated with the record to delete.</param>
    /// <param name="record">The record to be deleted.</param>
    /// <param name="previousToken">The token that precedes the current token in the record.</param>
    public void DeleteRecord(TToken token, TRecord record, TToken previousToken)
    {
        ThrowIfIndexIsDropped();
        ZoneTree1.ForceDelete(new CompositeKeyOfTokenRecordPrevious<TRecord, TToken>()
        {
            Token = token,
            Record = record,
            PreviousToken = previousToken
        });

        if (!useSecondaryIndex)
            return;

        ZoneTree2.ForceDelete(new CompositeKeyOfRecordToken<TRecord, TToken>()
        {
            Record = record,
            Token = token,
        });
    }

    /// <summary>
    /// Deletes a record from the index without using the secondary index.
    /// </summary>
    /// <param name="record">The record to delete.</param>
    /// <returns>The number of entries deleted.</returns>
    long DeleteRecordWithoutInvertedIndex(TRecord record)
    {
        using var iterator1 = ZoneTree1.CreateIterator(
            IteratorType.NoRefresh,
            contributeToTheBlockCache: false);
        var deletedEntries = 0L;
        var recordComparer = RecordComparer;
        while (iterator1.Next())
        {
            var key = iterator1.CurrentKey;
            if (recordComparer.AreNotEqual(key.Record, record)) continue;
            ZoneTree1.ForceDelete(key);
            ++deletedEntries;
        }
        return deletedEntries;
    }

    /// <summary>
    /// Deletes a record from the index, including from the secondary index if enabled.
    /// </summary>
    /// <param name="record">The record to delete.</param>
    /// <returns>The number of entries deleted.</returns>
    public long DeleteRecord(TRecord record)
    {
        ThrowIfIndexIsDropped();
        if (!useSecondaryIndex) return DeleteRecordWithoutInvertedIndex(record);
        using var iterator1 = ZoneTree1.CreateIterator(
            IteratorType.NoRefresh,
            contributeToTheBlockCache: false);
        using var iterator2 = ZoneTree2.CreateIterator(
            IteratorType.NoRefresh,
            contributeToTheBlockCache: false);
        iterator2.Seek(new()
        {
            Record = record
        });
        var recordComparer = RecordComparer;
        var deletedEntries = 0L;
        while (iterator2.Next())
        {
            var reverseKey = iterator2.CurrentKey;
            if (recordComparer.AreNotEqual(reverseKey.Record, record)) break;
            var reverseKeyToken = reverseKey.Token;
            iterator1.Seek(new()
            {
                Token = reverseKeyToken,
                Record = record
            });
            while (iterator1.Next())
            {
                var ftkey = iterator1.CurrentKey;
                if (TokenComparer.AreNotEqual(ftkey.Token, reverseKeyToken))
                    break;
                if (recordComparer.AreNotEqual(ftkey.Record, record))
                    break;
                ZoneTree1
                    .ForceDelete(ftkey);
                ++deletedEntries;
            }
            ZoneTree2?.ForceDelete(reverseKey);
        }
        return deletedEntries;
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
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. This allows the search operation to be canceled if necessary.
    /// </param>
    /// <returns>
    /// An array of records that match the specified tokens and facets, respecting the token order if specified.
    /// The array may be empty if no matching records are found.
    /// </returns>
    public TRecord[] SimpleSearch(
        ReadOnlySpan<TToken> tokens,
        TToken? firstLookAt = null,
        bool respectTokenOrder = true,
        ReadOnlySpan<TToken> facets = default,
        int skip = 0,
        int limit = 0,
        CancellationToken cancellationToken = default)
    {
        return searchAlgorithm
            .Search(tokens, firstLookAt, respectTokenOrder, facets, skip, limit, cancellationToken);
    }

    /// <summary>
    /// Performs a search based on the specified query and returns the matching records.
    /// </summary>
    /// <param name="query">The search query to execute.</param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. This allows the search operation to be canceled if necessary.
    /// </param>
    /// <returns>An array of records that match the search criteria.</returns>
    public TRecord[] Search(
        SearchQuery<TToken> query,
        CancellationToken cancellationToken = default)
    {
        return advancedSearchAlgorithm.Search(query, cancellationToken);
    }

    /// <summary>
    /// Disposes the resources used by the index.
    /// </summary>
    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        Maintainer1.WaitForBackgroundThreads();
        Maintainer1.Dispose();
        ZoneTree1.Dispose();
        if (useSecondaryIndex)
        {
            Maintainer2?.WaitForBackgroundThreads();
            Maintainer2?.Dispose();
            ZoneTree2?.Dispose();
        }
    }
}
