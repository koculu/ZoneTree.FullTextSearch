using System.Runtime.InteropServices;

namespace ZoneTree.FullTextSearch.Core.Model;

/// <summary>
/// Represents a 4-character n-gram token that can be accessed either as a 64-bit unsigned integer
/// or as individual characters. This struct is designed for efficient storage and manipulation of 
/// 4-character sequences using a packed memory layout.
/// </summary>
[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Pack = 1, Size = 8)]
public struct NGramToken4
{
    /// <summary>
    /// The raw data representing the 4-character n-gram as a 64-bit unsigned integer.
    /// This field overlaps with the individual character fields.
    /// </summary>
    [FieldOffset(0)]
    public ulong data;

    /// <summary>
    /// The first character of the 4-character n-gram.
    /// </summary>
    [FieldOffset(0)]
    public char c0;

    /// <summary>
    /// The second character of the 4-character n-gram.
    /// </summary>
    [FieldOffset(2)]
    public char c1;

    /// <summary>
    /// The third character of the 4-character n-gram.
    /// </summary>
    [FieldOffset(4)]
    public char c3;

    /// <summary>
    /// The fourth character of the 4-character n-gram.
    /// </summary>
    [FieldOffset(6)]
    public char c4;
}
