
namespace ZoneTree.FullTextSearch.Search;

/// <summary>
/// Represents a node in a query tree, containing tokens and/or child nodes
/// that are evaluated according to the node's type (AND, OR, NOT).
/// </summary>
/// <typeparam name="TToken">The type of tokens contained in the query node.</typeparam>
public sealed class QueryNode<TToken> : IEquatable<QueryNode<TToken>>
{
    /// <summary>
    /// Gets or sets the logical operation type of the query node (AND, OR, NOT).
    /// </summary>
    public QueryNodeType NodeType { get; set; }

    /// <summary>
    /// Gets or sets the tokens associated with this query node.
    /// </summary>
    public TToken[] Tokens { get; set; }

    /// <summary>
    /// Gets or sets the child nodes of this query node.
    /// </summary>
    public QueryNode<TToken>[] Children { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the order of tokens should be respected
    /// during the evaluation of this node. 
    /// </summary>
    public bool RespectTokenOrder { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this node is a facet node.
    /// </summary>
    public bool IsFacetNode { get; set; }

    bool _hasFirstLookAt;

    TToken _firstLookAt;

    /// <summary>
    /// Gets or sets the first token to consider when evaluating this node.
    /// Defaults to the first token in the <see cref="Tokens"/> array if not explicitly set.
    /// </summary>
    public TToken FirstLookAt
    {
        get => _hasFirstLookAt ? _firstLookAt : (HasTokens ? Tokens[0] : default);
        set
        {
            _firstLookAt = value;
            _hasFirstLookAt = true;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this node or any of its child nodes contain positive criteria for evaluation.
    /// Empty nodes and NOT queries are not counted as a positive criteria.
    /// </summary>
    public bool HasAnyPositiveCriteria =>
        NodeType != QueryNodeType.Not &&
        (NodeType != QueryNodeType.Or ||
            (NodeType == QueryNodeType.Or &&
            (Children == null ||
            !Children.Any(c => c.NodeType == QueryNodeType.Not)))) &&
        (HasTokens ||
        (Children != null && Children.Any(c => c.HasAnyPositiveCriteria)));

    /// <summary>
    /// Gets a value indicating whether this query node is empty.
    /// </summary>
    public bool IsEmpty => !HasTokens && !HasChildren;

    /// <summary>
    /// Gets a value indicating whether this node contains tokens.
    /// </summary>
    public bool HasTokens => Tokens != null && Tokens.Length > 0;

    /// <summary>
    /// Gets a value indicating whether this node has child nodes.
    /// </summary>
    public bool HasChildren => Children != null && Children.Length > 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryNode{TToken}"/> class.
    /// </summary>
    /// <param name="nodeType">The logical operation type of the node.</param>
    /// <param name="tokens">The tokens associated with this node.</param>
    /// <param name="children">The child nodes of this node.</param>
    /// <param name="respectTokenOrder">Whether the order of tokens should be respected.</param>
    /// <param name="isFacetNode">Whether this node is a facet node.</param>    
    public QueryNode(
        QueryNodeType nodeType,
        TToken[] tokens = null,
        QueryNode<TToken>[] children = null,
        bool respectTokenOrder = true,
        bool isFacetNode = false)
    {
        NodeType = nodeType;
        Tokens = tokens;
        Children = children;
        RespectTokenOrder = respectTokenOrder;
        IsFacetNode = isFacetNode;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as QueryNode<TToken>);
    }

    public bool Equals(QueryNode<TToken> other)
    {
        if (other is null) return false;

        if (NodeType != other.NodeType) return false;
        if (HasTokens != other.HasTokens) return false;
        if (HasChildren != other.HasChildren) return false;

        if (HasTokens &&
            !Enumerable.SequenceEqual(Tokens, other.Tokens))
            return false;

        if (HasChildren &&
            !Enumerable.SequenceEqual(Children, other.Children))
            return false;

        return RespectTokenOrder == other.RespectTokenOrder &&
               IsFacetNode == other.IsFacetNode &&
               _hasFirstLookAt == other._hasFirstLookAt &&
               EqualityComparer<TToken>
                .Default
                .Equals(_firstLookAt, other._firstLookAt);
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(NodeType);
        if (HasTokens)
        {
            foreach (var token in Tokens)
                hash.Add(token);
        }
        if (HasChildren)
        {
            foreach (var child in Children)
                hash.Add(child);
        }
        hash.Add(RespectTokenOrder);
        hash.Add(IsFacetNode);
        hash.Add(_hasFirstLookAt);
        hash.Add(FirstLookAt);
        hash.Add(HasTokens);
        hash.Add(HasChildren);
        return hash.ToHashCode();
    }

    public static bool operator ==(QueryNode<TToken> left, QueryNode<TToken> right)
    {
        return EqualityComparer<QueryNode<TToken>>.Default.Equals(left, right);
    }

    public static bool operator !=(QueryNode<TToken> left, QueryNode<TToken> right)
    {
        return !(left == right);
    }
}
