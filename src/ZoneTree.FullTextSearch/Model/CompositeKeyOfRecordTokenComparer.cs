using Tenray.ZoneTree.Comparers;

namespace ZoneTree.FullTextSearch.Model;

/// <summary>
/// A comparer for comparing instances of <see cref="CompositeKeyOfRecordToken{TRecord, TToken}"/>.
/// This comparer compares composite keys based on their record and token components.
/// </summary>
/// <typeparam name="TRecord">The type of the record component of the composite key. Must be an unmanaged type.</typeparam>
/// <typeparam name="TToken">The type of the token component of the composite key. Must be an unmanaged type.</typeparam>
public sealed class CompositeKeyOfRecordTokenComparer<TRecord, TToken>
    : IRefComparer<CompositeKeyOfRecordToken<TRecord, TToken>>
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
    /// Initializes a new instance of the <see cref="CompositeKeyOfRecordTokenComparer{TRecord, TToken}"/> class.
    /// </summary>
    /// <param name="recordComparer">The comparer to use for comparing the record components.</param>
    /// <param name="tokenComparer">The comparer to use for comparing the token components.</param>
    public CompositeKeyOfRecordTokenComparer(
        IRefComparer<TRecord> recordComparer,
        IRefComparer<TToken> tokenComparer)
    {
        RecordComparer = recordComparer;
        TokenComparer = tokenComparer;
    }

    /// <summary>
    /// Compares two <see cref="CompositeKeyOfRecordToken{TRecord, TToken}"/> instances.
    /// First, it compares the record components using <see cref="RecordComparer"/>.
    /// If the records are equal, it then compares the token components using <see cref="TokenComparer"/>.
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
        in CompositeKeyOfRecordToken<TRecord, TToken> x,
        in CompositeKeyOfRecordToken<TRecord, TToken> y)
    {
        var rc = RecordComparer.Compare(x.Record, y.Record);
        if (rc != 0) return rc;
        var hx = x.Token;
        var hy = y.Token;
        return TokenComparer.Compare(hx, hy);
    }
}