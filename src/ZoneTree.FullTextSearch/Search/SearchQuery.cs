
namespace ZoneTree.FullTextSearch.Search;

/// <summary>
/// Represents a search query containing a root query node and pagination parameters.
/// </summary>
/// <typeparam name="TToken">The type of tokens in the query.</typeparam>
public sealed class SearchQuery<TToken> : IEquatable<SearchQuery<TToken>>
{
    /// <summary>
    /// Gets or sets the root query node for this search query.
    /// </summary>
    public QueryNode<TToken> QueryNode { get; set; }

    /// <summary>
    /// Gets or sets the number of records to skip in the search results.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of records to return in the search results.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Gets a value indicating whether this search query contains any positive criteria for evaluation.
    /// Empty nodes and NOT queries are not counted as a positive criteria.
    /// </summary>
    public bool HasAnyPositiveCriteria => QueryNode != null && QueryNode.HasAnyPositiveCriteria;

    /// <summary>
    /// Gets a value indicating whether this search query is empty.
    /// </summary>
    public bool IsEmpty => QueryNode == null || QueryNode.IsEmpty;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchQuery{TToken}"/> class.
    /// </summary>
    /// <param name="queryNode">The root query node for this search query.</param>
    /// <param name="skip">The number of records to skip in the search results.</param>
    /// <param name="limit">The maximum number of records to return in the search results.</param>
    public SearchQuery(
        QueryNode<TToken> queryNode,
        int skip = 0,
        int limit = 0
        )
    {
        QueryNode = queryNode;
        Skip = skip;
        Limit = limit;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as SearchQuery<TToken>);
    }

    public bool Equals(SearchQuery<TToken> other)
    {
        return other is not null &&
               EqualityComparer<QueryNode<TToken>>
                .Default
                .Equals(QueryNode, other.QueryNode) &&
               Skip == other.Skip &&
               Limit == other.Limit;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(QueryNode, Skip, Limit);
    }

    public static bool operator ==(SearchQuery<TToken> left, SearchQuery<TToken> right)
    {
        return EqualityComparer<SearchQuery<TToken>>.Default.Equals(left, right);
    }

    public static bool operator !=(SearchQuery<TToken> left, SearchQuery<TToken> right)
    {
        return !(left == right);
    }
}
