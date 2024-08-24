using ZoneTree.FullTextSaearch.SearchEngines;
using ZoneTree.FullTextSearch.UnitTests.sampleData;

namespace ZoneTree.FullTextSearch.UnitTests;

public class FacetTests
{
    [Test]
    public void TestFacetSearch()
    {
        var dataPath = "data/TestFacetSearch";
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);
        using var searchEngine = new HashedSearchEngine<long>(dataPath);

        foreach (var product in ProductList.Products)
        {
            searchEngine.AddRecord(product.Id, product.ToString());
            foreach (var prop in typeof(Facets).GetProperties())
            {
                var value = prop.GetValue(product.Facets);
                if (value is string strValue)
                {
                    searchEngine.AddFacet(product.Id, prop.Name, strValue);
                }
                else if (value is string[] values)
                {
                    foreach (var str in values)
                        searchEngine.AddFacet(product.Id, prop.Name, str);
                }
            }
        }

        var result = searchEngine.Search("wireless", new Dictionary<string, string>
        {
            { "connectivity", "bluetooth"}
        });
        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(1));

        result = searchEngine.Search("wireless", new Dictionary<string, string>());
        Assert.That(result, Has.Length.EqualTo(1));

        result = searchEngine.Search("home", new Dictionary<string, string>());
        Assert.That(result, Has.Length.EqualTo(3));

        result = searchEngine.Search("home", new Dictionary<string, string>
        {
            { "Resolution", "4K UHD"},
            { "EnergyEfficiency", "A+"},
        });
        Assert.That(result, Has.Length.EqualTo(2));

        result = searchEngine.Search("product", new Dictionary<string, string>
        {
            { "Resolution", "4K UHD"},
            { "EnergyEfficiency", "A+"},
            { "Connectivity", "Bluetooth 5.0" },
            { "Features", "Milk Frother" },
        });

        Assert.That(result, Has.Length.EqualTo(4));

        result = searchEngine.Search("", new Dictionary<string, string>
        {
            { "Resolution", "4K UHD"},
            { "EnergyEfficiency", "A+"},
            { "Connectivity", "Bluetooth 5.0" },
            { "Features", "Milk Frother" },
        });

        Assert.That(result, Has.Length.EqualTo(4));

        // Returning all records without providing any criteria is not supported.
        result = searchEngine.Search(null, new Dictionary<string, string>());
        Assert.That(result, Is.Empty);

        result = searchEngine.Search("", new Dictionary<string, string>());
        Assert.That(result, Is.Empty);

        result = searchEngine.Search("");
        Assert.That(result, Is.Empty);
    }
}