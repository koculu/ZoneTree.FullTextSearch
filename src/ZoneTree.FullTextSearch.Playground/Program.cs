namespace ZoneTree.FullTextSearch.Playground;

public sealed class Program
{
    static void Main(string[] args)
    {
        using var app = new SearchEngineApp();
        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "create":
                    {
                        app.CreateIndex(app.DefaultIndexPath, app.DefaultFilePattern, false);
                        break;
                    }
                case "drop":
                    {
                        app.DropIndex(false);
                        break;
                    }
            }
            return;
        }
        app.Run();
    }
}

