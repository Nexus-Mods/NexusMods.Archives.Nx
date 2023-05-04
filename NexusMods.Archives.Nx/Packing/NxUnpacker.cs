namespace NexusMods.Archives.Nx.Packing;

/// <summary>
///     Utility for unpacking `.nx` files.
/// </summary>
public class NxUnpacker
{
    /// <summary>
    ///     Stores the raw offsets of the compressed blocks.
    /// </summary>
    public required long[] BlockOffsets { get; init; }
}
