## ZoneTree.FullTextSearch Query Language Documentation

### Grammar Definition

```bnf
<query>            ::= <expression>

<expression>       ::= <term> (("AND" | "OR" | "&" | "|") <term>)*

<term>             ::= ("NOT" | "-") <factor>
                    | <factor>
                    | <not_in_expression>
                    | <in_expression>

<factor>           ::= <keyword>
                    | <facet_expressions>
                    | <facet_in_expression>
                    | <facet_not_in_expression>
                    | "(" <expression> ")"

<facet_expressions> ::= <facet_expression> ( <facet_expression> )*

<facet_expression> ::= <single_keyword> ":" <single_keyword>

<keyword>          ::= <phrase> | <word> ( <word> )*

<keyword_list>     ::= "[" <keyword> ("," <keyword>)* "]"

<not_in_expression> ::= "NOT IN" <keyword_list>

<in_expression> ::= "IN" <keyword_list>

<facet_in_expression> ::= <keyword> "IN" <keyword_list>

<facet_not_in_expression> ::= <keyword> "NOT IN" <keyword_list>

<single_keyword>   ::= <word> | <phrase>

<word> ::= <word_char>+

<word_char> ::= [^\s:,()\[\]&|\\-]+
             | "\\" <any_char>

<phrase> ::= "\"" <word> ( <word> )* "\""
          |  "'" <word> ( <word> )* "'"
```

### Explanation of the Grammar

The grammar defines the structure of the query language, allowing you to construct complex queries with keywords, phrases, facets, logical operators, and grouping. Below is a detailed explanation of each component:

1. **`<query>`**: The starting point for parsing any query. It consists of a single `<expression>`.

2. **`<expression>`**: A combination of one or more `<term>` elements connected by logical operators (`AND`, `OR`, `&`, `|`). The operators can connect simple terms or more complex expressions.

3. **`<term>`**: A term is a fundamental unit that can:

   - Be negated using `NOT` or `-`.
   - Consist of a simple `<factor>` (e.g., a keyword or facet expressions).
   - Be a `NOT IN` expression that excludes specific keywords or phrases.
   - Be an `IN` expression that includes specific keywords or phrases.

4. **`<factor>`**: The smallest unit of a query. It can be:

   - A `<keyword>` (one or more words or a phrase).
   - A sequence of `<facet_expression>` elements (e.g., `category:electronics brand:Samsung`) that represent multiple facets without requiring `AND` between them.
   - A `<facet_in_expression>` (e.g., `category IN ["books", "electronics"]`).
   - A `<facet_not_in_expression>` (e.g., `category NOT IN ["books", "electronics"]`).
   - A nested `<expression>` enclosed in parentheses to indicate grouping.

5. **`<keyword>`**: A sequence of one or more words or a phrase. It can include multiple words without quotes (e.g., `quick fox`) or a phrase enclosed in quotes (e.g., `"quick brown fox"` or `'quick brown fox'`).

6. **`<keyword_list>`**: A comma-separated list of keywords or phrases enclosed in square brackets. It is used in `NOT IN`, `IN`, and `facet IN` expressions.

7. **`<not_in_expression>`**: A construct that excludes documents containing any keywords or phrases in the specified list. For example, `NOT IN ["fox", "cat"]`.

8. **`<in_expression>`**: A construct that includes documents containing any keywords or phrases in the specified list. For example, `IN ["fox", "cat"]`.

9. **`<facet_in_expression>`**: A construct that checks if a facet matches any keyword or phrase in the specified list. For example, `category IN ["books", "electronics"]`.

10. **`<facet_not_in_expression>`**: A construct that checks if a facet does not match any keyword or phrase in the specified list. For example, `category NOT IN ["books", "electronics"]`.

11. **`<facet_expression>`**: A basic facet matching expression. It checks if a specific facet matches a single keyword or phrase. For example, `category:books`.

12. **`<facet_expressions>`**: A sequence of multiple facet expressions that can be listed without requiring `AND` between them. For example, `category:electronics brand:Samsung`.

13. **`<single_keyword>`**: A single word or phrase, used within facet expressions.

14. **`<word>`**: The most basic element, representing a word that can contain any characters except whitespace and special characters used in the query language syntax. Backslashes (`\`) can be used to escape special characters, allowing them to be treated as part of the text. For example, `\\"`, `\,`, `\:`.

15. **`<phrase>`**: A sequence of words enclosed in double or single quotes, representing an exact match for that sequence.

### Special Characters in the Grammar

These characters are reserved for structuring the query language:

1. **Whitespace**: Used to separate keywords and operators.
2. **Double Quotes (`"`):** Used to define phrases.
3. **Single Quotes (`'`)**: Also used to define phrases.
4. **Colon (`:`):** Used in facet expressions.
5. **Square Brackets (`[` and `]`):** Used to define keyword lists.
6. **Commas (`,`):** Used as separators in lists.
7. **Parentheses (`(` and `)`):** Used for grouping expressions.
8. **Operators (`AND`, `OR`, `NOT`, `IN`, `&`, `|`, `-`):** Logical operators and keywords that influence how queries are parsed and executed.
9. **Backslash (`\`)**: Used as an escape character to include special characters like quotes, commas, colons, etc., in text.

### Explanation of the `<word>` Rule

The `<word>` rule is defined as:

```bnf
<word> ::= <word_char>+

<word_char> ::= [^\s:,()\[\]&|\\-]+
             | "\\" <any_char>
```

- **`<word>`**: A sequence of one or more `<word_char>` elements. The `+` indicates one or more repetitions.

- **`<word_char>`**:
  - The first part, `[^\s:,()\[\]&|\\-]+`, matches any sequence of characters that are **not**:
    - Whitespace characters
    - Colons (`:`)
    - Commas (`,`)
    - Parentheses (`(`, `)`)
    - Square brackets (`[`, `]`)
    - Logical operator aliases (`&`, `|`, `-`)
    - The backslash (`\`)
  - The second part, `"\\" <any_char>`, handles escaped characters by including the character immediately following the backslash, allowing special characters to be included as part of the word.

### Example Behavior:

- For input `"quick\\ fox"`, the word tokenized would be `quick fox` (with the space included as part of the word).
- For input `"quick:fox"`, the word tokenized would be `quick`.
- For input `"quick\\:"`, the word tokenized would be `quick:`.

### Summary of Tokenizer Behavior:

The `TokenizeWord` method constructs a word by including characters that do not serve as delimiters in the query language. It stops when it encounters a delimiter (e.g., whitespace, colon, comma, etc.). The backslash (`\`) is used as an escape character, allowing otherwise restricted characters to be part of the word.

### Operator Precedence

**Precedence Rule**: The query language follows the common convention where `AND` (`&`) has a higher precedence than `OR` (`|`). This means that `AND` operations are evaluated before `OR` operations when no parentheses are used to explicitly define the grouping.

- **Example with `AND` Precedence**:
  - Query: `cat AND dog OR fox`
  - Interpretation: `(cat AND dog) OR fox`
  - Meaning: The query returns documents that either contain both "cat" and "dog" or contain "fox".

**Parentheses for Clarity**: Users can use parentheses to override the default precedence and group expressions explicitly.

- **Example**:
  - Query: `cat AND (dog OR fox)`
  - Interpretation: The query returns documents that contain "cat" and either "dog" or "fox".

### Operator Aliases

The query language supports the use of shorthand aliases for the logical operators `AND`, `OR`, and `NOT`:

| Alias | Operator | Description                                  | Example      | Equivalent    |
| ----- | -------- | -------------------------------------------- | ------------ | ------------- |
| `&`   | `AND`    | Requires that both conditions must be true.  | `cat & dog`  | `cat AND dog` |
| `\|`  | `OR`     | Specifies that either condition can be true. | `cat \| dog` | `cat OR dog`  |
| `-`   | `NOT`    | Excludes a condition.                        | `-dog`       | `NOT dog`     |

### Sample Queries

Below are sample queries that match the grammar, demonstrating various features and combinations:

| **Query Type**                              | **Sample Query**                                                                     | **Description**                                                                                                                            |
| ------------------------------------------- | ------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------ |
| **Simple Keyword Query**                    | `cat dog`                                                                            | Searches for documents containing both "cat" and "dog" as separate keywords.                                                               |
| **Phrase Query**                            | `"quick brown fox"`                                                                  | Searches for the exact phrase "quick brown fox".                                                                                           |
| **Phrase Query with single quote**          | `'quick brown fox'`                                                                  | Searches for the exact phrase "quick brown fox".                                                                                           |
| **Simple Facet Query**                      | `category:electronics`                                                               | Searches for documents in the "electronics" category.                                                                                      |
| **Multiple Facet Expressions**              | `category:electronics brand:Samsung`                                                 | Searches for documents in the "electronics" category where the brand is "Samsung".                                                         |
| **Facet with Phrase**                       | `title:"The Great Gatsby"`                                                           | Searches for documents where the title is exactly "The Great Gatsby".                                                                      |
| **Facet IN List Query**                     | `category IN ["books", "electronics", "furniture"]`                                  | Searches for documents in either the "books", "electronics", or "furniture" categories.                                                    |
| **Negation Query**                          | `NOT cat`                                                                            | Searches for documents that do not contain the keyword "cat".                                                                              |
| **Negation with Phrase**                    | `NOT "lazy dog"`                                                                     | Searches for documents that do not contain the exact phrase "lazy dog".                                                                    |
| **NOT IN Keyword List Query**               | `NOT IN [cow, "lazy dog", fox]`                                                      | Searches for documents that do not contain any of the following: "cow", "lazy dog", or "fox".                                              |
| **IN Keyword List Query**                   | `IN [cat, dog, fox]`                                                                 | Searches for documents that contain any of the following: "cat", "dog", or "fox".                                                          |
| **Facet NOT IN Query**                      | `category NOT IN ["books", "furniture"]`                                             | Searches for documents where the category is neither "books" nor "furniture".                                                              |
| **Combined AND Query**                      | `cat AND dog`                                                                        | Searches for documents containing both "cat" and "dog".                                                                                    |
| **Combined OR Query**                       | `cat OR dog`                                                                         | Searches for documents containing either "cat" or "dog".                                                                                   |
| **Complex AND/OR Query**                    | `cat AND (dog OR fox)`                                                               | Searches for documents that contain "cat" and either "dog" or "fox".                                                                       |
| **Negation with AND**                       | `NOT cat AND dog`                                                                    | Searches for documents that do not contain "cat" but do contain "dog".                                                                     |
| **Negation with OR**                        | `NOT cat OR dog`                                                                     | Searches for documents that either do not contain "cat" or do contain "dog".                                                               |
| **Facet with AND**                          | `category:books AND author:"F. Scott Fitzgerald"`                                    | Searches for documents in the "books" category where the author is "F. Scott Fitzgerald".                                                  |
| **Facet IN with AND**                       | `category IN ["electronics", "appliances"] AND brand:Samsung`                        | Searches for documents in either the "electronics" or "appliances" categories where the brand is "Samsung".                                |
| **Multiple Facet Expressions without AND**  | `category:electronics brand:Sony model:TV`                                           | Searches for documents that match all the facets: category is "electronics", brand is "Sony", and model is "TV".                           |
| **Complex Nested Query**                    | `(cat OR dog) AND (NOT "lazy dog" OR fox)`                                           | Searches for documents that contain either "cat" or "dog" and also either do not contain the phrase "lazy dog" or do contain "fox".        |
| **Facet Expression with `NOT` and `IN`**    | `NOT brand IN ["Samsung", "Nokia"]`                                                  | Searches for documents where the brand is not "Samsung" or "Nokia".                                                                        |
| **Facet Expression with `IN`**              | `brand IN ["Sony", "Samsung"]`                                                       | Searches for documents where the brand is either "Sony" or "Samsung".                                                                      |
| **Combined AND with NOT and Facet**         | `NOT category IN ["books", "furniture"] AND author:"George Orwell"`                  | Searches for documents where the category is neither "books" nor "furniture" and the author is "George Orwell".                            |
| **Complex Nested Query with Facet and NOT** | `(category:electronics OR category:appliances) AND (NOT brand IN ["Sony", "LG"])`    | Searches for documents in either the "electronics" or "appliances" categories but excludes those where the brand is either "Sony" or "LG". |
| **Multiple Keywords Without Operators**     | `quick brown fox`                                                                    | Searches for documents containing all three keywords "quick", "brown", and "fox".                                                          |
| **Multiple Facets in Complex Query**        | `(category:books OR category:electronics) AND (author:"J.K. Rowling" OR brand:Sony)` | Searches for documents in either the "books" or "electronics" categories and either authored by "J.K. Rowling" or branded by "Sony".       |

### Performance Note on Negation Queries

When using negation in your queries (e.g., `NOT`, `NOT IN`), it is crucial to include at least one positive criterion. Queries that consist only of negations will necessitate scanning the entire index, which can significantly degrade performance, especially with large datasets. To ensure efficient query execution and faster results, always include at least one positive term, facet, or expression in your query. This approach enables the search engine to narrow down the dataset before applying negation filters, optimizing performance.
