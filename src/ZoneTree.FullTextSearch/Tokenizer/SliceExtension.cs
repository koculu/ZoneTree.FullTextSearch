namespace ZoneTree.FullTextSearch.Tokenizer;

/// <summary>
/// A static class that provides extension methods for slicing operations on ReadOnlyMemory and ReadOnlySpan.
/// </summary>
public static class SliceExtension
{
    /// <summary>
    /// Slices the specified ReadOnlyMemory using the provided Slice object.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the ReadOnlyMemory.</typeparam>
    /// <param name="memory">The ReadOnlyMemory to be sliced.</param>
    /// <param name="slice">An instance of the Slice class containing the offset and length for slicing.</param>
    /// <returns>A sliced ReadOnlyMemory segment according to the specified offset and length.</returns>
    public static ReadOnlyMemory<T> Slice<T>(this ReadOnlyMemory<T> memory, Slice slice)
    {
        return memory.Slice(slice.Offset, slice.Length);
    }

    /// <summary>
    /// Slices the specified ReadOnlySpan using the provided Slice object.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the ReadOnlySpan.</typeparam>
    /// <param name="readonlySpan">The ReadOnlySpan to be sliced.</param>
    /// <param name="slice">An instance of the Slice class containing the offset and length for slicing.</param>
    /// <returns>A sliced ReadOnlySpan segment according to the specified offset and length.</returns>
    public static ReadOnlySpan<T> Slice<T>(this ReadOnlySpan<T> readonlySpan, Slice slice)
    {
        return readonlySpan.Slice(slice.Offset, slice.Length);
    }
}