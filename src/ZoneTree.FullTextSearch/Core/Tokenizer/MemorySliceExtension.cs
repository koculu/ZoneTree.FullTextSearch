namespace ZoneTree.FullTextSearch.Core.Tokenizer;

/// <summary>
/// Provides extension methods for working with <see cref="ReadOnlyMemory{T}"/> instances.
/// </summary>
public static class MemorySliceExtension
{
    /// <summary>
    /// Returns a slice of the specified <see cref="ReadOnlyMemory{T}"/> using the given <see cref="Slice"/> structure.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the memory.</typeparam>
    /// <param name="memory">The <see cref="ReadOnlyMemory{T}"/> instance to slice.</param>
    /// <param name="slice">The <see cref="Slice"/> structure that specifies the offset and length of the slice.</param>
    /// <returns>A <see cref="ReadOnlyMemory{T}"/> representing the slice of the original memory.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the specified slice extends beyond the bounds of the original memory.
    /// </exception>
    public static ReadOnlyMemory<T> Slice<T>(this ReadOnlyMemory<T> memory, Slice slice)
    {
        return memory.Slice(slice.Offset, slice.Length);
    }
}