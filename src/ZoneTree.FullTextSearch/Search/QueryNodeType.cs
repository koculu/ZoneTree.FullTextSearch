namespace ZoneTree.FullTextSearch.Search;

/// <summary>
/// Represents the type of a query node, which defines the logical operation
/// (AND, OR, NOT) that should be applied to the node's tokens or child nodes.
/// </summary>
public enum QueryNodeType
{
    Not,
    And,
    Or,
}
