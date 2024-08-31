using ZoneTree.FullTextSearch.Search;
using ZoneTree.FullTextSearch.SearchEngines;

namespace ZoneTree.FullTextSearch.UnitTests;

public sealed class ExecuteSearchQueryTests
{
    [Test]
    public void SingleTokenAndQuery()
    {
        var dataPath = "data/SingleTokenAndQuery";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "fox");
        searchEngine.AddRecord(2, "fox cow cat");
        searchEngine.AddRecord(3, "fox cat cow");

        var node = new QueryNode<string>(QueryNodeType.And)
        {
            Tokens = ["fox cat cow"],
            RespectTokenOrder = true
        };
        var query = new SearchQuery<string>(
            new(QueryNodeType.And)
            {
                Tokens = ["fox cat cow"],
                RespectTokenOrder = true
            }
        );
        Assert.That(searchEngine.Search(query), Is.EqualTo(new int[] { 3 }));

        query = new SearchQuery<string>(
            new(QueryNodeType.And)
            {
                Tokens = ["fox", "cat", "cow"],
                RespectTokenOrder = false
            }
        );
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 2, 3 }));

        query = new SearchQuery<string>(
            new(QueryNodeType.Or)
            {
                Tokens = ["fox", "cat", "cow"],
                RespectTokenOrder = false
            }
        );

        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2, 3 }));


        query = new SearchQuery<string>(
            new(QueryNodeType.Not)
            {
                Tokens = ["abc"],
                RespectTokenOrder = false
            }
        );

        result = searchEngine.Search(query).Order();
        Assert.That(query.HasAnyPositiveCriteria, Is.False);
        Assert.That(result, Is.EqualTo((new int[] { 1, 2, 3 })));

        query = new SearchQuery<string>(
            new(QueryNodeType.And)
            {
                Children =
                [
                    new (QueryNodeType.And)
                    {
                        Tokens = ["fox"],
                    },
                    new QueryNode<string>(QueryNodeType.Not)
                    {
                        Tokens = ["cow"],
                    }
                ],
                RespectTokenOrder = false
            }
        );

        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1 }));

        query = new SearchQuery<string>(
            new(QueryNodeType.Or)
            {
                Children =
                [
                    new (QueryNodeType.And)
                    {
                        Tokens = ["fox"],
                    },
                    new QueryNode<string>(QueryNodeType.Not)
                    {
                        Tokens = ["cow"],
                    }
                ]
            }
        );

        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2, 3 }));
    }
}
