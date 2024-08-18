using System.Diagnostics;
using System.Runtime;
using ZoneTree.FullTextSaearch.SearchEngines;
using ZoneTree.FullTextSearch.Core.Tokenizer;
using ZoneTree.FullTextSearch.Core.Util;

namespace ZoneTree.FullTextSearch.Playground;

public sealed class SearchEngineApp : IDisposable
{
    readonly string DataPath = "data";

    readonly string DefaultIndexPath = @"D:\code\";

    readonly string DefaultPattern = "*.cs";

    readonly bool UseSecondaryIndex = false;

    readonly HashedSearchEngine<long> SearchEngine;

    readonly RecordTable<long, string> RecordTable;

    public SearchEngineApp()
    {
        SearchEngine = new HashedSearchEngine<long>(
            DataPath,
            UseSecondaryIndex,
            new WordTokenizer(3));
        RecordTable = new RecordTable<long, string>(DataPath);
    }

    public void Run()
    {
        MainMenu();
    }

    void MainMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("ZoneTree.FullTextSearch - HashedSearchEngine");

            if (!UseSecondaryIndex)
                Console.WriteLine("SecondaryIndex is disabled. Deleting records might be slow.");

            Console.WriteLine("1. Create Index");
            Console.WriteLine("2. Search");
            Console.WriteLine("3. Show Stats");
            Console.WriteLine("4. Drop Index");
            Console.WriteLine("5. Collect GC");
            Console.WriteLine("6. Exit");
            Console.Write("Select an option: ");
            var input = Console.ReadLine();
            try
            {
                switch (input)
                {
                    case "1":
                        CreateIndex();
                        break;
                    case "2":
                        Search();
                        break;
                    case "3":
                        ShowStats();
                        break;
                    case "4":
                        DropIndex();
                        return;
                    case "5":
                        CollectGC();
                        break;
                    case "6":
                    case "q":
                    case "Q":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                PressAnyKeyToContinue();
            }
        }
    }

    static void ShowMemoryUsage(string label)
    {
        var currentProcess = Process.GetCurrentProcess();
        var physicalMemory = currentProcess.WorkingSet64 / (1024 * 1024);
        var virtualMemory = currentProcess.PrivateMemorySize64 / (1024 * 1024);
        var pagedMemory = currentProcess.PagedMemorySize64 / (1024 * 1024);
        var peakPagedMemorySize = currentProcess.PeakPagedMemorySize64 / (1024 * 1024);
        var gcMemory = GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024);

        var separator = "+----------------------------------+--------------+";
        var header = "| Metric                           | Value (MB)   |";

        Console.WriteLine(separator);
        Console.WriteLine($"| {label.PadRight(32)}|               |");
        Console.WriteLine(separator);
        Console.WriteLine(header);
        Console.WriteLine(separator);
        Console.WriteLine($"| Total Physical Memory Usage      | {physicalMemory,12} |");
        Console.WriteLine($"| Total Virtual Memory Usage       | {virtualMemory,12} |");
        Console.WriteLine($"| Paged Memory Size                | {pagedMemory,12} |");
        Console.WriteLine($"| Peak Paged Memory Size           | {peakPagedMemorySize,12} |");
        Console.WriteLine($"| Total GC Memory                  | {gcMemory,12} |");
        Console.WriteLine(separator);
        Console.WriteLine();
    }

    static void CollectGC()
    {
        ShowMemoryUsage("Before GC");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        ShowMemoryUsage("Before LOH GC");
        GCSettings.LargeObjectHeapCompactionMode =
            GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect();
        ShowMemoryUsage("After LOH GC");
        PressAnyKeyToContinue();
    }

    static void PressAnyKeyToContinue()
    {
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    void DropIndex()
    {
        RecordTable.Drop();
        SearchEngine.Drop();
        Console.WriteLine("Dropped the index. Press any key to continue...");
        Console.ReadKey();
    }

    void ShowStats()
    {
        Console.WriteLine("Counting tokens and records...");
        var sw = Stopwatch.StartNew();
        var tokenCount = SearchEngine.Index.ZoneTree1.Count();
        var recordCount = RecordTable.ZoneTree1.Count();
        var elapsedMilliseconds = sw.ElapsedMilliseconds;

        // Prepare the data
        string[] headers = { "Metric", "Value" };
        string[] tokenCountRow = { "Token Count", tokenCount.ToString() };
        string[] recordCountRow = { "Record Count", recordCount.ToString() };
        string[] elapsedTimeRow = { "Elapsed Time (ms)", elapsedMilliseconds.ToString() };

        // Determine the width of each column
        int columnWidth = Math.Max(headers[0].Length, Math.Max(tokenCountRow[0].Length, Math.Max(recordCountRow[0].Length, elapsedTimeRow[0].Length))) + 2;
        int valueWidth = Math.Max(headers[1].Length, Math.Max(tokenCountRow[1].Length, Math.Max(recordCountRow[1].Length, elapsedTimeRow[1].Length))) + 2;

        // Print the table
        string separator = $"+{new string('-', columnWidth)}+{new string('-', valueWidth)}+";

        Console.Clear();
        Console.WriteLine(separator);
        Console.WriteLine($"| {"Metric".PadRight(columnWidth - 1)}| {"Value".PadRight(valueWidth - 1)}|");
        Console.WriteLine(separator);
        Console.WriteLine($"| {tokenCountRow[0].PadRight(columnWidth - 1)}| {tokenCountRow[1].PadRight(valueWidth - 1)}|");
        Console.WriteLine($"| {recordCountRow[0].PadRight(columnWidth - 1)}| {recordCountRow[1].PadRight(valueWidth - 1)}|");
        Console.WriteLine($"| {elapsedTimeRow[0].PadRight(columnWidth - 1)}| {elapsedTimeRow[1].PadRight(valueWidth - 1)}|");
        Console.WriteLine(separator);

        PressAnyKeyToContinue();
    }

    void CreateIndex()
    {
        Console.WriteLine($"Enter path to index (default: {DefaultIndexPath}):");
        var indexPath = Console.ReadLine();
        if (string.IsNullOrEmpty(indexPath)) indexPath = DefaultIndexPath;
        Console.WriteLine("Index path:" + indexPath);
        Console.WriteLine($"Enter pattern to index (default: {DefaultPattern}):");
        var pattern = Console.ReadLine();
        if (string.IsNullOrEmpty(pattern)) pattern = DefaultPattern;

        var sw = Stopwatch.StartNew();
        var folderIterator = new FolderIterator(indexPath, pattern, true);
        var nextRecord = RecordTable.GetLastRecord() ?? 1;
        Console.WriteLine("nextRecord: " + nextRecord);
        var totalRecordUpserted = 0;

        var cancellationTokenSource = new CancellationTokenSource();
        var task = Task.Run(() =>
        {
            var iteratorTask = folderIterator.IterateAll(
                (path) =>
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                        return Task.CompletedTask;
                    return Task.Run(async () =>
                    {
                        try
                        {
                            if (cancellationTokenSource.IsCancellationRequested) return;
                            if (!RecordTable.TryGetRecord(path, out var record))
                                record = Interlocked.Increment(ref nextRecord);

                            var text = await File.ReadAllTextAsync(path);
                            RecordTable.UpsertRecord(record, path);
                            SearchEngine.AddRecord(record, text);
                            Interlocked.Increment(ref totalRecordUpserted);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            throw;
                        }
                    });
                },
                cancellationTokenSource.Token);
            iteratorTask.Wait();
            sw.Stop();
            Console.WriteLine($"Created {totalRecordUpserted} records in: " + sw.ElapsedMilliseconds + " ms");

            sw.Restart();
            SearchEngine.Index.EvictToDisk();
            Console.WriteLine("Waiting for background threads...");
            SearchEngine.Index.WaitForBackgroundThreads();
            Console.WriteLine("Merging completed in: " + sw.ElapsedMilliseconds + " ms");
            if (cancellationTokenSource.IsCancellationRequested)
            {
                Console.WriteLine("Press any key to return to the main menu...");
                Console.ReadKey();
            }
        });
        Console.WriteLine("Creating the index...");
        Console.WriteLine("Press any key to quit the index creation...");
        Console.ReadKey();
        cancellationTokenSource.Cancel();
        task.Wait();
    }

    void Search()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Start with '-' to delete the search results.");
            Console.WriteLine("Enter search query (or 'q' to return to main menu):");
            var text = Console.ReadLine();
            if (text.Equals("q", StringComparison.InvariantCultureIgnoreCase)) break;

            var isDeleteRequest = false;
            if (text.StartsWith('-'))
            {
                text = text.Substring(1);
                isDeleteRequest = true;
            }

            var sw = Stopwatch.StartNew();
            var result = SearchEngine.Search(text, true, 0, 0);
            var elapsed = sw.ElapsedMilliseconds;
            Console.WriteLine($"Found {result.Length} records in {elapsed} ms.");

            if (isDeleteRequest)
            {
                sw.Restart();
                var sum = 0L;
                Parallel.ForEach(result, record =>
                {
                    var a = SearchEngine.DeleteRecord(record);
                    Interlocked.Add(ref sum, a);
                });
                elapsed = sw.ElapsedMilliseconds;
                Console.WriteLine($"Deleted {result.Length} / ({sum}) records in {elapsed} ms.");
            }

            var i = 1;
            foreach (var record in result)
            {
                RecordTable.TryGetValue(record, out var path);
                Console.WriteLine($"{i}. {path}");

                if (i % 10 == 0)
                {
                    Console.WriteLine("Press 'Enter' to continue viewing the next set of records, or 'q' to return to the main menu.");
                    var key = Console.ReadKey().KeyChar;
                    if (key == 'q' || key == 'Q') return;
                }
                ++i;
            }

            Console.WriteLine("End of results. Press any key to perform another search or 'q' to return to the main menu...");
            if (Console.ReadKey().KeyChar == 'q') break;
        }
    }

    public void Dispose()
    {
        if (!SearchEngine.Index.IsIndexDropped)
            SearchEngine.Dispose();
        if (!RecordTable.IsDropped)
            RecordTable.Dispose();
    }
}

