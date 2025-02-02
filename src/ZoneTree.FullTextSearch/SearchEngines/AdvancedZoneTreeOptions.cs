using Tenray.ZoneTree;
using ZoneTree.FullTextSearch.Model;
using Tenray.ZoneTree.AbstractFileStream;

namespace ZoneTree.FullTextSearch.SearchEngines;

/// <summary>
/// Provides advanced options for configuring the underlying ZoneTree instances 
/// used in the full-text search engine.
/// </summary>
/// <typeparam name="TRecord">
/// The type of the record being indexed.
/// </typeparam>
/// <typeparam name="TToken">
/// The type of the token used for hashing and indexing.
/// </typeparam>
public sealed class AdvancedZoneTreeOptions<TRecord, TToken>
    where TRecord : unmanaged
    where TToken : unmanaged
{
    /// <summary>
    /// Gets or sets the <see cref="IFileStreamProvider"/> used to manage file streams 
    /// for storing ZoneTree data. If this is <c>null</c>, the default implementation 
    /// provided by ZoneTree will be used.
    /// </summary>
    public IFileStreamProvider FileStreamProvider { get; set; }

    /// <summary>
    /// Gets or sets an optional delegate that configures the <see cref="ZoneTreeFactory{TKey,TValue}"/> 
    /// for the <see cref="CompositeKeyOfTokenRecordPrevious{TRecord, TToken}"/> keys 
    /// and <see cref="byte"/> values.
    /// <para>
    /// This is called before the factory builds its internal ZoneTree. You can use it 
    /// to configure advanced settings such as in-memory or on-disk data paths, caching,
    /// block sizes, compression, or other low-level ZoneTree behaviors.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This configurator applies specifically to data indexed by the hashed token 
    /// combined with the "previous token" to enforce token order. 
    /// </remarks>
    public Action<
            ZoneTreeFactory<
                CompositeKeyOfTokenRecordPrevious<TRecord, TToken>,
                byte>> FactoryConfigurator1
    { get; set; }

    /// <summary>
    /// Gets or sets an optional delegate that configures the <see cref="ZoneTreeFactory{TKey,TValue}"/>
    /// for the <see cref="CompositeKeyOfRecordToken{TRecord, TToken}"/> keys and <see cref="byte"/> values.
    /// <para>
    /// Similar to <see cref="FactoryConfigurator1"/>, this is invoked before the factory
    /// completes its setup, allowing custom adjustments for storage, caching, and other
    /// advanced ZoneTree features.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This configurator applies specifically to data indexed by the record combined 
    /// with the token, often used for efficient record deletion or secondary indexing.
    /// </remarks>
    public Action<
        ZoneTreeFactory<
            CompositeKeyOfRecordToken<TRecord, TToken>,
            byte>> FactoryConfigurator2
    { get; set; }
}
