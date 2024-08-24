using System;
using System.Drawing;
using Tenray.ZoneTree;
using Tenray.ZoneTree.Comparers;
using Tenray.ZoneTree.Core;
using ZoneTree.FullTextSearch.Core;
using ZoneTree.FullTextSearch.Core.Index;
using ZoneTree.FullTextSearch.Core.Tokenizer;

namespace ZoneTree.FullTextSearch.SearchEngines;

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
    /// The hash code generator used to generate hash codes for tokens.
    /// </summary>
    readonly IHashCodeGenerator HashCodeGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashedSearchEngine{TRecord}"/> class.
    /// </summary>
    /// <param name="dataPath">The path to the data storage, defaulting to "data".</param>    
    /// <param name="useSecondaryIndex">Indicates whether a secondary index should be used to perform faster deletion.</param>
    /// <param name="recordComparer">The comparer used to manage references to records.</param>
    /// <param name="wordTokenizer">The tokenizer used to split words. If null, a default tokenizer is used.</param>
    /// <param name="hashCodeGenerator">The hash code generator used to generate hash codes for the tokens. If null, a default generator is used.</param>
    public HashedSearchEngine(
        string dataPath = "data",
        bool useSecondaryIndex = false,
        IWordTokenizer wordTokenizer = null,
        IRefComparer<TRecord> recordComparer = null,
        IHashCodeGenerator hashCodeGenerator = null)
    {
        HashCodeGenerator = hashCodeGenerator ?? new DefaultHashCodeGenerator();
        Index = new(
            dataPath,
            recordComparer,
            new UInt64ComparerAscending(),
            useSecondaryIndex);
        Index.FacetPreviousToken = ulong.MaxValue;
        WordTokenizer =
            wordTokenizer ??
            new WordTokenizer(hashCodeGenerator: HashCodeGenerator);
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
        Index.FacetPreviousToken = ulong.MaxValue;
        WordTokenizer = wordTokenizer ?? new WordTokenizer();
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
        var slices = WordTokenizer.EnumerateSlices(memory).ToArray();
        var len = slices.Length;
        for (int i = 0; i < len; i++)
        {
            var slice = slices[i];
            var token = HashCodeGenerator.GetHashCode(memory.Slice(slice));
            Index.UpsertRecord(token, record, previousToken);
            previousToken = token;
        }
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

    ulong GetFacetToken(string name, string value)
    {
        var text = $"{{${name}:{value}}}";
        var memory = text.AsMemory();
        return HashCodeGenerator.GetHashCode(memory);
    }

    ulong[] GetFacetTokens(IReadOnlyDictionary<string, string> facets)
    {
        if (facets == null) return [];
        return facets.Select(x => GetFacetToken(x.Key, x.Value)).ToArray();
    }

    /// <summary>
    /// Adds or updates a single facet for the specified record.
    /// </summary>
    /// <param name="record">The record to which the facet will be added or updated.</param>
    /// <param name="name">The name of the facet (e.g., "category", "author").</param>
    /// <param name="value">The value of the facet (e.g., "books", "John Doe").</param>
    public void AddFacet(TRecord record, string name, string value)
    {
        var token = GetFacetToken(name, value);
        Index.UpsertRecord(token, record, Index.FacetPreviousToken);
    }

    /// <summary>
    /// Deletes a specific facet associated with the specified record from the index.
    /// </summary>
    /// <param name="record">The record from which the facet will be deleted.</param>
    /// <param name="name">The name of the facet to delete (e.g., "category", "author").</param>
    /// <param name="value">The value of the facet to delete (e.g., "books", "John Doe").</param>
    public void DeleteFacet(TRecord record, string name, string value)
    {
        var token = GetFacetToken(name, value);
        Index.DeleteRecord(token, record, Index.FacetPreviousToken);
    }

    /// <summary>
    /// Searches the index based on a search string, with optional token order respect and pagination.
    /// </summary>
    /// <param name="search">
    /// The search string containing the terms to look for in the index. 
    /// This string is tokenized internally to identify individual search tokens.
    /// The search terms are logically grouped using "AND", meaning all terms must be present in the matching records.
    /// If the search string is empty or null, the result will be an empty array. 
    /// Retrieving all records via the full-text index is avoided for performance reasons.
    /// In such cases, it is recommended to fetch records from the actual record source instead of using the search index.
    /// </param>
    /// <param name="respectTokenOrder">
    /// A boolean indicating whether the search should respect the order of tokens in the search string. 
    /// If true, the records must contain the tokens in the same order as they appear in the search string.
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
    /// An array of records that match the search string, respecting the token order if specified. 
    /// The array may be empty if no matching records are found.
    /// </returns>
    public TRecord[] Search(
        string search,
        bool respectTokenOrder = true,
        int skip = 0,
        int limit = 0)
    {
        if (string.IsNullOrWhiteSpace(search)) return [];
        var memory = search.AsMemory();
        var slices = WordTokenizer.GetSlices(search);
        if (slices.Count == 0) return [];
        var longestSlice = slices.MaxBy(x => x.Length);
        var longestToken = HashCodeGenerator.GetHashCode(memory.Slice(longestSlice));
        var tokens = slices
            .Select(slice => HashCodeGenerator.GetHashCode(memory.Slice(slice)))
            .ToArray();
        return Index
            .Search(tokens, longestToken, respectTokenOrder, default, skip, limit);
    }

    /// <summary>
    /// Searches the index based on a search string, with optional facet filters, token order respect, and pagination.
    /// </summary>
    /// <param name="search">
    /// The search string containing the terms to look for in the index. 
    /// This string is tokenized internally to identify individual search tokens.
    /// The search terms are logically grouped using "AND", meaning all terms must be present in the matching records.
    /// If the search string is empty or null, the search will still be performed if facets are provided.
    /// However, if both the search string and facets are empty or null, the result will be an empty array, as searching without any criteria is not supported.
    /// Retrieving all records via the full-text index is avoided for performance reasons.
    /// In such cases, it is recommended to fetch records from the actual record source instead of using the search index.
    /// </param>
    /// <param name="facets">
    /// A dictionary of facet filters where the key represents the facet field and the value represents the required facet value.
    /// The facets are logically grouped using "OR", meaning the records must match at least one of the specified facet values if any facets are provided.
    /// If the dictionary is empty or null, no facet filtering is applied, and all matching records are returned regardless of facet values.
    /// </param>
    /// <param name="respectTokenOrder">
    /// A boolean indicating whether the search should respect the order of tokens in the search string. 
    /// If true, the records must contain the tokens in the same order as they appear in the search string.
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
    /// An array of records that match the search string and facet filters, respecting the token order if specified. 
    /// The array may be empty if no matching records are found.
    /// </returns>
    public TRecord[] Search(
        string search,
        IReadOnlyDictionary<string, string> facets,
        bool respectTokenOrder = true,
        int skip = 0,
        int limit = 0)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            var facetTokens = GetFacetTokens(facets);
            if (facetTokens.Length == 0) return [];
            return Index
            .Search(
                [],
                default,
                respectTokenOrder,
                facetTokens,
                skip,
                limit);
        }
        var memory = search.AsMemory();
        var slices = WordTokenizer.GetSlices(search);
        if (slices.Count == 0) return [];
        var longestSlice = slices.MaxBy(x => x.Length);
        var longestToken = HashCodeGenerator.GetHashCode(memory.Slice(longestSlice));
        var tokens = slices
            .Select(slice => HashCodeGenerator.GetHashCode(memory.Slice(slice)))
            .ToArray();
        return Index
            .Search(
                tokens,
                longestToken,
                respectTokenOrder,
                GetFacetTokens(facets),
                skip,
                limit);
    }

    /// <summary>
    /// Drops the search engine.
    /// </summary>
    public void Drop()
    {
        Index.Drop();
    }

    bool isDisposed = false;

    /// <summary>
    /// Disposes the resources used by the search engine.
    /// </summary>
    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        Index.IsReadOnly = true;
        Index.WaitForBackgroundThreads();
        Index.Dispose();
    }
}
