using System.Runtime.InteropServices;

namespace NexusMods.Archives.Nx.Headers.Structs;

/// <summary>
///     Represents an individual block in the TOC.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BlockSize
{
    // Note: Max Block Size 512MB so this is ok as int.

    /// <summary>
    ///     Compressed size of the block.
    /// </summary>
    public int CompressedSize;

    /// <summary>
    ///     Creates a blocksize with a specified compressed size.
    /// </summary>
    public BlockSize(int compressedSize) => CompressedSize = compressedSize;
}
