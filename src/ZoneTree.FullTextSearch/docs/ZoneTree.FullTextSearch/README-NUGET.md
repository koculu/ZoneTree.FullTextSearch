# ZoneTree.FullTextSearch

![License](https://img.shields.io/badge/license-MIT-blue.svg)
[![Downloads](https://img.shields.io/nuget/dt/ZoneTree.FullTextSearch)](https://www.nuget.org/packages/ZoneTree.FullTextSearch/)
![Platform](https://img.shields.io/badge/platform-.NET-blue.svg)
![Build](https://img.shields.io/badge/build-passing-brightgreen.svg)

## Overview

**ZoneTree.FullTextSearch** is an open-source library for implementing full-text search engines using the [ZoneTree](https://github.com/koculu/ZoneTree) storage engine. The library is designed for high performance and flexibility, offering the ability to index and search text data efficiently. The first search engine implementation in this library is the **HashedSearchEngine**, which provides a fast, lightweight and reliable full-text search using hashed tokens.

## Key Features

- **High Performance:** Handles millions of tokens and large datasets with lightning-fast query responses and efficient indexing.
- **Advanced Search**: Supports complex queries with Boolean operators and facets.
- **Customizable:** Supports the use of custom tokenizers, stemmers, normalizers and comparers to meet specific needs.
- **Dual-Key Storage:** Leverages synchronized ZoneTrees to enable fast lookups by both records and their associated values.
- **Scalable:** Designed to handle large datasets with background maintenance tasks and disk-based storage to manage memory usage.

### Performance

ZoneTree.FullTextSearch is designed for high performance, ensuring that even large datasets can be indexed and queried efficiently. Here are some performance metrics from recent tests, demonstrating its capability to handle substantial workloads:

| Metric                           | Value                                   |
| -------------------------------- | --------------------------------------- |
| **Token Count**                  | 27,869,351                              |
| **Record Count**                 | 103,499                                 |
| **Index Creation Time**          | 54,814 ms (approximately 54.8 seconds)  |
| **Query (matching 90K records)** | 325 ms (fetching 90K records from disk) |
| **Query (matching 11 records)**  | 16 ms (fetching 11 records from disk)   |
| **Query (matching 11 records)**  | ~0 ms (warmed-up queries)               |

**Indexing Performance:**  
The library successfully indexed 27.8 million tokens across 103,499 records in just under 55 seconds, highlighting its efficiency in handling large datasets.

**Query Performance:**

- A complex query that matched 90,000 records was completed in just 325 milliseconds, which includes the time taken to fetch the record IDs from disk storage.
- A more specific query matching 11 records was resolved in only 16 milliseconds, which includes the time taken to fetch the record IDs from disk storage.

**In-Memory Caching:**

ZoneTree's in-memory cache significantly enhances query performance. Once records are loaded into memory, subsequent queries on the same disk segment can be executed in less than a millisecond. This caching mechanism ensures that repeated access to the same data is nearly instantaneous, making the system highly responsive, even under heavy query loads.

**Conclusion:**

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
    useSecondaryIndex: false);
```

### Adding Records

To add a record to the search engine, use the `AddRecord` method. The engine automatically tokenizes and indexes the text:

```csharp
searchEngine.AddRecord(1, "The quick brown fox jumps over the lazy dog.");
```

### Searching Records

#### Simple Search

Use the `SimpleSearch` method for straightforward queries:

```csharp
var results = searchEngine.SimpleSearch("quick fox", respectTokenOrder: true);
```

#### Advanced Query Language

The search engine supports a powerful query language that allows for complex search operations. Here are some of the key features:

- **Boolean Operators**: Use `AND`, `OR`, and `NOT` to combine search terms.

  The following query searches for records that contain "quick" and "fox", or contain "lazy" but do not contain "dog":

  ```csharp
  var results = searchEngine.Search("quick AND fox OR lazy NOT dog");
  ```

- **Facet Filtering**: Filter results by specific facets such as category, author, etc.

  The following query searches for records where the category is "books" and the author is "John Doe":

  ```csharp
  var results = searchEngine.Search("category:books AND author:'John Doe'");
  ```

- **Grouping with Parentheses**: Group expressions for more complex logic.

  The following query searches for records that belong to either the "books" or "electronics" categories and are authored by "John Doe":

  ```csharp
  var results = searchEngine.Search("(category:books OR category:electronics) AND author:'John Doe'");
  ```

- **IN and NOT IN Clauses**: Use `IN` and `NOT IN` to filter records that match any or exclude all values in a list. These clauses can be applied both to faceted searches and non-faceted words:

  - **Non-faceted Words**:

    The following query searches for records containing any of the words "quick", "lazy", or "fox":

    ```csharp
    var results = searchEngine.Search("IN ['quick', 'lazy', 'fox']");
    ```

    The following query excludes records containing any of the words "dog", "cat", or "mouse":

    ```csharp
    var results = searchEngine.Search("NOT IN ['dog', 'cat', 'mouse']");
    ```

  - **Faceted Search**:

    - **IN Clause**: The following query searches for records where the category is either "books" or "electronics" and excludes records authored by either "Jane Doe" or "John Smith":

    ```csharp
    var results = searchEngine.Search("category IN ['books', 'electronics'] AND author NOT IN ['Jane Doe', 'John Smith']");
    ```

    - **NOT IN Clause**: The following query searches for records where the category is neither "books" nor "furniture", and the author is "John Doe":

    ```csharp
    var results = searchEngine.Search("category NOT IN ['books', 'furniture'] AND author:'John Doe'");
    ```

    This query will filter out any records that belong to the "books" or "furniture" categories, ensuring that only records where the author is "John Doe" and the category is not in the excluded list are returned.

### Operator Aliases

The query language supports the use of shorthand aliases for the logical operators `AND`, `OR`, and `NOT`:

| Alias | Operator | Description                                  | Example      | Equivalent    |
| ----- | -------- | -------------------------------------------- | ------------ | ------------- |
| `&`   | `AND`    | Requires that both conditions must be true.  | `cat & dog`  | `cat AND dog` |
| `\|`  | `OR`     | Specifies that either condition can be true. | `cat \| dog` | `cat OR dog`  |
| `-`   | `NOT`    | Excludes a condition.                        | `-dog`       | `NOT dog`     |

### Updating Records

To update an existing record in the search engine, you can use the `UpdateRecord` method. This method allows you to efficiently delete the old tokens and insert new tokens in a single operation, ensuring that only the tokens that have changed between the old and new text are modified.

#### Example Usage

```csharp
using ZoneTree.FullTextSearch.SearchEngines;

// Initialize the search engine
using var searchEngine = new HashedSearchEngine<int>(dataPath: "data");

// Adding a record
searchEngine.AddRecord(1, "The quick brown fox jumps over the lazy dog.");

// Updating the record
searchEngine.UpdateRecord(1, "The quick brown fox jumps over the lazy dog.", "The quick brown fox leaps over the lazy dog.");
```

#### How It Works

- **Tokenization**: The `UpdateRecord` method tokenizes both the old text and the new text.
- **Change Detection**: It identifies which tokens need to be removed (those present in the old text but not in the new text) and which need to be added (those present in the new text but not in the old text).
- **Efficient Update**: The method then deletes the outdated tokens and inserts the new tokens into the index, optimizing the update process by only modifying the necessary parts of the index.

### Deleting Records

To delete a record from the search engine, you can use one of the following methods:

1. **DeleteRecord Method**:

   - ```csharp
     searchEngine.DeleteRecord(1);
     ```
   - This method deletes the record from the search engine. If the `HashedSearchEngine` was initialized with `useSecondaryIndex: true`, it uses the secondary index to efficiently find and remove all tokens associated with the record. However, if the secondary index is **not** used (`useSecondaryIndex: false`), this method is **extremely slow** because it requires a full index scan to locate and delete every token linked to the record.

2. **DeleteTokens Method**:
   - ```csharp
     searchEngine.DeleteTokens(1, "The quick brown fox jumps over the lazy dog.");
     ```
   - This method is faster in both scenarios (with or without a secondary index) because it directly re-tokenizes the text and removes all associated tokens. The main drawback is that you must provide the text of the record, which may not always be readily available.

### Pagination

You can handle pagination using the `skip` and `limit` parameters in the `SimpleSearch` or `Search` methods:

```csharp
var advancedQuery = "the book AND category NOT IN(movie, documentary) AND author:'John Doe'";

// Retrieve the first 10 records
var resultsPage1 = searchEngine.Search(
    search: advancedQuery,
    skip: 0,  // Start from the first record
    limit: 10 // Retrieve 10 records
);

// Retrieve the next 10 records
var resultsPage2 = searchEngine.Search(
    search: advancedQuery,
    skip: 10, // Skip the first 10 records
    limit: 10 // Retrieve the next 10 records
);
```

- **`skip`**: Number of records to skip.
- **`limit`**: Number of records to retrieve.

This allows you to paginate through large datasets efficiently.

### Adding and Managing Facets

Facets allow you to categorize and filter your data more effectively:

- **Adding Facets**: Associate a facet with a record:

```csharp
searchEngine.AddFacet(1, "category", "books");
searchEngine.AddFacet(1, "author", "John Doe");
```

- **Deleting Facets**: Remove a specific facet associated with a record:

```csharp
searchEngine.DeleteFacet(1, "category", "books");
searchEngine.DeleteFacet(1, "author", "John Doe");
```

### Faceted Search

#### Simple Faceted Search

Combine text search with facet filters:

```csharp
var facets = new Dictionary<string, string>
{
    { "category", "books" },
    { "author", "John Doe" }
};

var results = searchEngine.SimpleSearch(
    search: "quick fox",
    facets: facets,
    respectTokenOrder: true
);
```

#### Advanced Facet Search

Leverage the query language for complex facet searches, combining multiple facets with different logical operators:

The following query searches for records that match either the "books" or "electronics" category and are authored by either "John Doe" or "Jane Doe". The results are then paginated to retrieve the first 10 matching records:

```csharp
var advancedQuery = "(category IN [books, electronics]) AND (author:'John Doe' OR author:'Jane Doe')";

var results = searchEngine.Search(
    search: advancedQuery,
    skip: 0,  // Pagination: start from the first matching record
    limit: 10 // Retrieve up to 10 records
);
```

### Cancellation of Searches

The HashedSearchEngine supports the ability to cancel ongoing search operations using a `CancellationToken`. This feature is useful for managing long-running searches, allowing you to return partial results if the operation is canceled.

#### Example Usage

You can create a `CancellationTokenSource` and pass its `CancellationToken` to the search methods. If the operation is canceled, it will return the results gathered up to that point.

```csharp
using System.Threading;
using ZoneTree.FullTextSearch.SearchEngines;

// Initialize the search engine
using var searchEngine = new HashedSearchEngine<int>(dataPath: "data");

// Create a CancellationTokenSource
using var cancellationTokenSource = new CancellationTokenSource();

// Start a search operation
var results = searchEngine.SimpleSearch(
    search: "quick fox",
    respectTokenOrder: true,
    cancellationToken: cancellationTokenSource.Token
);

// Cancel the search after a delay (example: 1 second)
cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(1));

// Output the results gathered before cancellation
foreach (var result in results)
{
    Console.WriteLine(result);
}
```

### Cleanup

Ensure proper cleanup when you're done using the search engine to release resources:

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

#### Example Scenario

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

## NormalizableHashCodeGenerator and DiacriticNormalizer

### Overview

The `NormalizableHashCodeGenerator` provides a flexible way to generate hash codes for text, allowing you to control case sensitivity and apply custom character normalization before hashing. The `DiacriticNormalizer` is one such normalizer that can strip diacritical marks (accents) from characters, converting them to their base forms. These features are particularly useful when you need to perform searches that should be insensitive to accents or case differences.

### Example Usage

#### Setting Up the Search Engine with Normalization

To create a search engine that normalizes diacritical marks and is case-insensitive, you can use the `NormalizableHashCodeGenerator` with the `DiacriticNormalizer`:

```csharp
using ZoneTree.FullTextSearch.Hashing;
using ZoneTree.FullTextSearch.Normalizers;
using ZoneTree.FullTextSearch.SearchEngines;

// Initialize the search engine with normalization
var diacriticNormalizer = new DiacriticNormalizer();
var hashCodeGenerator = new NormalizableHashCodeGenerator(
    charNormalizer: diacriticNormalizer,
    caseSensitive: false);

using var searchEngine = new HashedSearchEngine<int>(
    dataPath: "data",
    hashCodeGenerator: hashCodeGenerator
);

// Add records with diacritical marks
searchEngine.AddRecord(1, "Crème brûlée is delicious.");
searchEngine.AddRecord(2, "Cafe latte is popular.");
searchEngine.AddRecord(3, "Jalapeño peppers are spicy.");
```

In this example, the `DiacriticNormalizer` will convert characters like `é` to `e`, `ü` to `u`, and so on. The search engine will also treat text in a case-insensitive manner, so both "Crème" and "creme" will be treated the same.

#### Searching with Normalization

With the normalization setup, you can perform searches that are insensitive to case and diacritics:

```csharp
// Search for a term without worrying about accents or case
var results = searchEngine.Search("creme brulee");
```

This query will return results containing "Crème brûlée" despite the absence of accents in the search query.

#### Customizing the Normalizer

You can customize the `DiacriticNormalizer` by providing your own character mappings or specifying characters to exclude from normalization:

```csharp
var customCharMap = new Dictionary<char, char>
{
    {'â', 'a'}, {'ç', 'c'}, {'é', 'e'}
};

var excludeSet = new HashSet<char> { 'ç' };

var customNormalizer = new DiacriticNormalizer(
    charMap: customCharMap,
    exclude: excludeSet);

var customHashCodeGenerator = new NormalizableHashCodeGenerator(
    charNormalizer: customNormalizer,
    caseSensitive: true);

using var customSearchEngine = new HashedSearchEngine<int>(
    dataPath: "data/custom",
    hashCodeGenerator: customHashCodeGenerator
);

// Add records using the custom normalizer
customSearchEngine.AddRecord(1, "Crème brûlée");
customSearchEngine.AddRecord(2, "Café");
```

In this setup, the custom normalizer will apply the specified mappings, but characters like `ç` will be excluded from normalization, preserving their original form.

## Playground Console App

The Playground Console App for ZoneTree.FullTextSearch provides an interactive environment to experiment with the library's capabilities. It allows developers to:

- **Test Indexing and Querying:** Quickly create indexes, add records, and execute queries to see real-time performance and results.
- **Explore Features:** Experiment with different search engine settings, tokenizers, and configurations to understand how they impact performance and accuracy.
- **Benchmark Performance:** Compare the performance of ZoneTree.FullTextSearch against other indexing solutions, using your own datasets.

This app serves as a valuable tool for both new users looking to learn how the library works and experienced developers who want to fine-tune their search engine settings for optimal performance in their applications.

## Contributing

Contributions to ZoneTree.FullTextSearch are welcome! Please submit pull requests or issues through the GitHub repository.

## License

ZoneTree.FullTextSearch is licensed under the MIT License. See the [LICENSE](https://github.com/koculu/ZoneTree.FullTextSearch/blob/main/LICENSE) file for more details.

This library is developed and maintained by the author of ZoneTree, [@koculu](https://github.com/koculu). For more information, visit the [GitHub Repository](https://github.com/koculu/ZoneTree.FullTextSearch).

## Acknowledgements

Special thanks to the contributors and the open-source community for their support.
