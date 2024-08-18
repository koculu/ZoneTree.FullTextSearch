namespace ZoneTree.FullTextSearch.Core.Tokenizer;

/// <summary>
/// Represents a slice of text with a specified offset and length. 
/// This is commonly used to denote the position and size of a token within a larger string.
/// </summary>
/// <param name="Offset">The starting position of the slice within the text.</param>
/// <param name="Length">The length of the slice.</param>
public readonly record struct Slice(int Offset, int Length);
