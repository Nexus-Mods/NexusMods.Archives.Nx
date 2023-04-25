using NexusMods.Archives.Nx.Enums;

namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     Represents an individual SOLID block.
/// </summary>
/// <param name="Items">Items tied to the given block.</param>
/// <param name="Compression">Compression method to use.</param>
public record SolidBlock<T>(List<T> Items, CompressionPreference Compression) : IBlock<T>
{
    /// <inheritdoc />
    public IEnumerable<T> GetAllItems()
    {
        foreach (var item in Items)
            yield return item;
    }
}
