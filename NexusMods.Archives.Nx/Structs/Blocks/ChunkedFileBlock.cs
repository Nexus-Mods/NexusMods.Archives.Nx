using NexusMods.Archives.Nx.Enums;

namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     A block that represents a slice of an existing file.
/// </summary>
/// <typeparam name="T">Type of item stored inside the block.</typeparam>
/// <param name="File">File associated with the chunked block.</param>
/// <param name="StartOffset">Start offset of the file.</param>
/// <param name="ChunkSize">Size of the file segment.</param>
/// <param name="Compression">Compression method to use.</param>
public record ChunkedFileBlock<T>
    (T File, long StartOffset, int ChunkSize, CompressionPreference Compression) : IBlock<T>
{
    /// <inheritdoc />
    public IEnumerable<T> GetAllItems()
    {
        yield return File;
    }
}
