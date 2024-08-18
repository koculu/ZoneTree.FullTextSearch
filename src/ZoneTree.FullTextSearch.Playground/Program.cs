using System.Diagnostics;
using ZoneTree.FullTextSaearch.SearchEngines;

namespace ZoneTree.FullTextSearch.Playground;

public class Program
{
    static void Main()
    {
        using var app = new SearchEngineApp();
        app.Run();
        if (true) return;
    }
}

