using System;
using System.Drawing;
using Tenray.ZoneTree;
using Tenray.ZoneTree.Comparers;
using Tenray.ZoneTree.Core;
using ZoneTree.FullTextSearch;
using ZoneTree.FullTextSearch.Index;
using ZoneTree.FullTextSearch.QueryLanguage;
using ZoneTree.FullTextSearch.Search;
using ZoneTree.FullTextSearch.Tokenizer;
using ZoneTree.FullTextSearch.Hashing;
using System.Security.Cryptography;

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
    /// <param name="blockCacheLifeTimeInMilliseconds">Defines the life time of cached blocks. Default is 1 minute.</param>
    public HashedSearchEngine(
        string dataPath = "data",
        bool useSecondaryIndex = false,
        IWordTokenizer wordTokenizer = null,
        IRefComparer<TRecord> recordComparer = null,
        IHashCodeGenerator hashCodeGenerator = null,
        long blockCacheLifeTimeInMilliseconds = 60_000)
    {
        HashCodeGenerator = hashCodeGenerator ?? new DefaultHashCodeGenerator();
        Index = new(
            dataPath,
            recordComparer,
            new UInt64ComparerAscending(),
            useSecondaryIndex,
            blockCacheLifeTimeInMilliseconds: blockCacheLifeTimeInMilliseconds);
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
    /// Updates a record in the search engine by deleting tokens from the old text 
    /// and inserting tokens from the new text in a single operation.
    /// This method ensures that only the tokens that have changed between the old and new text
    /// are deleted and added to the index, optimizing the update process.
    /// </summary>
    /// <param name="record">The record to update.</param>
    /// <param name="oldText">The original text of the record that needs to be updated.</param>
    /// <param name="newText">The new text that will replace the old text in the record.</param>
    public void UpdateRecord(TRecord record, string oldText, string newText)
    {
        // Tokenize the old text and store tokens and their previous tokens
        var memory1 = (oldText ?? string.Empty).AsMemory();
        var previousToken1 = 0ul;
        var slices1 = WordTokenizer.EnumerateSlices(memory1).ToArray();
        var len1 = slices1.Length;

        // Tokenize the new text and store tokens and their previous tokens
        var memory2 = (newText ?? string.Empty).AsMemory();
        var previousToken2 = 0ul;
        var slices2 = WordTokenizer.EnumerateSlices(memory2).ToArray();
        var len2 = slices2.Length;

        // Create sets to track tokens that need to be created and deleted
        HashSet<TokenPair<ulong>> itemsInTheNewText = new();
        HashSet<TokenPair<ulong>> itemsInTheOldText = new();

        for (var i = 0; i < len2; i++)
        {
            var slice = slices2[i];
            var token = HashCodeGenerator.GetHashCode(memory2.Slice(slice));
            itemsInTheNewText.Add(new()
            {
                Token = token,
                PreviousToken = previousToken2
            });
            previousToken2 = token;
        }

        for (var i = 0; i < len1; i++)
        {
            var slice = slices1[i];
            var token = HashCodeGenerator.GetHashCode(memory1.Slice(slice));
            var item = new TokenPair<ulong>()
            {
                Token = token,
                PreviousToken = previousToken1
            };
            itemsInTheOldText.Add(item);

            // Tokens in the old text that do not appear in the new text are deleted.
            if (!itemsInTheNewText.Contains(item))
            {
                Index.DeleteRecord(token, record, previousToken1);
            }

            previousToken1 = token;
        }

        foreach (var item in itemsInTheNewText)
        {
            // Tokens in the new text that do not appear in the old text are inserted.
            if (!itemsInTheOldText.Contains(item))
            {
                Index.UpsertRecord(item.Token, record, item.PreviousToken);
            }
        }
    }

    /// <summary>
    /// Deletes a record from the index.
    /// This method removes all entries of the record from the index.
    /// If no secondary index is used, this method can be extremely slow
    /// as it requires a full index scan to remove all associated tokens.
    /// </summary>
    /// <param name="record">The record identifier to delete.</param>
    /// <returns>The number of tokens deleted.</returns>
    public long DeleteRecord(TRecord record)
    {
        return Index.DeleteRecord(record);
    }

    /// <summary>
    /// Deletes a record from the search engine by re-tokenizing its text.
    /// This method is faster than <see cref="DeleteRecord"/> in both scenarios (with or without a secondary index),
    /// but it requires the original text of the record to re-generate and delete all associated tokens.
    /// </summary>
    /// <param name="record">The record identifier to delete.</param>
    /// <param name="text">The original text of the record, used for tokenization.</param>
    /// <returns>The number of tokens deleted.</returns>
    public long DeleteTokens(TRecord record, string text)
    {
        var memory = text.AsMemory();
        var previousToken = 0ul;
        var slices = WordTokenizer.EnumerateSlices(memory).ToArray();
        var len = slices.Length;
        for (int i = 0; i < len; i++)
        {
            var slice = slices[i];
            var token = HashCodeGenerator.GetHashCode(memory.Slice(slice));
            Index.DeleteRecord(token, record, previousToken);
            previousToken = token;
        }

        return Index.DeleteRecord(record);
    }

    ulong GetFacetToken(string name, string value)
    {
        var text = $"{name}:{value}";
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
        Index.UpsertRecord(token, record, token);
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
        Index.DeleteRecord(token, record, token);
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
    public TRecord[] SimpleSearch(
        string search,
        bool respectTokenOrder = true,
        int skip = 0,
        int limit = 0,
        CancellationToken cancellationToken = default)
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
            .SimpleSearch(
            tokens, longestToken, respectTokenOrder,
            default, skip, limit, cancellationToken);
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
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. This allows the search operation to be canceled if necessary.
    /// </param>
    /// <returns>
    /// An array of records that match the search string and facet filters, respecting the token order if specified. 
    /// The array may be empty if no matching records are found.
    /// </returns>
    public TRecord[] SimpleSearch(
        string search,
        IReadOnlyDictionary<string, string> facets,
        bool respectTokenOrder = true,
        int skip = 0,
        int limit = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            var facetTokens = GetFacetTokens(facets);
            if (facetTokens.Length == 0) return [];
            return Index
            .SimpleSearch(
                [],
                default,
                respectTokenOrder,
                facetTokens,
                skip,
                limit,
                cancellationToken);
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
            .SimpleSearch(
                tokens,
                longestToken,
                respectTokenOrder,
                GetFacetTokens(facets),
                skip,
                limit,
                cancellationToken);
    }

    /// <summary>
    /// Performs a search based on the specified query and returns the matching records.
    /// </summary>
    /// <param name="query">The search query to execute.</param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. This allows the search operation to be canceled if necessary.
    /// </param>
    /// <returns>An array of records that match the search criteria.</returns>
    public TRecord[] Search(SearchQuery<string> query, CancellationToken cancellationToken = default)
    {
        var hashedQuery = HashedSearchQueryFactory
            .FromStringSearchQuery(query, HashCodeGenerator, WordTokenizer);
        return Index.Search(hashedQuery, cancellationToken);
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
        string search,
        int skip = 0,
        int limit = 0,
        CancellationToken cancellationToken = default)
    {
        var parser = new Parser(search);
        var query = parser.Parse();
        query.Limit = limit;
        query.Skip = skip;
        var hashedQuery = HashedSearchQueryFactory
            .FromStringSearchQuery(query, HashCodeGenerator, WordTokenizer);
        return Index.Search(hashedQuery, cancellationToken);
    }

    /// <summary>
    /// Drops the search engine.
    /// </summary>
    public void Drop()
    {
        Index.Drop();
    }

    bool isDisposed;

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
