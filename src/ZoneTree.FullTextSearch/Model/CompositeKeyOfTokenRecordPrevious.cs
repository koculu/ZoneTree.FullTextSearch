using System.Runtime.InteropServices;

namespace ZoneTree.FullTextSearch;

/// <summary>
/// Represents a composite key that combines a token, a record, and the previous token.
/// This struct is used for indexing and searching where the relationship between tokens
/// and records, as well as the sequence of tokens, is important.
/// </summary>
/// <typeparam name="TRecord">The type of the record component of the key. Must be an unmanaged type.</typeparam>
/// <typeparam name="TToken">The type of the token components of the key. Must be an unmanaged type.</typeparam>
[StructLayout(LayoutKind.Sequential)]
public struct CompositeKeyOfTokenRecordPrevious<TRecord, TToken>
    : IEquatable<CompositeKeyOfTokenRecordPrevious<TRecord, TToken>>
    where TRecord : unmanaged
    where TToken : unmanaged
{
    /// <summary>
    /// The token component of the composite key. This part of the key represents the current token
    /// associated with the record.
    /// </summary>
    public TToken Token;

    /// <summary>
    /// The record component of the composite key. This part of the key identifies the specific record
    /// associated with the token.
    /// </summary>
    public TRecord Record;

    /// <summary>
    /// The previous token component of the composite key. This part of the key represents the token
    /// that immediately precedes the current token in the sequence, providing context for token order.
    /// </summary>
    public TToken PreviousToken;

    public override bool Equals(object obj)
    {
        return obj is CompositeKeyOfTokenRecordPrevious<TRecord, TToken> previous && Equals(previous);
    }

    public bool Equals(CompositeKeyOfTokenRecordPrevious<TRecord, TToken> other)
    {
        return EqualityComparer<TToken>.Default.Equals(Token, other.Token) &&
               EqualityComparer<TRecord>.Default.Equals(Record, other.Record) &&
               EqualityComparer<TToken>.Default.Equals(PreviousToken, other.PreviousToken);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Token, Record, PreviousToken);
    }

    public static bool operator ==(CompositeKeyOfTokenRecordPrevious<TRecord, TToken> left, CompositeKeyOfTokenRecordPrevious<TRecord, TToken> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CompositeKeyOfTokenRecordPrevious<TRecord, TToken> left, CompositeKeyOfTokenRecordPrevious<TRecord, TToken> right)
    {
        return !(left == right);
    }
}
