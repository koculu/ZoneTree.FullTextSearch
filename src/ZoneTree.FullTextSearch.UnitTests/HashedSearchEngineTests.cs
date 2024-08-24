using Tenray.ZoneTree.Exceptions;
using ZoneTree.FullTextSaearch.SearchEngines;

namespace ZoneTree.FullTextSearch.UnitTests;

public class HashedSearchEngineTests
{
    [Test]
    public void AddRecord_ShouldAddRecordToIndex()
    {
        var dataPath = "data/AddRecord_ShouldAddRecordToIndex";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(
            dataPath);

        // Arrange
        int record = 1;
        string text = "sample text";

        // Act
        searchEngine.AddRecord(record, text);

        // Assert
        var searchResult = searchEngine.Search("sample");
        Assert.That(searchResult, Is.Not.Empty);
        Assert.That(searchResult, Contains.Item(record));
    }

    [Test]
    public void DeleteRecord_ShouldRemoveRecordFromIndex()
    {
        var dataPath = "data/DeleteRecord_ShouldRemoveRecordFromIndex";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);

        // Arrange
        int record = 1;
        string text = "sample text";

        // Act
        searchEngine.AddRecord(record, text);
        long deletedCount = searchEngine.DeleteRecord(record);

        // Assert
        Assert.That(deletedCount, Is.EqualTo(2));
        var searchResult = searchEngine.Search("sample");
        Assert.That(searchResult, Is.Empty);
    }

    [Test]
    public void Search_WithRespectTokenOrderTrue_ShouldReturnRecords()
    {
        var dataPath = "data/Search_WithRespectTokenOrderTrue_ShouldReturnRecords";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);

        // Arrange
        int record1 = 1;
        int record2 = 2;
        searchEngine.AddRecord(record1, "quick brown fox");
        searchEngine.AddRecord(record2, "brown fox jumps");

        // Act
        var searchResult = searchEngine.Search("brown fox");

        // Assert
        Assert.That(searchResult.Length, Is.EqualTo(2));
        Assert.That(searchResult, Contains.Item(record1));
        Assert.That(searchResult, Contains.Item(record2));
    }

    [Test]
    public void Search_WithRespectTokenOrderFalse_ShouldReturnRecordsRegardlessOfOrder()
    {
        var dataPath = "data/Search_WithRespectTokenOrderFalse_ShouldReturnRecordsRegardlessOfOrder";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);

        // Arrange
        int record1 = 1;
        int record2 = 2;
        searchEngine.AddRecord(record1, "quick brown fox");
        searchEngine.AddRecord(record2, "fox brown jumps");

        // Act
        var searchResult = searchEngine.Search("brown fox", respectTokenOrder: false);

        // Assert
        Assert.That(searchResult.Length, Is.EqualTo(2));
        Assert.That(searchResult, Contains.Item(record1));
        Assert.That(searchResult, Contains.Item(record2));
    }

    [Test]
    public void Search_WithSkipAndLimit_ShouldReturnLimitedRecords()
    {
        var dataPath = "data/Search_WithSkipAndLimit_ShouldReturnLimitedRecords";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<int>(dataPath);

        // Arrange
        searchEngine.AddRecord(1, "record one");
        searchEngine.AddRecord(2, "record two");
        searchEngine.AddRecord(3, "record three");

        // Act
        var searchResult = searchEngine.Search("record", skip: 1, limit: 1);

        // Assert
        Assert.That(searchResult.Length, Is.EqualTo(1));
        Assert.That(searchResult[0], Is.EqualTo(2));
    }

    [Test]
    public void Dispose_ShouldDisposeIndexProperly()
    {
        // Arrange
        var dataPath = "data/Dispose_ShouldDisposeIndexProperly";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);

        using var searchEngine = new HashedSearchEngine<int>(dataPath);
        searchEngine.AddRecord(1, "sample");

        // Act        
        searchEngine.Dispose();

        // Assert
        Assert.That(searchEngine.Index.IsReadOnly, Is.True);
        Assert.Throws<ZoneTreeIsReadOnlyException>(() => searchEngine.AddRecord(2, "abc"));
    }
}
