using System.Runtime.InteropServices;

namespace ZoneTree.FullTextSearch.Model;

/// <summary>
/// Represents a composite key that combines a record and a token. This struct is used
/// for indexing and searching within a data structure, where both the record and the token
/// are required to uniquely identify an entry.
/// </summary>
/// <typeparam name="TRecord">The type of the record component of the key. Must be an unmanaged type.</typeparam>
/// <typeparam name="TToken">The type of the token component of the key. Must be an unmanaged type.</typeparam>
[StructLayout(LayoutKind.Sequential)]
public struct CompositeKeyOfRecordToken<TRecord, TToken>
    : IEquatable<CompositeKeyOfRecordToken<TRecord, TToken>>
    where TRecord : unmanaged
    where TToken : unmanaged
{
    /// <summary>
    /// The record component of the composite key. This part of the key identifies the specific record.
    /// </summary>
    public TRecord Record;

    /// <summary>
    /// The token component of the composite key. This part of the key represents the token
    /// associated with the record, used to differentiate records or to index them based on the token.
    /// </summary>
    public TToken Token;

    public override bool Equals(object obj)
    {
        return obj is CompositeKeyOfRecordToken<TRecord, TToken> token && Equals(token);
    }

    public bool Equals(CompositeKeyOfRecordToken<TRecord, TToken> other)
    {
        return EqualityComparer<TRecord>.Default.Equals(Record, other.Record) &&
               EqualityComparer<TToken>.Default.Equals(Token, other.Token);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Record, Token);
    }

    public static bool operator ==(CompositeKeyOfRecordToken<TRecord, TToken> left, CompositeKeyOfRecordToken<TRecord, TToken> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CompositeKeyOfRecordToken<TRecord, TToken> left, CompositeKeyOfRecordToken<TRecord, TToken> right)
    {
        return !(left == right);
    }
}
