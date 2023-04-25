using NexusMods.Archives.Nx.Enums;

namespace NexusMods.Archives.Nx.Traits;

/// <summary>
///     Trait for an item which declares a preferred approach for being compressed..
/// </summary>
public interface IHasCompressionPreference
{
    /// <summary>
    ///     Preferred algorithm to compress the item with.
    /// </summary>
    public CompressionPreference CompressionPreference { get; }
}
