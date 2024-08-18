using Tenray.ZoneTree;
using Tenray.ZoneTree.Comparers;
using Tenray.ZoneTree.PresetTypes;
using Tenray.ZoneTree.Serializers;
using ZoneTree.FullTextSearch.Core.Model;
using ZoneTree.FullTextSearch.Core.Search;

namespace ZoneTree.FullTextSearch.Core.Index;

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

    readonly bool useSecondaryIndex;

    bool isDropped = false;

    readonly SearchOnIndexOfTokenRecordPreviousToken<TRecord, TToken>
        searchAlgorithm;

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
                byte>> configure2 = null)
    {
        if (recordComparer == null)
            recordComparer = ComponentsForKnownTypes.GetComparer<TRecord>();
        if (tokenComparer == null)
            tokenComparer = ComponentsForKnownTypes.GetComparer<TToken>();
        var factory1 = new ZoneTreeFactory<CompositeKeyOfTokenRecordPrevious<TRecord, TToken>, byte>()
            .SetDataDirectory($"{dataPath}/index1")
            .SetIsValueDeletedDelegate((in byte x) => x == 1)
            .SetMarkValueDeletedDelegate((ref byte x) => x = 1)
            .SetKeySerializer(new StructSerializer<CompositeKeyOfTokenRecordPrevious<TRecord, TToken>>())
            .SetComparer(
                new CompositeKeyOfTokenRecordPreviousComparer<TRecord, TToken>(
                    recordComparer,
                    tokenComparer));

        configure1?.Invoke(factory1);

        ZoneTree1 = factory1.OpenOrCreate();
        Maintainer1 = ZoneTree1.CreateMaintainer();
        Maintainer1.EnableJobForCleaningInactiveCaches = true;
        RecordComparer = recordComparer;
        TokenComparer = tokenComparer;
        this.useSecondaryIndex = useSecondaryIndex;
        if (useSecondaryIndex)
        {
            var factory2 = new ZoneTreeFactory<CompositeKeyOfRecordToken<TRecord, TToken>, byte>()
                .SetDataDirectory($"{dataPath}/index2")
                .SetIsValueDeletedDelegate((in byte x) => x == 1)
                .SetMarkValueDeletedDelegate((ref byte x) => x = 1)
                .SetKeySerializer(new StructSerializer<CompositeKeyOfRecordToken<TRecord, TToken>>())
                .SetComparer(
                    new CompositeKeyOfRecordTokenComparer<TRecord, TToken>(
                        recordComparer,
                        tokenComparer));

            configure2?.Invoke(factory2);

            ZoneTree2 = factory2.OpenOrCreate();
            Maintainer2 = ZoneTree2.CreateMaintainer();
            Maintainer2.EnableJobForCleaningInactiveCaches = true;
        }
        searchAlgorithm = new
            SearchOnIndexOfTokenRecordPreviousToken<TRecord, TToken>(this);
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
    /// Returns true if the index is dropped, otherwise false.
    /// </summary>
    public bool IsIndexDropped { get => isDropped; }

    /// <summary>
    /// Evicts inactive data from memory to disk in both primary and secondary zone trees.
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
        ZoneTree1.Maintenance.DestroyTree();
        ZoneTree2?.Maintenance.DestroyTree();
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
        ZoneTree2.TryAdd(key, new());
    }

    /// <summary>
    /// Deletes a record from the index without using the secondary index.
    /// </summary>
    /// <param name="record">The record to delete.</param>
    /// <returns>The number of entries deleted.</returns>
    long DeleteRecordWithoutInvertedIndex(TRecord record)
    {
        using var iterator1 = ZoneTree1.CreateIterator(IteratorType.NoRefresh);
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
        using var iterator1 = ZoneTree1.CreateIterator(IteratorType.NoRefresh);
        using var iterator2 = ZoneTree2.CreateIterator(IteratorType.NoRefresh);
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
    /// Searches for records that match the provided tokens, with optional parameters for token order,
    /// skipping records, and limiting the number of results.
    /// </summary>
    /// <param name="tokens">The tokens to search for.</param>
    /// <param name="firstLookAt">An optional token to prioritize during the search.</param>
    /// <param name="respectTokenOrder">Indicates whether the order of tokens should be respected during the search.</param>
    /// <param name="skip">The number of records to skip in the result set.</param>
    /// <param name="limit">The maximum number of records to return.</param>
    /// <returns>An array of records that match the search criteria.</returns>
    public TRecord[] Search(
        ReadOnlySpan<TToken> tokens,
        TToken? firstLookAt = null,
        bool respectTokenOrder = true,
        int skip = 0,
        int limit = 0)
    {
        return searchAlgorithm
            .Search(tokens, firstLookAt, respectTokenOrder, skip, limit);
    }

    /// <summary>
    /// Disposes the resources used by the index.
    /// </summary>
    public void Dispose()
    {
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
