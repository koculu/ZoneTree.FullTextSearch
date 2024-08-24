using Tenray.ZoneTree.Exceptions;

namespace ZoneTree.FullTextSearch.UnitTests;

public class RecordTableTests
{
    [Test]
    public void UpsertRecord_ShouldInsertRecordAndValueIntoBothZoneTrees()
    {
        var dataPath = "data/UpsertRecord_ShouldInsertRecordAndValueIntoBothZoneTrees";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var recordTable = new RecordTable<int, string>(dataPath);

        // Arrange
        int record = 1;
        string value = "Value1";

        // Act
        recordTable.UpsertRecord(record, value);

        // Assert
        Assert.That(recordTable.TryGetValue(record, out var retrievedValue), Is.True);
        Assert.That(recordTable.TryGetRecord(value, out var retrievedRecord), Is.True);
        Assert.That(retrievedValue, Is.EqualTo("Value1"));
        Assert.That(retrievedRecord, Is.EqualTo(record));
    }

    [Test]
    public void GetLastRecord_ShouldReturnLastInsertedRecord()
    {
        var dataPath = "data/GetLastRecord_ShouldReturnLastInsertedRecord";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var recordTable = new RecordTable<int, string>(dataPath);

        // Arrange
        int record1 = 1;
        int record2 = 2;
        string value1 = "Value1";
        string value2 = "Value2";

        // Act
        recordTable.UpsertRecord(record1, value1);
        recordTable.UpsertRecord(record2, value2);
        var lastRecord = recordTable.GetLastRecord();

        // Assert
        Assert.That(lastRecord, Is.EqualTo(record2));
    }

    [Test]
    public void GetValue_ShouldReturnAssociatedValueForRecord()
    {
        var dataPath = "data/GetValue_ShouldReturnAssociatedValueForRecord";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var recordTable = new RecordTable<int, string>(dataPath);

        // Arrange
        int record = 1;
        string value = "Value1";

        // Act
        recordTable.UpsertRecord(record, value);
        recordTable.TryGetValue(record, out var retrievedValue);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(value));
    }

    [Test]
    public void TryGetRecord_ShouldReturnTrueAndRecordWhenValueExists()
    {
        var dataPath = "data/TryGetRecord_ShouldReturnTrueAndRecordWhenValueExists";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var recordTable = new RecordTable<int, string>(dataPath);

        // Arrange
        int record = 1;
        string value = "Value1";

        // Act
        recordTable.UpsertRecord(record, value);
        bool found = recordTable.TryGetRecord(value, out int retrievedRecord);

        // Assert
        Assert.That(found, Is.True);
        Assert.That(retrievedRecord, Is.EqualTo(record));
    }

    [Test]
    public void TryGetRecord_ShouldReturnFalseWhenValueDoesNotExist()
    {
        var dataPath = "data/TryGetRecord_ShouldReturnFalseWhenValueDoesNotExist";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var recordTable = new RecordTable<int, string>(dataPath);

        // Act
        bool found = recordTable.TryGetRecord("NonExistentValue", out int _);

        // Assert
        Assert.That(found, Is.False);
    }

    [Test]
    public void Dispose_ShouldDisposeBothZoneTreesAndMaintainers()
    {
        // Arrange
        var recordTable = new RecordTable<int, string>("data/Dispose_ShouldDisposeBothZoneTreesAndMaintainers");
        recordTable.UpsertRecord(1, "Value1");

        // Act
        recordTable.Dispose();

        // Assert
        Assert.Throws<ZoneTreeIsReadOnlyException>(() => recordTable.UpsertRecord(2, "Value2"));
    }
}
