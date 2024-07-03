using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;

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
    (nuint StartOffset, int ChunkSize, int ChunkIndex, ChunkedBlockFromExistingNxState<T> State, CompressionPreference Compression) : IBlock<T>
    where T : IHasFileSize, ICanProvideFileData, IHasRelativePath
{
    /// <inheritdoc />
    public ulong LargestItemSize() => (ulong)State.NxSource.FileSize;

    /// <inheritdoc />
    public bool CanCreateChunks() => true;

    /// <inheritdoc />
    public void ProcessBlock(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools)
    {
        unsafe
        {
            using var data = State.NxSource.FileDataProvider.GetFileData(StartOffset, (uint)ChunkSize);
            var dataSpan = new Span<byte>(data.Data, (int)data.DataLength);
            State.UpdateState(ChunkIndex, dataSpan, tocBuilder, settings, blockIndex, Compression);
        }
    }
}

/// <summary>
///     This item stores the shared state of all <see cref="ChunkedFileFromExistingNxBlock{T}"/> chunks.
///     This is a modified variant of <see cref="ChunkedBlockState{T}"/> which skips some unnecessary
///     hashing operations as we already know the hash of the input.
/// </summary>
internal class ChunkedBlockFromExistingNxState<T> where T : IHasFileSize, ICanProvideFileData, IHasRelativePath
{
    /// <summary>
    ///     Number of total chunks in this chunked block.
    /// </summary>
    public int NumChunks = 0;

    /// <summary>
    ///     Source Nx archive associated with this chunked block.
    /// </summary>
    public T NxSource { get; init; } = default!;

    /// <summary>
    ///     The length of the decompressed file in bytes.
    /// </summary>
    public ulong FileLength { get; init; }

    /// <summary>
    ///     Known hash from existing file in original Nx archive.
    /// </summary>
    public ulong FileHash { get; init; }

    /// <summary>
    ///     Sets the specific index as processed and updates internal state.
    /// </summary>
    /// <param name="chunkIndex">The index to set.</param>
    /// <param name="blockData">The compressed data for the block.</param>
    /// <param name="tocBuilder">Builds table of contents.</param>
    /// <param name="settings">Packer settings.</param>
    /// <param name="blockIndex">Index of currently packed block.</param>
    /// <param name="compression">Type of compression used by the <paramref name="blockData"/>.</param>
    public void UpdateState(int chunkIndex, Span<byte> blockData, TableOfContentsBuilder<T> tocBuilder,
        PackerSettings settings, int blockIndex, CompressionPreference compression)
    {
        // Write out actual block.
        BlockHelpers.StartProcessingBlock(tocBuilder, blockIndex);
        BlockHelpers.WriteToOutput(settings.Output, blockData);

        // Update Block Details
        var toc = tocBuilder.Toc;
        ref var blockSize = ref toc.Blocks.DangerousGetReferenceAt(blockIndex);
        blockSize.CompressedSize = blockData.Length;

        ref var blockCompression = ref toc.BlockCompressions.DangerousGetReferenceAt(blockIndex);
        blockCompression = compression;

        // Update file details once all chunks are done.
        if (chunkIndex != NumChunks - 1)
        {
            BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
            return;
        }

        // Only executed on final thread, so we can end and increment early.
        BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
        ref var file = ref tocBuilder.GetAndIncrementFileAtomic();
        file.FilePathIndex = tocBuilder.FileNameToIndexDictionary[NxSource.RelativePath];
        file.FirstBlockIndex = blockIndex + 1 - NumChunks; // All chunks (blocks) are sequentially queued/written.
        file.DecompressedSize = FileLength;
        file.DecompressedBlockOffset = 0;
        file.Hash = FileHash;
    }
}
