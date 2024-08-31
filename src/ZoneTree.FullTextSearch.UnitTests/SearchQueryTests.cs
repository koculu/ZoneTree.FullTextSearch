using ZoneTree.FullTextSearch.Search;
using ZoneTree.FullTextSearch.Hashing;
using ZoneTree.FullTextSearch.Tokenizer;

namespace ZoneTree.FullTextSearch.UnitTests;

public sealed class SearchQueryTests
{
    [Test]
    public void SingleTokenAndQueryRespectTokenOrder()
    {
        var hashGenerator = new DefaultHashCodeGenerator();
        var tokenizer = new WordTokenizer();
        var node = new QueryNode<string>(QueryNodeType.And);
        var query = new SearchQuery<string>(node);
        node.Tokens = ["fox cat cow"];
        node.RespectTokenOrder = true;
        var hashedQuery = HashedSearchQueryFactory.FromStringSearchQuery(query, hashGenerator, tokenizer);
        var hashedNode = hashedQuery.QueryNode;

        Assert.That(hashedNode.NodeType, Is.EqualTo(QueryNodeType.And));
        Assert.That(hashedNode.HasChildren, Is.False);
        Assert.That(hashedNode.HasTokens, Is.True);
        var expectedHashedTokens = new[] { "fox", "cat", "cow" }
            .Select(hashGenerator.GetHashCode)
            .ToArray();
        Assert.That(hashedNode.Tokens, Is.EqualTo(expectedHashedTokens));
    }

    [Test]
    public void SingleTokenOrQuery()
    {
        var hashGenerator = new DefaultHashCodeGenerator();
        var tokenizer = new WordTokenizer();
        var node = new QueryNode<string>(QueryNodeType.Or);
        var query = new SearchQuery<string>(node);
        node.Tokens = ["fox cat cow"];
        node.RespectTokenOrder = false;
        var hashedQuery = HashedSearchQueryFactory.FromStringSearchQuery(query, hashGenerator, tokenizer);
        var hashedNode = hashedQuery.QueryNode;

        // Edge case, single token converted into AND
        Assert.That(hashedNode.NodeType,
            Is.EqualTo(QueryNodeType.And));
        Assert.That(hashedNode.HasChildren, Is.False);
        Assert.That(hashedNode.HasTokens, Is.True);
        Assert.That(hashedNode.RespectTokenOrder, Is.True);
        var expectedHashedTokens = new[] { "fox", "cat", "cow" }
            .Select(hashGenerator.GetHashCode)
            .ToArray();
        Assert.That(hashedNode.Tokens, Is.EqualTo(expectedHashedTokens));
    }

    [Test]
    public void DoubleTokensOrQuery()
    {
        var hashGenerator = new DefaultHashCodeGenerator();
        var tokenizer = new WordTokenizer();
        var node = new QueryNode<string>(QueryNodeType.Or);
        var query = new SearchQuery<string>(node);
        node.Tokens = ["fox cat cow", "lion dog wolf"];
        node.RespectTokenOrder = true;
        var hashedQuery = HashedSearchQueryFactory.FromStringSearchQuery(query, hashGenerator, tokenizer);

        var hashedNode = hashedQuery.QueryNode;
        Assert.That(hashedNode.NodeType, Is.EqualTo(QueryNodeType.Or));
        Assert.That(hashedNode.HasChildren, Is.True);
        Assert.That(hashedNode.HasTokens, Is.False);
        Assert.That(hashedNode.RespectTokenOrder, Is.True);

        Assert.That(hashedNode.Children.Length, Is.EqualTo(2));

        var child1 = hashedNode.Children[0];
        var child2 = hashedNode.Children[1];

        Assert.That(child1.NodeType, Is.EqualTo(QueryNodeType.And));
        Assert.That(child2.NodeType, Is.EqualTo(QueryNodeType.And));
        Assert.That(child1.RespectTokenOrder, Is.True);
        Assert.That(child2.RespectTokenOrder, Is.True);

        var expectedHashedTokens1 = new[] { "fox", "cat", "cow" }
            .Select(hashGenerator.GetHashCode)
            .ToArray();
        Assert.That(child1.Tokens, Is.EqualTo(expectedHashedTokens1));

        var expectedHashedTokens2 = new[] { "lion", "dog", "wolf" }
            .Select(hashGenerator.GetHashCode)
            .ToArray();
        Assert.That(child2.Tokens, Is.EqualTo(expectedHashedTokens2));
    }

    [TestCase(QueryNodeType.And)]
    [TestCase(QueryNodeType.Not)]
    public void DoubleTokensQuery(QueryNodeType nodeType)
    {
        var hashGenerator = new DefaultHashCodeGenerator();
        var tokenizer = new WordTokenizer();
        var node = new QueryNode<string>(nodeType);
        var query = new SearchQuery<string>(node);
        node.Tokens = ["fox cat cow", "lion dog wolf"];
        node.RespectTokenOrder = false;
        var hashedQuery = HashedSearchQueryFactory.FromStringSearchQuery(query, hashGenerator, tokenizer);

        var hashedNode = hashedQuery.QueryNode;
        Assert.That(hashedNode.NodeType, Is.EqualTo(QueryNodeType.And));
        Assert.That(hashedNode.HasChildren, Is.True);
        Assert.That(hashedNode.HasTokens, Is.False);
        Assert.That(hashedNode.RespectTokenOrder, Is.False);

        Assert.That(hashedNode.Children.Length, Is.EqualTo(2));

        var child1 = hashedNode.Children[0];
        var child2 = hashedNode.Children[1];

        Assert.That(child1.NodeType, Is.EqualTo(nodeType));
        Assert.That(child2.NodeType, Is.EqualTo(nodeType));
        Assert.That(child1.RespectTokenOrder, Is.True);
        Assert.That(child2.RespectTokenOrder, Is.True);

        var expectedHashedTokens1 = new[] { "fox", "cat", "cow" }
            .Select(hashGenerator.GetHashCode)
            .ToArray();
        Assert.That(child1.Tokens, Is.EqualTo(expectedHashedTokens1));

        var expectedHashedTokens2 = new[] { "lion", "dog", "wolf" }
            .Select(hashGenerator.GetHashCode)
            .ToArray();
        Assert.That(child2.Tokens, Is.EqualTo(expectedHashedTokens2));
    }

    [TestCase(QueryNodeType.And)]
    [TestCase(QueryNodeType.Not)]
    public void DoubleTokensQueryRespectTokenOrder(QueryNodeType nodeType)
    {
        var hashGenerator = new DefaultHashCodeGenerator();
        var tokenizer = new WordTokenizer();
        var node = new QueryNode<string>(nodeType);
        var query = new SearchQuery<string>(node);
        node.Tokens = ["fox cat cow", "lion dog wolf"];
        node.RespectTokenOrder = true;
        var hashedQuery = HashedSearchQueryFactory.FromStringSearchQuery(query, hashGenerator, tokenizer);

        var hashedNode = hashedQuery.QueryNode;
        Assert.That(hashedNode.NodeType, Is.EqualTo(nodeType));
        Assert.That(hashedNode.HasChildren, Is.False);
        Assert.That(hashedNode.HasTokens, Is.True);
        Assert.That(hashedNode.RespectTokenOrder, Is.True);

        Assert.That(hashedNode.Tokens.Length, Is.EqualTo(6));

        var expectedHashedTokens1 = new[] { "fox", "cat", "cow", "lion", "dog", "wolf" }
            .Select(hashGenerator.GetHashCode)
            .ToArray();
        Assert.That(hashedNode.Tokens, Is.EqualTo(expectedHashedTokens1));
    }

    [TestCase(QueryNodeType.Or)]
    [TestCase(QueryNodeType.And)]
    [TestCase(QueryNodeType.Not)]
    public void TripleTokenQuery(QueryNodeType nodeType)
    {
        var hashGenerator = new DefaultHashCodeGenerator();
        var tokenizer = new WordTokenizer();
        var node = new QueryNode<string>(nodeType);
        var query = new SearchQuery<string>(node);
        node.Tokens = ["fox", "cat", "cow"];
        node.RespectTokenOrder = true;
        var hashedQuery = HashedSearchQueryFactory.FromStringSearchQuery(query, hashGenerator, tokenizer);
        var hashedNode = hashedQuery.QueryNode;

        Assert.That(hashedNode.NodeType,
            Is.EqualTo(nodeType));
        Assert.That(hashedNode.HasChildren, Is.False);
        Assert.That(hashedNode.HasTokens, Is.True);
        Assert.That(hashedNode.RespectTokenOrder, Is.True);
        var expectedHashedTokens = new[] { "fox", "cat", "cow" }
            .Select(hashGenerator.GetHashCode)
            .ToArray();
        Assert.That(hashedNode.Tokens, Is.EqualTo(expectedHashedTokens));
    }

    [TestCase(QueryNodeType.Or)]
    [TestCase(QueryNodeType.And)]
    [TestCase(QueryNodeType.Not)]
    public void ComplexQuery(QueryNodeType rootType)
    {
        var hashGenerator = new DefaultHashCodeGenerator();
        var tokenizer = new WordTokenizer();
        var node = new QueryNode<string>(rootType);
        var query = new SearchQuery<string>(node);
        node.Children = [
            new QueryNode<string>(QueryNodeType.And) {
                Tokens = ["fox cat", "cow"],
                RespectTokenOrder = false,
            },
            new QueryNode<string>(QueryNodeType.And) {
                Tokens = ["fox cat", "cow"],
                RespectTokenOrder = true,
            },
            new QueryNode<string>(QueryNodeType.Not) {
                Tokens = ["fox cat", "cow"],
                RespectTokenOrder = true,
            },
            new QueryNode<string>(QueryNodeType.Not) {
                Tokens = ["fox cat", "cow"],
                RespectTokenOrder = false,
            },
            new QueryNode<string>(QueryNodeType.Or) {
                Tokens = ["fox cat", "cow"],
                RespectTokenOrder = false,
            },
        ];
        node.RespectTokenOrder = true;

        var hashedQuery = HashedSearchQueryFactory.FromStringSearchQuery(query, hashGenerator, tokenizer);
        var hashedNode = hashedQuery.QueryNode;

        Assert.That(hashedNode.NodeType, Is.EqualTo(rootType));
        Assert.That(hashedNode.HasChildren, Is.True);
        Assert.That(hashedNode.HasTokens, Is.False);
        Assert.That(hashedNode.RespectTokenOrder, Is.True);

        ulong[] Hash(string[] words) => words.Select(x => hashGenerator.GetHashCode(x)).ToArray();

        QueryNode<ulong>[] expectedChildren = [
            new QueryNode<ulong>(QueryNodeType.And) {
                Children = [
                    new QueryNode<ulong>(QueryNodeType.And) {
                        Tokens = Hash(["fox", "cat"]),
                        RespectTokenOrder = true,
                    },
                    new QueryNode<ulong>(QueryNodeType.And) {
                        Tokens = Hash(["cow"]),
                        RespectTokenOrder = true,
                    }],
                RespectTokenOrder = false
            },
            new QueryNode<ulong>(QueryNodeType.And) {
                Tokens = Hash(["fox", "cat", "cow"]),
                RespectTokenOrder = true,
            },
            new QueryNode<ulong>(QueryNodeType.Not) {
                Tokens = Hash(["fox", "cat", "cow"]),
                RespectTokenOrder = true,
            },
            new QueryNode<ulong>(QueryNodeType.And) {
                 Children = [
                    new QueryNode<ulong>(QueryNodeType.Not) {
                        Tokens = Hash(["fox", "cat"]),
                        RespectTokenOrder = true,
                    },
                    new QueryNode<ulong>(QueryNodeType.Not) {
                        Tokens = Hash(["cow"]),
                        RespectTokenOrder = true,
                    }],
                RespectTokenOrder = false
            },
            new QueryNode<ulong>(QueryNodeType.Or) {
                Children = [
                    new QueryNode<ulong>(QueryNodeType.And) {
                        Tokens = Hash(["fox", "cat"]),
                        RespectTokenOrder = true,
                    },
                    new QueryNode<ulong>(QueryNodeType.And) {
                        Tokens = Hash(["cow"]),
                        RespectTokenOrder = true,
                    }],
                RespectTokenOrder = false
            },
        ];
        AssertChildrenAreEqual(hashedNode.Children, expectedChildren);
    }

    void AssertNodesAreEqual(QueryNode<ulong> given, QueryNode<ulong> expected)
    {
        Assert.That(given.NodeType, Is.EqualTo(expected.NodeType));
        Assert.That(given.RespectTokenOrder, Is.EqualTo(expected.RespectTokenOrder));
        Assert.That(given.IsFacetNode, Is.EqualTo(expected.IsFacetNode));
        Assert.That(given.HasTokens, Is.EqualTo(expected.HasTokens));
        Assert.That(given.HasChildren, Is.EqualTo(expected.HasChildren));
        Assert.That(given.FirstLookAt, Is.EqualTo(expected.FirstLookAt));
        Assert.That(given.IsFacetNode, Is.EqualTo(expected.IsFacetNode));
        if (given.HasTokens)
        {
            Assert.That(given.Tokens, Is.EqualTo(expected.Tokens));
        }
        if (given.HasChildren)
        {
            AssertChildrenAreEqual(given.Children, expected.Children);
        }
    }

    void AssertChildrenAreEqual(QueryNode<ulong>[] given, QueryNode<ulong>[] expected)
    {
        Assert.That(given.Length, Is.EqualTo(expected.Length));
        for (var i = 0; i < given.Length; ++i)
        {
            var givenNode = given[i];
            var expectedNode = expected[i];
            AssertNodesAreEqual(givenNode, expectedNode);
        }
    }
}
