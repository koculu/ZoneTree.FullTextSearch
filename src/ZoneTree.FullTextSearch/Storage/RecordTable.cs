using Tenray.ZoneTree;

namespace ZoneTree.FullTextSearch;

/// <summary>
/// Provides a dual-key storage solution where both keys can act as a lookup for the other, using two synchronized ZoneTrees.
/// </summary>
/// <typeparam name="TRecord">The type of the record which must be an unmanaged type.</typeparam>
/// <typeparam name="TValue">The type of the value associated with each record.</typeparam>
public sealed class RecordTable<TRecord, TValue> : IDisposable where TRecord : unmanaged
{
    /// <summary>
    /// The primary ZoneTree used for storing records with their associated values.
    /// </summary>
    public readonly IZoneTree<TRecord, TValue> ZoneTree1;

    /// <summary>
    /// The secondary ZoneTree used for storing values with their associated records, facilitating reverse lookups.
    /// </summary>
    public readonly IZoneTree<TValue, TRecord> ZoneTree2;

    /// <summary>
    /// Maintainer for the primary ZoneTree, handles background maintenance tasks.
    /// </summary>
    public readonly IMaintainer Maintainer1;

    /// <summary>
    /// Maintainer for the secondary ZoneTree, also handles background maintenance tasks.
    /// </summary>
    public readonly IMaintainer Maintainer2;

    /// <summary>
    /// Returns true if the record table is dropped, otherwise false.
    /// </summary>
    public bool IsDropped { get => isDropped; }

    bool isDropped;

    /// <summary>
    /// Initializes a new instance of the RecordTable class, setting up the two ZoneTrees and their maintainers.
    /// </summary>
    /// <param name="dataPath">The base directory path where the data of both ZoneTrees will be stored.</param>
    /// <param name="factory1">Optional configuration action for the first ZoneTree factory.</param>
    /// <param name="factory2">Optional configuration action for the second ZoneTree factory.</param>
    /// <param name="blockCacheLifeTimeInMilliseconds">Defines the life time of cached blocks. Default is 1 minute.</param>
    public RecordTable(
        string dataPath = "data",
        Action<ZoneTreeFactory<TRecord, TValue>> factory1 = null,
        Action<ZoneTreeFactory<TValue, TRecord>> factory2 = null,
        long blockCacheLifeTimeInMilliseconds = 60_000)
    {
        var f1 = new ZoneTreeFactory<TRecord, TValue>()
            .SetDataDirectory($"{dataPath}/rectable1");
        var f2 = new ZoneTreeFactory<TValue, TRecord>()
            .SetDataDirectory($"{dataPath}/rectable2");
        factory1?.Invoke(f1);
        factory2?.Invoke(f2);
        ZoneTree1 = f1.OpenOrCreate();
        ZoneTree2 = f2.OpenOrCreate();

        Maintainer1 = ZoneTree1.CreateMaintainer();
        Maintainer2 = ZoneTree2.CreateMaintainer();

        Maintainer1.InactiveBlockCacheCleanupInterval = TimeSpan.FromSeconds(30);
        Maintainer2.InactiveBlockCacheCleanupInterval = TimeSpan.FromSeconds(30);

        Maintainer1.DiskSegmentBufferLifeTime = blockCacheLifeTimeInMilliseconds;
        Maintainer2.DiskSegmentBufferLifeTime = blockCacheLifeTimeInMilliseconds;

        Maintainer1.EnableJobForCleaningInactiveCaches = true;
        Maintainer2.EnableJobForCleaningInactiveCaches = true;
    }

    /// <summary>
    /// Upserts a record and its associated value into both ZoneTrees, ensuring synchronization between the two.
    /// </summary>
    /// <param name="record">The record to upsert.</param>
    /// <param name="value">The value associated with the record.</param>
    public void UpsertRecord(TRecord record, TValue value)
    {
        ZoneTree1.Upsert(record, value);
        ZoneTree2.Upsert(value, record);
    }

    /// <summary>
    /// Retrieves the last record from the primary ZoneTree based on the insertion order.
    /// </summary>
    /// <returns>The last record if available, otherwise null.</returns>
    public TRecord? GetLastRecord()
    {
        using var iterator = ZoneTree1.CreateReverseIterator(IteratorType.NoRefresh);
        if (iterator.Next())
            return iterator.CurrentKey;
        return null;
    }

    /// <summary>
    /// Tries to retrieve a value associated with a given record.
    /// </summary>
    /// <param name="record">The record to look up the associated value for.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified record, if the value is found.</param>
    /// <returns>true if the record is found; otherwise, false.</returns>
    public bool TryGetValue(TRecord record, out TValue value)
    {
        return ZoneTree1.TryGet(record, out value);
    }

    /// <summary>
    /// Throws an exception if the index has been dropped, preventing further operations on a dropped index.
    /// </summary>
    void ThrowIfIndexIsDropped()
    {
        if (isDropped)
            throw new Exception($"{nameof(RecordTable<TRecord, TValue>)} is dropped.");
    }

    /// <summary>
    /// Tries to retrieve a record associated with a given value.
    /// </summary>
    /// <param name="value">The value to look up the associated record for.</param>
    /// <param name="record">When this method returns, contains the record associated with the specified value, if the record is found.</param>
    /// <returns>true if the record is found; otherwise, false.</returns>
    public bool TryGetRecord(TValue value, out TRecord record)
    {
        return ZoneTree2.TryGet(value, out record);
    }

    /// <summary>
    /// Evicts data from memory to disk in both primary and secondary zone trees.
    /// </summary>
    public void EvictToDisk()
    {
        ThrowIfIndexIsDropped();
        Maintainer1.EvictToDisk();
        Maintainer2.EvictToDisk();
    }

    /// <summary>
    /// Attempts to cancel any background threads associated with maintenance tasks for both zone trees.
    /// </summary>
    public void TryCancelBackgroundThreads()
    {
        ThrowIfIndexIsDropped();
        Maintainer1.TryCancelBackgroundThreads();
        Maintainer2.TryCancelBackgroundThreads();
    }

    /// <summary>
    /// Waits for all background threads associated with maintenance tasks to complete for both zone trees.
    /// </summary>
    public void WaitForBackgroundThreads()
    {
        ThrowIfIndexIsDropped();
        Maintainer1.WaitForBackgroundThreads();
        Maintainer2.WaitForBackgroundThreads();
    }

    /// <summary>
    /// Drops the record table.
    /// </summary>
    public void Drop()
    {
        Maintainer1.TryCancelBackgroundThreads();
        Maintainer2.TryCancelBackgroundThreads();
        Maintainer1.WaitForBackgroundThreads();
        Maintainer2.WaitForBackgroundThreads();
        ZoneTree1.IsReadOnly = true;
        ZoneTree2.IsReadOnly = true;
        isDropped = true;
        ZoneTree1.Maintenance.Drop();
        ZoneTree2.Maintenance.Drop();
        ZoneTree1.Dispose();
        ZoneTree2.Dispose();
    }

    /// <summary>
    /// Disposes resources used by the ZoneTrees and their maintainers, ensuring a clean shutdown.
    /// </summary>
    public void Dispose()
    {
        Maintainer1.WaitForBackgroundThreads();
        Maintainer1.Dispose();
        Maintainer2.WaitForBackgroundThreads();
        Maintainer2.Dispose();
        ZoneTree1.IsReadOnly = true;
        ZoneTree2.IsReadOnly = true;
        ZoneTree1.Dispose();
        ZoneTree2.Dispose();
    }
}
