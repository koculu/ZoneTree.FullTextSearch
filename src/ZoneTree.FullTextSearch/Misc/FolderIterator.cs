namespace ZoneTree.FullTextSearch.Misc;

/// <summary>
/// Provides functionality to iterate through files in a specified directory based on a search pattern,
/// with support for asynchronous processing and recursive directory traversal.
/// </summary>
public sealed class FolderIterator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FolderIterator"/> class with the specified path, search pattern, and recursion option.
    /// </summary>
    /// <param name="path">The path of the directory to iterate through.</param>
    /// <param name="searchPattern">The search pattern to match against the names of files in the directory.</param>
    /// <param name="isRecursive">Indicates whether the search should include subdirectories.</param>
    public FolderIterator(string path, string searchPattern, bool isRecursive)
    {
        Path = path;
        SearchPattern = searchPattern;
        IsRecursive = isRecursive;
    }

    /// <summary>
    /// Gets the path of the directory to iterate through.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the search pattern used to match against the names of files in the directory.
    /// </summary>
    public string SearchPattern { get; }

    /// <summary>
    /// Gets a value indicating whether the iteration includes subdirectories.
    /// </summary>
    public bool IsRecursive { get; }

    /// <summary>
    /// Asynchronously iterates through all files in the directory that match the search pattern,
    /// and invokes a callback function on each file.
    /// </summary>
    /// <param name="callback">A function to be invoked for each file found. The function receives the file path as an argument.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is null.</exception>
    public async Task IterateAll(Func<string, Task> callback, CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            var paths = Directory.EnumerateFiles(Path, SearchPattern, new EnumerationOptions()
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            });
            var tasks = new List<Task>();
            foreach (var path in paths)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Cancelled the folder iteration.");
                    break;
                }
                tasks.Add(callback(path));
            }
            await Task.WhenAll(tasks.ToArray());
        });
    }
}
