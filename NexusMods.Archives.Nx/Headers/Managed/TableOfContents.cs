using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers.Enums;
using NexusMods.Archives.Nx.Headers.Structs;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Headers.Managed;

/// <summary>
///     Managed representation of the table of contents.
/// </summary>
public class TableOfContents : IDisposable
{
    private bool _disposed;

    // pointers & non-primitives

    /// <summary>
    ///     Used formats for compression of each block.
    /// </summary>
    public required CompressionPreference[] BlockCompressions;

    /// <summary>
    ///     Individual block sizes in this structure.
    /// </summary>
    public required BlockSize[] Blocks;

    /// <summary>
    ///     Individual file entries.
    /// </summary>
    public required FileEntry[] Entries;

    /// <summary>
    ///     String pool data.
    /// </summary>
    public ArrayRentalSlice PoolData;

    // primitives

    /// <summary>
    ///     Size of an individual block, in bytes.
    /// </summary>
    public int BlockSize;

    /// <summary>
    ///     Size of large files.
    /// </summary>
    public int LargeFileChunkSize;

    /// <summary>
    ///     Size of the table (after serialization) in bytes.
    /// </summary>
    public int Size;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        PoolData.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    ~TableOfContents() => Dispose();

    /// <summary>
    ///     Calculates the size of the table after serialization to binary.
    /// </summary>
    /// <param name="version">Version to serialize into.</param>
    public void CalculateTableSize(ArchiveVersion version) { }
}
