using ZoneTree.FullTextSearch.QueryLanguage;
using ZoneTree.FullTextSearch.SearchEngines;
using ZoneTree.FullTextSearch.Tokenizer;

namespace ZoneTree.FullTextSearch.UnitTests;

public sealed class ExecuteParsedQueries
{
    [Test]
    public void SimpleQueries()
    {
        var dataPath = "data/SingleTokenAndQuery2";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "fox");
        searchEngine.AddRecord(2, "fox cow cat");
        searchEngine.AddRecord(3, "fox cat cow");
        searchEngine.AddFacet(3, "category", "red");

        var parser = new Parser("(cat OR cow) AND NOT category:tear");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 2, 3 }));

        parser = new Parser("cat cow AND NOT category:red");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 2 }));

        parser = new Parser("'cat cow' AND NOT category:red");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { }));

        parser = new Parser("'cat cow' AND NOT category:blue");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 3 }));

        parser = new Parser("\"cat cow\" AND NOT category:\"blue\"");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 3 }));

        parser = new Parser("\"cat cow\" AND NOT category:\'blue\'");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 3 }));
    }

    [Test]
    public void PhraseQueryTest()
    {
        var dataPath = "data/PhraseQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "quick brown fox");
        searchEngine.AddRecord(2, "lazy dog");
        searchEngine.AddRecord(3, "quick brown cat");

        var parser = new Parser("\"quick brown fox\"");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1 }));

        parser = new Parser("'quick brown fox'");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1 }));
    }

    [Test]
    public void FacetQueryTest()
    {
        var dataPath = "data/FacetQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "electronics");
        searchEngine.AddRecord(2, "electronics Samsung");
        searchEngine.AddFacet(2, "category", "electronics");

        var parser = new Parser("category:electronics");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 2 }));
    }

    [Test]
    public void FacetInQueryTest()
    {
        var dataPath = "data/FacetInQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "electronics");
        searchEngine.AddRecord(2, "books");
        searchEngine.AddRecord(3, "furniture");
        searchEngine.AddFacet(1, "category", "electronics");
        searchEngine.AddFacet(2, "category", "books");
        searchEngine.AddFacet(3, "category", "furniture");

        var parser = new Parser("category IN [\"books\", \"electronics\"]");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void NotInQueryTest()
    {
        var dataPath = "data/NotInQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "fox");
        searchEngine.AddRecord(2, "lazy dog");
        searchEngine.AddRecord(3, "cat");

        var parser = new Parser("NOT IN [\"lazy dog\", \"cat\"]");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1 }));
    }

    [Test]
    public void ComplexAndOrQueryTest()
    {
        var dataPath = "data/ComplexAndOrQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "cat dog");
        searchEngine.AddRecord(2, "cat fox");
        searchEngine.AddRecord(3, "dog fox");

        var parser = new Parser("cat AND (dog OR fox)");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void NegationQueryWithAndTest()
    {
        var dataPath = "data/NegationQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "black cat");
        searchEngine.AddRecord(2, "dog");
        searchEngine.AddRecord(3, "fox");

        var parser = new Parser("(dog or fox or black) and NOT cat");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 2, 3 }));
    }

    [Test]
    public void NegationQueryTest()
    {
        var dataPath = "data/NegationQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "black cat");
        searchEngine.AddRecord(2, "dog");
        searchEngine.AddRecord(3, "fox");

        var parser = new Parser("NOT cat");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 2, 3 }));

        parser = new Parser("NOT cat or abc");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 2, 3 }));
    }

    [Test]
    public void FacetWithAndQueryTest()
    {
        var dataPath = "data/FacetWithAndQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "books");
        searchEngine.AddFacet(1, "author", "F. Scott Fitzgerald");
        searchEngine.AddRecord(2, "books");
        searchEngine.AddFacet(2, "author", "Bar");

        var parser = new Parser("books AND author:\"F. Scott Fitzgerald\"");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1 }));
    }

    [Test]
    public void FacetWithOrQueryTest()
    {
        var dataPath = "data/FacetWithOrQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "books");
        searchEngine.AddFacet(1, "author", "J.K. Rowling");
        searchEngine.AddFacet(1, "category", "books");
        searchEngine.AddRecord(2, "electronics");
        searchEngine.AddFacet(2, "brand", "Sony");
        searchEngine.AddFacet(2, "category", "electronics");

        var parser = new Parser("(category:books OR category:electronics) AND (author:\"J.K. Rowling\" OR brand:Sony)");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void FacetNotInQueryTest()
    {
        var dataPath = "data/FacetNotInQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "books");
        searchEngine.AddFacet(1, "category", "books");
        searchEngine.AddRecord(2, "electronics");
        searchEngine.AddFacet(2, "category", "electronics");
        searchEngine.AddRecord(3, "furniture");
        searchEngine.AddFacet(3, "category", "furniture");

        var parser = new Parser("category NOT IN [\"books\", \"furniture\"]");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 2 }));
    }

    [Test]
    public void InKeywordListQueryTest()
    {
        var dataPath = "data/InKeywordListQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "cat");
        searchEngine.AddRecord(2, "dog");
        searchEngine.AddRecord(3, "fox");
        searchEngine.AddRecord(4, "cow");

        var parser = new Parser("IN [\"cat\", \"dog\", \"fox\"]");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2, 3 }));
    }

    [Test]
    public void FacetExpressionWithNotInTest()
    {
        var dataPath = "data/FacetExpressionWithNotInTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "Samsung");
        searchEngine.AddFacet(1, "brand", "Samsung");
        searchEngine.AddRecord(2, "Nokia");
        searchEngine.AddFacet(2, "brand", "Nokia");
        searchEngine.AddRecord(3, "Sony");
        searchEngine.AddFacet(3, "brand", "Sony");

        var parser = new Parser("NOT brand IN [\"Samsung\", \"Nokia\"]");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 3 }));
    }

    [Test]
    public void MultipleFacetsInComplexQueryTest()
    {
        var dataPath = "data/MultipleFacetsInComplexQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "Harry Potter");
        searchEngine.AddFacet(1, "author", "J.K. Rowling");
        searchEngine.AddFacet(1, "category", "books");
        searchEngine.AddRecord(2, "Sony TV");
        searchEngine.AddFacet(2, "brand", "Sony");
        searchEngine.AddFacet(2, "category", "electronics");
        searchEngine.AddRecord(3, "Samsung Galaxy");
        searchEngine.AddFacet(3, "brand", "Samsung");
        searchEngine.AddFacet(3, "category", "electronics");

        var parser = new Parser("(category:books OR category:electronics) AND (author:\"J.K. Rowling\" OR brand:Sony)");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void SimpleKeywordQueryTest()
    {
        var dataPath = "data/SimpleKeywordQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "cat");
        searchEngine.AddRecord(2, "dog");
        searchEngine.AddRecord(3, "cat dog");
        searchEngine.AddRecord(4, "fox");

        var parser = new Parser("cat dog");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 3 }));
    }

    [Test]
    public void SimpleFacetQueryTest()
    {
        var dataPath = "data/SimpleFacetQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "electronics");
        searchEngine.AddFacet(1, "category", "electronics");
        searchEngine.AddRecord(2, "appliances");
        searchEngine.AddFacet(2, "category", "appliances");

        var parser = new Parser("category:electronics");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1 }));
    }

    [Test]
    public void MultipleFacetExpressionsTest()
    {
        var dataPath = "data/MultipleFacetExpressionsTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "electronics");
        searchEngine.AddFacet(1, "category", "electronics");
        searchEngine.AddFacet(1, "brand", "Samsung");
        searchEngine.AddRecord(2, "electronics");
        searchEngine.AddFacet(2, "category", "electronics");
        searchEngine.AddFacet(2, "brand", "Sony");
        searchEngine.AddRecord(3, "device");
        searchEngine.AddFacet(2, "category", "analog");
        searchEngine.AddFacet(2, "brand", "Dell");

        var parser = new Parser("category:electronics brand:Samsung");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1 }));
    }

    [Test]
    public void FacetWithPhraseTest()
    {
        var dataPath = "data/FacetWithPhraseTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "The Great Gatsby");
        searchEngine.AddFacet(1, "title", "The Great Gatsby");
        searchEngine.AddRecord(2, "To Kill a Mockingbird");
        searchEngine.AddFacet(2, "title", "To Kill a Mockingbird");

        var parser = new Parser("title:\"The Great Gatsby\"");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1 }));
    }

    [Test]
    public void FacetInWithAndQueryTest()
    {
        var dataPath = "data/FacetInWithAndQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "Samsung");
        searchEngine.AddFacet(1, "brand", "Samsung");
        searchEngine.AddRecord(2, "Sony");
        searchEngine.AddFacet(2, "brand", "Sony");
        searchEngine.AddRecord(3, "Samsung");
        searchEngine.AddFacet(3, "brand", "Samsung");
        searchEngine.AddFacet(3, "category", "electronics");

        var parser = new Parser("brand IN [\"Samsung\", \"Sony\"] AND category:electronics");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 3 }));
    }

    [Test]
    public void EmptyQueryTest()
    {
        var dataPath = "data/CombinedAndQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "cat");
        searchEngine.AddRecord(2, "dog");
        searchEngine.AddRecord(3, "cat dog");
        searchEngine.AddRecord(4, "fox");

        var parser = new Parser("");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(Array.Empty<int>()));
    }

    [Test]
    public void CombinedAndQueryTest()
    {
        var dataPath = "data/CombinedAndQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "cat");
        searchEngine.AddRecord(2, "dog");
        searchEngine.AddRecord(3, "cat dog");
        searchEngine.AddRecord(4, "fox");

        var parser = new Parser("cat AND dog");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 3 }));
    }

    [Test]
    public void CombinedOrQueryTest()
    {
        var dataPath = "data/CombinedOrQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "cat");
        searchEngine.AddRecord(2, "dog");
        searchEngine.AddRecord(3, "cat dog");
        searchEngine.AddRecord(4, "fox");

        var parser = new Parser("cat OR dog");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2, 3 }));
    }

    [Test]
    public void NegationWithOrQueryTest()
    {
        var dataPath = "data/NegationWithOrQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "cat");
        searchEngine.AddRecord(2, "dog");
        searchEngine.AddRecord(3, "fox");

        var parser = new Parser("NOT cat OR dog");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 2, 3 }));
    }

    [Test]
    public void ComplexAndOrQueryWithNegationTest()
    {
        var dataPath = "data/ComplexAndOrQueryWithNegationTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "cat");
        searchEngine.AddRecord(2, "dog");
        searchEngine.AddRecord(3, "lazy dog");
        searchEngine.AddRecord(4, "fox");

        var parser = new Parser("(cat OR dog) AND (NOT \"lazy dog\" OR fox)");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void FacetWithNotInQueryTest()
    {
        var dataPath = "data/FacetWithNotInQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "electronics");
        searchEngine.AddFacet(1, "category", "electronics");
        searchEngine.AddFacet(1, "brand", "Samsung");
        searchEngine.AddRecord(2, "books");
        searchEngine.AddFacet(2, "category", "books");
        searchEngine.AddFacet(2, "brand", "Sony");

        var parser = new Parser("NOT category IN [\"books\", \"furniture\"] AND brand:Samsung");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1 }));
    }

    [Test]
    public void MultipleKeywordsWithoutOperatorsTest()
    {
        var dataPath = "data/MultipleKeywordsWithoutOperatorsTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "quick");
        searchEngine.AddRecord(2, "brown");
        searchEngine.AddRecord(3, "fox");
        searchEngine.AddRecord(4, "quick brown fox");

        var parser = new Parser("quick brown fox");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 4 }));
    }

    [Test]
    public void ComplexMultiFacetAndOrQueryTest()
    {
        var dataPath = "data/ComplexMultiFacetAndOrQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);

        // Adding multiple records with complex facets
        searchEngine.AddRecord(1, "Harry Potter and the Sorcerer's Stone");
        searchEngine.AddFacet(1, "author", "J.K. Rowling");
        searchEngine.AddFacet(1, "category", "books");
        searchEngine.AddFacet(1, "publisher", "Bloomsbury");

        searchEngine.AddRecord(2, "Harry Potter and the Chamber of Secrets");
        searchEngine.AddFacet(2, "author", "J.K. Rowling");
        searchEngine.AddFacet(2, "category", "books");
        searchEngine.AddFacet(2, "publisher", "Scholastic");

        searchEngine.AddRecord(3, "The Fellowship of the Ring");
        searchEngine.AddFacet(3, "author", "J.R.R. Tolkien");
        searchEngine.AddFacet(3, "category", "books");
        searchEngine.AddFacet(3, "publisher", "Allen & Unwin");

        searchEngine.AddRecord(4, "The Two Towers");
        searchEngine.AddFacet(4, "author", "J.R.R. Tolkien");
        searchEngine.AddFacet(4, "category", "books");
        searchEngine.AddFacet(4, "publisher", "Allen & Unwin");

        searchEngine.AddRecord(5, "The Return of the King");
        searchEngine.AddFacet(5, "author", "J.R.R. Tolkien");
        searchEngine.AddFacet(5, "category", "books");
        searchEngine.AddFacet(5, "publisher", "Allen & Unwin");

        searchEngine.AddRecord(6, "The Hobbit");
        searchEngine.AddFacet(6, "author", "J.R.R. Tolkien");
        searchEngine.AddFacet(6, "category", "books");
        searchEngine.AddFacet(6, "publisher", "George Allen & Unwin");

        searchEngine.AddRecord(7, "The Hobbit: An Unexpected Journey");
        searchEngine.AddFacet(7, "author", "Peter Jackson");
        searchEngine.AddFacet(7, "category", "movies");

        // Complex query combining multiple facets and conditions
        var parser = new Parser("(author:\"J.K. Rowling\" AND publisher:Scholastic) OR (author:\"J.R.R. Tolkien\" AND publisher:\"Allen & Unwin\")");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 2, 3, 4, 5 }));
    }

    [Test]
    public void MultiLayeredNestedQueryTest()
    {
        var dataPath = "data/MultiLayeredNestedQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);

        // Adding data with multiple categories and facets
        searchEngine.AddRecord(1, "MacBook Pro");
        searchEngine.AddFacet(1, "brand", "Apple");
        searchEngine.AddFacet(1, "category", "laptops");
        searchEngine.AddFacet(1, "processor", "M1");

        searchEngine.AddRecord(2, "MacBook Air");
        searchEngine.AddFacet(2, "brand", "Apple");
        searchEngine.AddFacet(2, "category", "laptops");
        searchEngine.AddFacet(2, "processor", "M1");

        searchEngine.AddRecord(3, "Surface Laptop 4");
        searchEngine.AddFacet(3, "brand", "Microsoft");
        searchEngine.AddFacet(3, "category", "laptops");
        searchEngine.AddFacet(3, "processor", "Intel");

        searchEngine.AddRecord(4, "Dell XPS 13");
        searchEngine.AddFacet(4, "brand", "Dell");
        searchEngine.AddFacet(4, "category", "laptops");
        searchEngine.AddFacet(4, "processor", "Intel");

        searchEngine.AddRecord(5, "iPhone 12");
        searchEngine.AddFacet(5, "brand", "Apple");
        searchEngine.AddFacet(5, "category", "smartphones");
        searchEngine.AddFacet(5, "processor", "A14");

        searchEngine.AddRecord(6, "Galaxy S21");
        searchEngine.AddFacet(6, "brand", "Samsung");
        searchEngine.AddFacet(6, "category", "smartphones");
        searchEngine.AddFacet(6, "processor", "Exynos");

        searchEngine.AddRecord(7, "Surface Pro 7");
        searchEngine.AddFacet(7, "brand", "Microsoft");
        searchEngine.AddFacet(7, "category", "tablets");
        searchEngine.AddFacet(7, "processor", "Intel");

        // Multi-layered nested query combining facets, categories, and processors
        var parser = new Parser("((brand:Apple AND category:laptops AND processor:M1) OR (brand:Microsoft AND category:laptops)) AND NOT brand:Dell");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2, 3 }));
    }

    [Test]
    public void QueryWithMultipleNegationsTest()
    {
        var dataPath = "data/QueryWithMultipleNegationsTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);

        // Adding more records with different categories and facets
        searchEngine.AddRecord(1, "Harry Potter and the Philosopher's Stone");
        searchEngine.AddFacet(1, "author", "J.K. Rowling");
        searchEngine.AddFacet(1, "category", "books");
        searchEngine.AddFacet(1, "publisher", "Bloomsbury");

        searchEngine.AddRecord(2, "Harry Potter and the Chamber of Secrets");
        searchEngine.AddFacet(2, "author", "J.K. Rowling");
        searchEngine.AddFacet(2, "category", "books");
        searchEngine.AddFacet(2, "publisher", "Scholastic");

        searchEngine.AddRecord(3, "The Hobbit");
        searchEngine.AddFacet(3, "author", "J.R.R. Tolkien");
        searchEngine.AddFacet(3, "category", "books");
        searchEngine.AddFacet(3, "publisher", "Allen & Unwin");

        searchEngine.AddRecord(4, "The Hobbit: An Unexpected Journey");
        searchEngine.AddFacet(4, "author", "Peter Jackson");
        searchEngine.AddFacet(4, "category", "movies");

        searchEngine.AddRecord(5, "Inception");
        searchEngine.AddFacet(5, "director", "Christopher Nolan");
        searchEngine.AddFacet(5, "category", "movies");

        searchEngine.AddRecord(6, "Interstellar");
        searchEngine.AddFacet(6, "director", "Christopher Nolan");
        searchEngine.AddFacet(6, "category", "movies");

        searchEngine.AddRecord(7, "Dunkirk");
        searchEngine.AddFacet(7, "director", "Christopher Nolan");
        searchEngine.AddFacet(7, "category", "movies");

        // Complex query with multiple negations and AND/OR combinations
        var parser = new Parser("(category:books AND NOT author:\"J.K. Rowling\") OR (category:movies AND NOT director:\"Christopher Nolan\")");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 3, 4 }));
    }

    [Test]
    public void DeeplyNestedComplexQueryTest()
    {
        var dataPath = "data/DeeplyNestedComplexQueryTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);

        // Adding a variety of records with deeply nested facets and categories
        searchEngine.AddRecord(1, "iPhone 13");
        searchEngine.AddFacet(1, "brand", "Apple");
        searchEngine.AddFacet(1, "category", "smartphones");
        searchEngine.AddFacet(1, "processor", "A15");

        searchEngine.AddRecord(2, "iPhone 12");
        searchEngine.AddFacet(2, "brand", "Apple");
        searchEngine.AddFacet(2, "category", "smartphones");
        searchEngine.AddFacet(2, "processor", "A14");

        searchEngine.AddRecord(3, "MacBook Air");
        searchEngine.AddFacet(3, "brand", "Apple");
        searchEngine.AddFacet(3, "category", "laptops");
        searchEngine.AddFacet(3, "processor", "M1");

        searchEngine.AddRecord(4, "Galaxy S21");
        searchEngine.AddFacet(4, "brand", "Samsung");
        searchEngine.AddFacet(4, "category", "smartphones");
        searchEngine.AddFacet(4, "processor", "Exynos");

        searchEngine.AddRecord(5, "Surface Laptop 4");
        searchEngine.AddFacet(5, "brand", "Microsoft");
        searchEngine.AddFacet(5, "category", "laptops");
        searchEngine.AddFacet(5, "processor", "Intel");

        searchEngine.AddRecord(6, "Surface Pro 7");
        searchEngine.AddFacet(6, "brand", "Microsoft");
        searchEngine.AddFacet(6, "category", "tablets");
        searchEngine.AddFacet(6, "processor", "Intel");

        searchEngine.AddRecord(7, "Dell XPS 13");
        searchEngine.AddFacet(7, "brand", "Dell");
        searchEngine.AddFacet(7, "category", "laptops");
        searchEngine.AddFacet(7, "processor", "Intel");

        searchEngine.AddRecord(8, "ThinkPad X1 Carbon");
        searchEngine.AddFacet(8, "brand", "Lenovo");
        searchEngine.AddFacet(8, "category", "laptops");
        searchEngine.AddFacet(8, "processor", "Intel");

        // Deeply nested query combining AND, OR, and NOT across different categories and facets
        var parser = new Parser("((brand:Apple AND category:smartphones) OR " +
            "(brand:Microsoft AND category:laptops)) AND " +
            "(processor:Intel OR NOT category:tablets)");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2, 5 }));
    }

    [Test]
    public void OperatorPrecedenceBetweenAndOrTest()
    {
        var dataPath = "data/OperatorPrecedenceBetweenAndOrTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);

        // Adding records
        searchEngine.AddRecord(1, "cat dog fox");
        searchEngine.AddRecord(2, "cat fox");
        searchEngine.AddRecord(3, "dog fox");
        searchEngine.AddRecord(4, "dog");
        searchEngine.AddRecord(5, "fox");
        searchEngine.AddRecord(6, "cat");

        // Query: cat AND dog OR fox
        // Expected interpretation: (cat AND dog) OR fox
        // Matches records: 1, 2, 3, 5
        var parser = new Parser("cat AND dog OR fox");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2, 3, 5 }));


        // Query: cat AND dog OR fox
        // Expected interpretation: fox OR (cat AND dog)
        // Matches records: 1, 2, 3, 5
        parser = new Parser("fox OR cat AND dog");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2, 3, 5 }));

        // Query: cat AND (dog OR fox)
        // Explicit precedence given by parentheses
        // Matches records: 1, 2
        parser = new Parser("cat AND (dog OR fox)");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void BasicOperatorPrecedenceTest()
    {
        var dataPath = "data/BasicOperatorPrecedenceTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);

        // Adding records
        searchEngine.AddRecord(1, "cat dog");
        searchEngine.AddRecord(2, "cat");
        searchEngine.AddRecord(3, "dog");
        searchEngine.AddRecord(4, "fox");
        searchEngine.AddRecord(5, "dog fox");

        // Query: cat OR dog AND NOT fox
        // Expected interpretation: cat OR (dog AND (NOT fox))
        // Matches records: 1, 2, 3
        var parser = new Parser("cat OR dog AND NOT fox");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2, 3 }));
    }

    [Test]
    public void NestedOperatorPrecedenceTest()
    {
        var dataPath = "data/NestedOperatorPrecedenceTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);

        // Adding records
        searchEngine.AddRecord(1, "cat dog");
        searchEngine.AddRecord(2, "cat fox");
        searchEngine.AddRecord(3, "dog fox");
        searchEngine.AddRecord(4, "dog");
        searchEngine.AddRecord(5, "fox");
        searchEngine.AddRecord(6, "cat");

        // Query: (cat OR dog) AND NOT (fox OR dog)
        // Expected interpretation: (cat OR dog) AND NOT (fox OR dog)
        // Matches records: 6
        var parser = new Parser("(cat OR dog) AND NOT (fox OR dog)");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 6 }));

        // Query: NOT cat OR (dog AND fox)
        // Expected interpretation: (NOT cat) OR (dog AND fox)
        // Matches records: 3, 4, 5
        parser = new Parser("NOT cat OR (dog AND fox)");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 3, 4, 5 }));
    }

    [Test]
    public void ComplexOperatorPrecedenceWithFacetsTest()
    {
        var dataPath = "data/ComplexOperatorPrecedenceWithFacetsTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);

        // Adding records
        searchEngine.AddRecord(1, "book");
        searchEngine.AddRecord(2, "book");
        searchEngine.AddRecord(3, "electronics");
        searchEngine.AddFacet(1, "author", "George Orwell");
        searchEngine.AddFacet(2, "author", "F. Scott Fitzgerald");
        searchEngine.AddFacet(3, "brand", "Sony");

        // Query: author:"George Orwell" OR brand:Sony AND NOT author:"F. Scott Fitzgerald"
        // Expected interpretation: (author:"George Orwell") OR (brand:Sony AND NOT author:"F. Scott Fitzgerald")
        // Matches records: 1, 3
        var parser = new Parser("author:\"George Orwell\" OR brand:Sony AND NOT author:\"F. Scott Fitzgerald\"");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 3 }));
    }

    [Test]
    public void MixedOperationsWithParenthesesTest()
    {
        var dataPath = "data/MixedOperationsWithParenthesesTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);

        // Adding records
        searchEngine.AddRecord(1, "cat dog fox");
        searchEngine.AddRecord(2, "cat dog");
        searchEngine.AddRecord(3, "cat fox");
        searchEngine.AddRecord(4, "dog fox");
        searchEngine.AddRecord(5, "fox");
        searchEngine.AddRecord(6, "dog");

        // Query: (cat OR dog) AND fox
        // Expected interpretation: ((cat OR dog) AND fox)
        // Matches records: 1, 3, 4
        var parser = new Parser("(cat OR dog) AND fox");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 3, 4 }));

        // Query: NOT (cat OR fox) AND dog
        // Expected interpretation: (NOT (cat OR fox)) AND dog
        // Matches records: 6
        parser = new Parser("NOT (cat OR fox) AND dog");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 6 }));
    }

    [Test]
    public void UnicodeSupportTest()
    {
        var dataPath = "data/UnicodeSupportTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(
            dataPath,
            wordTokenizer: new WordTokenizer(1));

        // Adding records with Unicode characters
        searchEngine.AddRecord(1, "こんにちは 世界"); // Japanese for "Hello World"
        searchEngine.AddRecord(2, "Привет мир"); // Russian for "Hello World"
        searchEngine.AddRecord(3, "你好 世界"); // Chinese for "Hello World"
        searchEngine.AddRecord(4, "안녕하세요 세계"); // Korean for "Hello World"
        searchEngine.AddRecord(5, "Hello World"); // English
        searchEngine.AddRecord(6, "مرحبا بالعالم"); // Arabic for "Hello World"

        // Query: "こんにちは"
        // Should match record 1
        var parser = new Parser("こんにちは");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1 }));

        // Query: "Привет"
        // Should match record 2
        parser = new Parser("Привет");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 2 }));

        // Query: "世界" (common in Japanese and Chinese)
        // Should match records 1 and 3
        parser = new Parser("世界");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 3 }));

        // Query: "안녕하세요"
        // Should match record 4
        parser = new Parser("안녕하세요");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 4 }));

        // Query: "Hello"
        // Should match record 5
        parser = new Parser("Hello");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 5 }));

        // Query: "World"
        // Should match record 5
        parser = new Parser("World");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 5 }));

        // Query: "مرحبا"
        // Should match record 6
        parser = new Parser("مرحبا");
        var queryArabic = parser.Parse();
        var resultArabic = searchEngine.Search(queryArabic).Order();
        Assert.That(resultArabic, Is.EqualTo(new int[] { 6 }));

        // Query: "بالعالم"
        // Should match record 6
        parser = new Parser("بالعالم");
        queryArabic = parser.Parse();
        resultArabic = searchEngine.Search(queryArabic).Order();
        Assert.That(resultArabic, Is.EqualTo(new int[] { 6 }));

        // Query: "مرحبا بالعالم"
        // Should match record 6
        parser = new Parser("\"مرحبا بالعالم\"");
        queryArabic = parser.Parse();
        resultArabic = searchEngine.Search(queryArabic).Order();
        Assert.That(resultArabic, Is.EqualTo(new int[] { 6 }));

        // Query: "Hello World"
        // Should match record 5 (exact match for English phrase)
        parser = new Parser("\"Hello World\"");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 5 }));
    }

    [Test]
    public void AliasAndOrNotTest()
    {
        var dataPath = "data/AliasAndOrNotTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "cat dog fox");
        searchEngine.AddRecord(2, "cat fox");
        searchEngine.AddRecord(3, "dog fox");
        searchEngine.AddRecord(4, "dog");
        searchEngine.AddRecord(5, "fox");
        searchEngine.AddRecord(6, "cat");

        // Test alias for AND (&)
        var parser = new Parser("cat & dog");
        var query = parser.Parse();
        var result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1 }));

        // Test alias for OR (|)
        parser = new Parser("cat | dog");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2, 3, 4, 6 }));

        // Test alias for NOT (-)
        parser = new Parser("-fox");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 4, 6 }));

        // Combined aliases test: "cat & (dog | -fox)"
        parser = new Parser("cat & (dog | -fox)");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 6 }));


        // Test aliases without whitespace
        parser = new Parser("cat&(dog|-fox)");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 6 }));

        // More complex case with all three aliases: "cat & -dog | fox"
        parser = new Parser("cat & -dog | fox");
        query = parser.Parse();
        result = searchEngine.Search(query).Order();
        Assert.That(result, Is.EqualTo(new int[] { 1, 2, 3, 5, 6 }));
    }

    [Test]
    public void DeleteRecordTest()
    {
        var dataPath = "data/DeleteRecordTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath, useSecondaryIndex: false);
        searchEngine.AddRecord(1, "The quick brown fox jumps over the lazy dog.");
        searchEngine.AddRecord(2, "The quick brown fox.");

        // Delete the first record
        searchEngine.DeleteRecord(1);

        // Search to confirm deletion
        var results = searchEngine.Search("quick brown");
        Assert.That(results.Order(), Is.EqualTo(new int[] { 2 }));
    }

    [Test]
    public void DeleteTokensTest()
    {
        var dataPath = "data/DeleteTokensTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath, useSecondaryIndex: false);
        searchEngine.AddRecord(1, "The quick brown fox jumps over the lazy dog.");
        searchEngine.AddRecord(2, "The quick brown fox.");

        // Delete tokens associated with the first record
        searchEngine.DeleteTokens(1, "The quick brown fox jumps over the lazy dog.");

        // Search to confirm deletion
        var results = searchEngine.Search("quick brown or lazy");
        Assert.That(results.Order(), Is.EqualTo(new int[] { 2 }));
    }

    [Test]
    public void UpdateRecordTest()
    {
        var dataPath = "data/UpdateRecordTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "The quick brown fox jumps over the lazy dog.");

        // Update the record with new text
        searchEngine.UpdateRecord(
            1,
            "The quick brown fox jumps over the lazy dog.",
            "The quick brown fox leaps over the lazy dog.");

        // Confirm the old text is no longer found
        var oldResults = searchEngine.Search("jumps");
        Assert.That(oldResults, Is.Empty);

        // Confirm the new text is found
        var newResults = searchEngine.Search("leaps");
        Assert.That(newResults.Order(), Is.EqualTo(new int[] { 1 }));
    }

    [Test]
    public void DeleteAndUpdateMixedTest()
    {
        var dataPath = "data/DeleteAndUpdateMixedTest";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "The quick brown fox.");
        searchEngine.AddRecord(2, "The lazy dog.");

        // Update the first record
        searchEngine.UpdateRecord(1, "The quick brown fox.", "The quick brown bear.");

        // Delete the second record
        searchEngine.DeleteRecord(2);

        // Confirm that the old text for the first record is no longer found
        var oldResults = searchEngine.Search("fox");
        Assert.That(oldResults, Is.Empty);

        // Confirm that the new text for the first record is found
        var newResults = searchEngine.Search("bear");
        Assert.That(newResults.Order(), Is.EqualTo(new int[] { 1 }));

        // Confirm that the second record has been deleted
        var deletedResults = searchEngine.Search("dog");
        Assert.That(deletedResults, Is.Empty);
    }
}