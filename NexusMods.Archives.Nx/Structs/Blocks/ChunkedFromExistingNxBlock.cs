using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;
using static NexusMods.Archives.Nx.Structs.Blocks.ChunkedCommon;

namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     A block that represents a slice of an existing file.
///     This is a special variant of <see cref="NexusMods.Archives.Nx.Structs.Blocks.ChunkedFileBlock{T}"/>
///     which is backed by blocks within an existing Nx archive.
/// </summary>
/// <typeparam name="T">Type of item stored inside the block.</typeparam>
/// <param name="StartOffset">Start offset of the compressed data in existing file.</param>
/// <param name="ChunkIndex">Zero based index of this chunk.</param>
/// <param name="ChunkSize">Size of the compressed data segment.</param>
/// <param name="State">Stores the shared state of all chunks.</param>
/// <param name="Compression">Stores the shared state of all chunks.</param>
internal record ChunkedFileFromExistingNxBlock<T>
    (ulong StartOffset, int ChunkSize, int ChunkIndex, ChunkedBlockFromExistingNxState State, CompressionPreference Compression) : IBlock<T>
    where T : IHasFileSize, ICanProvideFileData, IHasRelativePath
{
    /// <inheritdoc />
    public ulong LargestItemSize() => State.FileLength;

    /// <inheritdoc />
    public bool CanCreateChunks() => true;

    /// <inheritdoc />
    public int FileCount() => ChunkIndex == 0 ? 1 : 0; // Only first chunk should be counted.

    /// <inheritdoc />
    public void AppendFilesUnsafe(ref int currentIndex, HasRelativePathWrapper[] paths)
    {
        if (ChunkIndex == 0)
            paths.DangerousGetReferenceAt(currentIndex++) = State.RelativePath;
    }

    /// <inheritdoc />
    public void ProcessBlock(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools)
    {
        ProcessBlockWithoutDeduplication(tocBuilder, settings, blockIndex, pools);

        /*
           var isDedupeEnabled = settings.ChunkedDeduplicationState != null;

           if (!isDedupeEnabled)
           {
               return;
           }

        if (ChunkIndex == 0)
            ProcessFirstChunkDedupe(tocBuilder, settings, blockIndex, pools);
        else
            ProcessRemainingChunksDedupe(tocBuilder, settings, blockIndex, pools);
        */
    }

    private unsafe void ProcessBlockWithoutDeduplication(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools)
    {
        using var data = State.NxSource.GetFileData(StartOffset, (uint)ChunkSize);
        var dataSpan = new Span<byte>(data.Data, (int)data.DataLength);

        // Take lock.
        BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);
        WriteBlock(dataSpan, tocBuilder, settings, blockIndex, false, Compression);

        if (IsLastChunk(State.NumChunks, ChunkIndex))
            AddFileEntryToTocAtomic(tocBuilder, FirstBlockIndex(blockIndex, State.NumChunks), State.FileHash, State.RelativePath, State.FileLength);

        BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
    }
}

/// <summary>
///     This item stores the shared state of all <see cref="ChunkedFileFromExistingNxBlock{T}"/> chunks.
///     This is a modified variant of <see cref="ChunkedBlockState{T}"/> which skips some unnecessary
///     hashing operations as we already know the hash of the input.
/// </summary>
internal class ChunkedBlockFromExistingNxState
{
    /// <summary>
    ///     Number of total chunks in this chunked block.
    /// </summary>
    public required int NumChunks { get; init; }

    /// <summary>
    ///     Provides access to the raw Nx file.
    /// </summary>
    public required IFileDataProvider NxSource { get; init; }

    /// <summary>
    ///     The relative path of the file in the Nx archive.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    ///     The length of the decompressed file in bytes.
    /// </summary>
    public required ulong FileLength { get; init; }

    /// <summary>
    ///     Known hash from existing file in original Nx archive.
    /// </summary>
    public required ulong FileHash { get; init; }
}
