using Tenray.ZoneTree.Comparers;

namespace ZoneTree.FullTextSearch.Model;

/// <summary>
/// Provides a comparer for instances of <see cref="CompositeKeyOfTokenRecordPrevious{TRecord, TToken}"/>.
/// The comparer evaluates composite keys based on their token, record, and previous token components, in that order.
/// </summary>
/// <typeparam name="TRecord">The type of the record component of the composite key. Must be an unmanaged type.</typeparam>
/// <typeparam name="TToken">The type of the token component of the composite key. Must be an unmanaged type.</typeparam>
public sealed class CompositeKeyOfTokenRecordPreviousComparer<TRecord, TToken>
    : IRefComparer<CompositeKeyOfTokenRecordPrevious<TRecord, TToken>>
    where TRecord : unmanaged
    where TToken : unmanaged
{
    /// <summary>
    /// Gets the comparer used to compare the record components of the composite keys.
    /// </summary>
    public IRefComparer<TRecord> RecordComparer { get; }

    /// <summary>
    /// Gets the comparer used to compare the token components of the composite keys.
    /// </summary>
    public IRefComparer<TToken> TokenComparer { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeKeyOfTokenRecordPreviousComparer{TRecord, TToken}"/> class
    /// with the specified comparers for the record and token components.
    /// </summary>
    /// <param name="recordComparer">The comparer to use for comparing the record components.</param>
    /// <param name="tokenComparer">The comparer to use for comparing the token components.</param>
    public CompositeKeyOfTokenRecordPreviousComparer(
        IRefComparer<TRecord> recordComparer,
        IRefComparer<TToken> tokenComparer)
    {
        RecordComparer = recordComparer;
        TokenComparer = tokenComparer;
    }

    /// <summary>
    /// Compares two <see cref="CompositeKeyOfTokenRecordPrevious{TRecord, TToken}"/> instances.
    /// The comparison is performed first on the token components, then on the record components if the tokens are equal,
    /// and finally on the previous token components if both the tokens and records are equal.
    /// </summary>
    /// <param name="x">The first composite key to compare.</param>
    /// <param name="y">The second composite key to compare.</param>
    /// <returns>
    /// An integer indicating the relative order of the two composite keys:
    /// <list type="bullet">
    /// <item><description>Less than zero if <paramref name="x"/> is less than <paramref name="y"/>.</description></item>
    /// <item><description>Zero if <paramref name="x"/> is equal to <paramref name="y"/>.</description></item>
    /// <item><description>Greater than zero if <paramref name="x"/> is greater than <paramref name="y"/>.</description></item>
    /// </list>
    /// </returns>
    public int Compare(
        in CompositeKeyOfTokenRecordPrevious<TRecord, TToken> x,
        in CompositeKeyOfTokenRecordPrevious<TRecord, TToken> y)
    {
        var tokenComparer = TokenComparer;
        var hx = x.Token;
        var hy = y.Token;
        var hc = tokenComparer.Compare(hx, hy);
        if (hc != 0) return hc;
        var rc = RecordComparer.Compare(x.Record, y.Record);
        if (rc != 0) return rc;
        var px = x.PreviousToken;
        var py = y.PreviousToken;
        return tokenComparer.Compare(px, py);
    }
}