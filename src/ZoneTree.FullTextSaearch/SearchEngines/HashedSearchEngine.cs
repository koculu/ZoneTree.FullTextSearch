using Tenray.ZoneTree;
using Tenray.ZoneTree.Comparers;
using Tenray.ZoneTree.Core;
using ZoneTree.FullTextSearch.Core;
using ZoneTree.FullTextSearch.Core.Index;
using ZoneTree.FullTextSearch.Core.Tokenizer;

namespace ZoneTree.FullTextSaearch.SearchEngines;

/// <summary>
/// Represents a search engine that uses hashed tokens for fast searching and retrieval of records.
/// </summary>
/// <typeparam name="TRecord">The type of the records managed by the search engine. Must be an unmanaged type.</typeparam>
public sealed class HashedSearchEngine<TRecord> : IDisposable
    where TRecord : unmanaged
{
    /// <summary>
    /// Gets the index used by the search engine to store and retrieve records.
    /// </summary>
    public readonly IndexOfTokenRecordPreviousToken<TRecord, ulong> Index;

    /// <summary>
    /// The tokenizer used to split text into word slices for hashing.
    /// </summary>
    readonly IWordTokenizer WordTokenizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashedSearchEngine{TRecord}"/> class.
    /// </summary>
    /// <param name="dataPath">The path to the data storage, defaulting to "data".</param>    
    /// <param name="useSecondaryIndex">Indicates whether a secondary index should be used to perform faster deletion.</param>
    /// <param name="refComparer">The comparer used to manage references to records.</param>
    /// <param name="wordTokenizer">The tokenizer used to split words. If null, a default tokenizer is used.</param>
    public HashedSearchEngine(
        string dataPath = "data",
        bool useSecondaryIndex = false,
        IWordTokenizer wordTokenizer = null,
        IRefComparer<TRecord> refComparer = null)
    {
        Index = new(
            dataPath,
            refComparer,
            new UInt64ComparerAscending(),
            useSecondaryIndex
            );
        WordTokenizer = wordTokenizer ?? new WordTokenizer();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashedSearchEngine{TRecord}"/> class using an existing index.
    /// </summary>
    /// <param name="index">The pre-existing index to use for this search engine.</param>
    /// <param name="wordTokenizer">The tokenizer used to split words. If null, a default tokenizer is used.</param>
    public HashedSearchEngine(
        IndexOfTokenRecordPreviousToken<TRecord, ulong> index,
        IWordTokenizer wordTokenizer = null)
    {
        Index = index;
        WordTokenizer = wordTokenizer ?? new WordTokenizer();
    }

    /// <summary>
    /// Deletes the specified record from the index.
    /// </summary>
    /// <param name="record">The record to delete.</param>
    /// <returns>The number of records deleted.</returns>
    public long DeleteRecord(TRecord record)
    {
        return Index.DeleteRecord(record);
    }

    /// <summary>
    /// Adds a new record to the index, associating it with the hashed tokens from the provided text.
    /// </summary>
    /// <param name="record">The record to add to the index.</param>
    /// <param name="text">The text to tokenize and hash for indexing.</param>
    public void AddRecord(TRecord record, string text)
    {
        var memory = text.AsMemory();
        var previousToken = 0ul;
        foreach (var slice in WordTokenizer.EnumerateSlices(memory))
        {
            var token = HashCodeGenerator.GetHashCode(memory.Slice(slice));
            Index.UpsertRecord(token, record, previousToken);
            previousToken = token;
        }
    }

    /// <summary>
    /// Searches for records that match the hashed tokens of the provided search string.
    /// </summary>
    /// <param name="search">The search string to tokenize and hash.</param>
    /// <param name="respectTokenOrder">Indicates whether the order of tokens should be respected during the search.</param>
    /// <param name="skip">The number of records to skip in the result set.</param>
    /// <param name="limit">The maximum number of records to return.</param>
    /// <returns>An array of records that match the search criteria.</returns>
    public TRecord[] Search(
        string search,
        bool respectTokenOrder = true,
        int skip = 0,
        int limit = 0)
    {
        var memory = search.AsMemory();
        var slices = WordTokenizer.GetSlices(search);
        if (slices.Count == 0) return [];
        var longestSlice = slices.MaxBy(x => x.Length);
        var longestToken = HashCodeGenerator.GetHashCode(memory.Slice(longestSlice));
        var tokens = slices
            .Select(slice => HashCodeGenerator.GetHashCode(memory.Slice(slice)))
            .ToArray();
        return Index
            .Search(tokens, longestToken, respectTokenOrder, skip, limit);
    }

    /// <summary>
    /// Drops the search engine.
    /// </summary>
    public void Drop()
    {
        Index.Drop();
    }

    /// <summary>
    /// Disposes the resources used by the search engine.
    /// </summary>
    public void Dispose()
    {
        Index.IsReadOnly = true;
        Index.WaitForBackgroundThreads();
        Index.Dispose();
    }
}
