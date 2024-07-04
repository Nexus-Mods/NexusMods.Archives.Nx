using NexusMods.Archives.Nx.Interfaces;

namespace NexusMods.Archives.Nx.FileProviders.FileData;

/// <summary>
///     Implementation of <see cref="IFileData" /> backed up by a decompressed Nx block
///     (see <see cref="LazyRefCounterDecompressedNxBlock"/>).
/// </summary>
internal class DecompressedNxBlockFileData : IFileData
{
    /// <inheritdoc />
    public unsafe byte* Data { get; init; }

    /// <inheritdoc />
    public ulong DataLength { get; init; }

    private readonly LazyRefCounterDecompressedNxBlock _block;

    /// <summary>
    ///     Creates an <see cref="IFileData"/> that is derivative from a decompressed Nx block.
    /// </summary>
    /// <param name="block">Shared block reference.</param>
    /// <param name="fileStartOffset">Offset to the start of the file.</param>
    /// <param name="fileLength">Length of the file.</param>
    public unsafe DecompressedNxBlockFileData(LazyRefCounterDecompressedNxBlock block, int fileStartOffset, ulong fileLength)
    {
        _block = block;
        Data = (byte*)_block.GetData() + fileStartOffset;
        DataLength = fileLength;
        block.Acquire();
    }

    /// <inheritdoc />
    ~DecompressedNxBlockFileData() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        _block.Release();
        GC.SuppressFinalize(this);
    }
}
