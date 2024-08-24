# ZoneTree.FullTextSearch

![License](https://img.shields.io/badge/license-MIT-blue.svg)
[![Downloads](https://img.shields.io/nuget/dt/ZoneTree.FullTextSearch)](https://www.nuget.org/packages/ZoneTree.FullTextSearch/)
![Platform](https://img.shields.io/badge/platform-.NET-blue.svg)
![Build](https://img.shields.io/badge/build-passing-brightgreen.svg)

## Overview

**ZoneTree.FullTextSearch** is an open-source library for implementing full-text search engines using the ZoneTree storage engine. The library is designed for high performance and flexibility, offering the ability to index and search text data efficiently. The first search engine implementation in this library is the **HashedSearchEngine**, which provides a fast, lightweight and reliable full-text search using hashed tokens.

## Key Features

- **Dual-Key Storage:** Leverages synchronized ZoneTrees to enable fast lookups by both records and their associated values.
- **Full-Text Search:** Implements a hashed token-based search engine, providing efficient indexing and searching capabilities.
- **Customizable:** Supports the use of custom tokenizers and comparers to tailor the search engine to specific needs.
- **Scalable:** Designed to handle large datasets with background maintenance tasks and disk-based storage to manage memory usage.

### Performance

ZoneTree.FullTextSearch is designed for high performance, ensuring that even large datasets can be indexed and queried efficiently. Here are some performance metrics from recent tests, demonstrating its capability to handle substantial workloads:

| Metric                           | Value                                  |
| -------------------------------- | -------------------------------------- |
| **Token Count**                  | 27,869,351                             |
| **Record Count**                 | 103,499                                |
| **Index Creation Time**          | 54,814 ms (approximately 54.8 seconds) |
| **Query (matching 90K records)** | 325 ms                                 |
| **Query (matching 11 records)**  | 16 ms                                  |

**Indexing Performance:**  
The library successfully indexed 27.8 million tokens across 103,499 records in just under 55 seconds, highlighting its efficiency in handling large datasets.

**Query Performance:**

- A complex query that matched 90,000 records was completed in just 325 milliseconds.
- A more specific query matching 11 records was resolved in only 16 milliseconds.

These results illustrate that ZoneTree.FullTextSearch provides quick response times, even with extensive data, making it suitable for applications requiring both scalability and speed. Whether you're dealing with vast amounts of text data or need rapid search capabilities, this library offers the performance necessary for demanding environments.

**Environment:**

```
Intel Core i7-6850K CPU 3.60GHz (Skylake), 1 CPU, 12 logical and 6 physical cores
64 GB DDR4 Memory
SSD: Samsung SSD 850 EVO 1TB
```

## Installation

To install ZoneTree.FullTextSearch, you can add the package to your project via NuGet:

```bash
dotnet add package ZoneTree.FullTextSearch
```

## Usage

### Setting Up a Search Engine

To create an instance of `HashedSearchEngine`, initialize it with the required parameters:

```csharp
using ZoneTree.FullTextSearch.SearchEngines;

// Initialize the search engine
using var searchEngine = new HashedSearchEngine<int>(
    dataPath: "data",
    useSecondaryIndex: true,
    wordTokenizer: new WordTokenizer(),
    refComparer: null
);
```

### Adding Records

To add a record to the search engine, use the `AddRecord` method:

```csharp
searchEngine.AddRecord(1, "The quick brown fox jumps over the lazy dog.");
```

### Searching Records

To perform a search, use the `Search` method:

```csharp
var results = searchEngine.Search("quick fox", respectTokenOrder: true);
```

### Deleting Records

To delete a record from the search engine:

```csharp
searchEngine.DeleteRecord(1);
```

### Pagination

In addition to basic searching, you can easily implement pagination using the `skip` and `limit` parameters in the `Search` method. This is especially useful when dealing with large datasets where you only want to display a subset of the results at a time.

```csharp
var resultsPage1 = searchEngine.Search(
    search: "quick fox",
    respectTokenOrder: true,
    skip: 0, // Skip 0 records, start from the beginning
    limit: 10 // Limit to 10 records in this page
);

var resultsPage2 = searchEngine.Search(
    search: "quick fox",
    respectTokenOrder: true,
    skip: 10, // Skip the first 10 records
    limit: 10 // Limit to 10 records in this page
);
```

### Adding Facets

To associate a facet (e.g., category, author) with a record:

```csharp
searchEngine.AddFacet(1, "category", "books");
searchEngine.AddFacet(1, "author", "John Doe");
```

### Deleting Facets

To remove a specific facet associated with a record:

```csharp
searchEngine.DeleteFacet(1, "category", "books");
searchEngine.DeleteFacet(1, "author", "John Doe");
```

### Faceted Search

If you're also filtering by facets, you can still apply pagination by specifying the `skip` and `limit` parameters:

```csharp
var facets = new Dictionary<string, string>
{
    { "category", "books" },
    { "author", "John Doe" }
};

var paginatedFacetedResultsPage1 = searchEngine.Search(
    search: "quick fox",
    facets: facets,
    respectTokenOrder: true,
    skip: 0, // Start from the first matching record
    limit: 10 // Retrieve up to 10 records
);

var paginatedFacetedResultsPage2 = searchEngine.Search(
    search: "quick fox",
    facets: facets,
    respectTokenOrder: true,
    skip: 10, // Skip the first 10 matching records
    limit: 10 // Retrieve the next 10 records
);
```

### Cleanup

When you're done using the search engine, ensure proper cleanup:

```csharp
searchEngine.Dispose();
```

## Customization

### Custom Tokenizer

You can implement your own tokenizer by adhering to the `IWordTokenizer` interface:

```csharp
public sealed class CustomTokenizer : IWordTokenizer
{
    public IReadOnlyList<Slice> GetSlices(ReadOnlySpan<char> text)
    {
        // Custom tokenization logic
    }

    public IEnumerable<Slice> EnumerateSlices(ReadOnlyMemory<char> text)
    {
        // Custom tokenization logic
    }
}
```

Then, pass the custom tokenizer to the `HashedSearchEngine`:

```csharp
var searchEngine = new HashedSearchEngine<int>(
    wordTokenizer: new CustomTokenizer()
);
```

### Stemming using a Custom Hash Generator

1. **Stemming Integration**: In the custom hash generator, you can integrate a stemming library to reduce each word to its stem before hashing.
2. **Stemming Libraries**: You can use any existing .NET stemming library, such as **PorterStemmer** or **SnowballStemmer**, within your custom hash generator to perform the stemming process.

### Example Custom Hash Generator with Stemming

Here’s a conceptual example of how you could implement a custom hash generator that incorporates stemming:

```csharp
public sealed class StemmingHashCodeGenerator : IHashCodeGenerator
{
    private readonly IStemmer Stemmer;

    public StemmingHashCodeGenerator(IStemmer stemmer)
    {
        Stemmer = stemmer;
    }

    public ulong GetHashCode(ReadOnlySpan<char> text)
    {
        return GetHashCode(text.ToString());
    }

    public ulong GetHashCode(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var stemmedWord = Stemmer.Stem(text); // Apply stemming
        return ComputeHash(stemmedWord.AsSpan());
    }

    public ulong GetHashCode(ReadOnlyMemory<char> text)
    {
        return GetHashCode(text.Span);
    }

    ulong ComputeHash(ReadOnlySpan<char> text)
    {
        if (text.IsWhiteSpace()) return 0;
        var hashedValue = 3074457345618258791ul;
        for (var i = 0; i < text.Length; i++)
        {
            hashedValue += char.ToLowerInvariant(text[i]);
            hashedValue *= 3074457345618258799ul;
        }
        return hashedValue;
    }
}
```

### Using a Stemming Library

You can use a stemming library in the custom hash generator like this:

```csharp
// Example of using a stemming library (like PorterStemmer)
var stemmer = new PorterStemmer(); // Assuming this is a class from the stemming library
var hashCodeGenerator = new StemmingHashCodeGenerator(stemmer);

var searchEngine = new HashedSearchEngine<int>(
    hashCodeGenerator: hashCodeGenerator
);
```

### Benefits

- **Improved Search Accuracy**: By stemming words, the search engine can match related terms more effectively, ensuring that different forms of a word (e.g., "run," "running," "ran") are all indexed under the same root form.
- **Flexibility**: You can swap out the stemming algorithm easily by using a different stemming library or custom implementation, depending on the specific language or domain requirements.

## Using RecordTable for Dual-Key Storage

The `RecordTable` class in ZoneTree.FullTextSearch provides a powerful dual-key storage solution where both keys can act as a lookup for the other. This is particularly useful for scenarios where you need to efficiently manage and query data based on two different keys.

### Key Features

- **Two Synchronized ZoneTrees:** `RecordTable` manages two synchronized ZoneTrees, enabling fast lookups by either the primary key (record) or the secondary key (value).
- **Background Maintenance:** Automatic background maintenance tasks to manage caches and optimize performance.
- **Ease of Use:** Simplified API for inserting, querying, and managing records with dual keys.

### Why RecordTable is Needed

The `RecordTable` is essential when you need to handle complex records that the search engine index cannot directly manage due to its reliance on unmanaged types (simple, fixed-size data types). Since the search engine is optimized for these types, it cannot store more complex data like strings or classes directly.

If your application doesn't use a separate database for managing and looking up records, `RecordTable` acts as an embedded solution, allowing you to map and store complex records efficiently. It synchronizes with the search engine, providing dual-key lookups without needing an external database, simplifying your application's architecture while maintaining high performance.

### Basic Usage

#### Initializing RecordTable

To set up a `RecordTable`, simply provide the necessary types and an optional data path for storage:

```csharp
using ZoneTree.FullTextSearch;

var recordTable = new RecordTable<int, string>(dataPath: "data/recordTable");
```

#### Inserting Records

You can insert or update records using the `UpsertRecord` method, which ensures that both keys (record and value) are synchronized across the two ZoneTrees:

```csharp
recordTable.UpsertRecord(1, "Value1");
recordTable.UpsertRecord(2, "Value2");
```

#### Retrieving Data by Primary Key

To retrieve a value associated with a given record (primary key), use the `TryGetValue` method:

```csharp
if (recordTable.TryGetValue(1, out var value))
{
    Console.WriteLine($"Record 1 has value: {value}");
}
```

#### Retrieving Data by Secondary Key

Conversely, you can retrieve a record using its associated value (secondary key) with the `TryGetRecord` method:

```csharp
if (recordTable.TryGetRecord("Value2", out var record))
{
    Console.WriteLine($"Value2 is associated with record: {record}");
}
```

#### Dropping the RecordTable

When you no longer need the `RecordTable`, you can drop it to clean up all resources and delete the data:

```csharp
recordTable.Drop();
```

#### Disposing the RecordTable

Ensure to dispose of the `RecordTable` properly to close any open resources:

```csharp
recordTable.Dispose();
```

### Example Scenario

Imagine a scenario where you're storing user profiles where the `TRecord` is a unique user ID and the `TValue` is the user's email. With `RecordTable`, you can quickly find the user ID by email or retrieve the user's email by their ID, all while ensuring data consistency between the two ZoneTrees.

```csharp
var userTable = new RecordTable<int, string>("data/users");

// Adding users
userTable.UpsertRecord(101, "user101@example.com");
userTable.UpsertRecord(102, "user102@example.com");

// Lookup by ID
if (userTable.TryGetValue(101, out var email))
{
    Console.WriteLine($"User 101's email: {email}");
}

// Lookup by email
if (userTable.TryGetRecord("user102@example.com", out var userId))
{
    Console.WriteLine($"Email user102@example.com belongs to user ID: {userId}");
}
```

This dual-key approach greatly simplifies scenarios where data needs to be accessed and managed from multiple perspectives.

### Playground Console App

The Playground Console App for ZoneTree.FullTextSearch provides an interactive environment to experiment with the library's capabilities. It allows developers to:

- **Test Indexing and Querying:** Quickly create indexes, add records, and execute queries to see real-time performance and results.
- **Explore Features:** Experiment with different search engine settings, tokenizers, and configurations to understand how they impact performance and accuracy.
- **Benchmark Performance:** Compare the performance of ZoneTree.FullTextSearch against other indexing solutions, using your own datasets.

This app serves as a valuable tool for both new users looking to learn how the library works and experienced developers who want to fine-tune their search engine settings for optimal performance in their applications.

## Contributing

Contributions to ZoneTree.FullTextSearch are welcome! Please submit pull requests or issues through the GitHub repository.

## Documentation

API Reference: TBD.
Examples: TBD.

## Roadmap

Future Search Engines: Additional search engines, such as Phrase Search Engine and Proximity Search Engine, are planned for future releases.

## License

ZoneTree.FullTextSearch is licensed under the MIT License. See the [LICENSE](https://github.com/koculu/ZoneTree.FullTextSearch/blob/main/LICENSE) file for more details.

---

This library is developed and maintained by the author of ZoneTree, [@koculu](https://github.com/koculu). For more information, visit the [GitHub Repository](https://github.com/koculu/ZoneTree.FullTextSearch).

## Acknowledgements

Special thanks to the contributors and the open-source community for their support.
