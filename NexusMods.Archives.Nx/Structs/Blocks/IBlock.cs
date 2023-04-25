using NexusMods.Archives.Nx.Enums;

namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     Represents an individual block..
/// </summary>
// ReSharper disable once UnusedTypeParameter
public interface IBlock<out T>
{
    /// <summary>
    ///     Retrieves all <typeparamref name="T" /> items associated with this block.
    /// </summary>
    public IEnumerable<T> GetAllItems();

    /// <summary>
    ///     Compression method used to pack the block.
    /// </summary>
    public CompressionPreference Compression { get; }
}
