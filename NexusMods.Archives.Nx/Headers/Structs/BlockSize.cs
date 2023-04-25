namespace NexusMods.Archives.Nx.Headers.Structs;

/// <summary>
///     Represents an individual block in the TOC.
/// </summary>
public struct BlockSize
{
    /// <summary>
    ///     Compressed size of the block.
    /// </summary>
    public long CompressedSize;

    /// <summary>
    ///     Creates a blocksize with a specified compressed size.
    /// </summary>
    public BlockSize(long compressedSize) => CompressedSize = compressedSize;
}
