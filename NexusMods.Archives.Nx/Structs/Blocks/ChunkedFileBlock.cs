using System.Diagnostics;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     A block that represents a slice of an existing file.
/// </summary>
/// <typeparam name="T">Type of item stored inside the block.</typeparam>
/// <param name="StartOffset">Start offset of the file.</param>
/// <param name="ChunkIndex">Zero based index of this chunk.</param>
/// <param name="ChunkSize">Size of the file segment.</param>
/// <param name="State">Stores the shared state of all chunks.</param>
internal record ChunkedFileBlock<T>
    (long StartOffset, int ChunkSize, int ChunkIndex, ChunkedBlockState<T> State) : IBlock<T>
    where T : IHasFileSize, ICanProvideFileData, IHasRelativePath
{
    /// <inheritdoc />
    public ulong LargestItemSize() => (ulong)State.File.FileSize;

    /// <inheritdoc />
    public bool CanCreateChunks() => true;

    /// <inheritdoc />
    public unsafe void ProcessBlock(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools)
    {
        using var allocation = pools.ChunkPool.Rent(settings.ChunkSize);
        var allocSpan = allocation.Span;
        fixed (byte* allocationPtr = allocSpan)
        {
            // Compress the block
            using var data = State.File.FileDataProvider.GetFileData(StartOffset, (uint)ChunkSize);
            var compressed = BlockHelpers.Compress(Compression, settings.ChunkedCompressionLevel, data, allocationPtr, allocSpan.Length,
                out var asCopy);
            State.UpdateState(ChunkIndex, allocation, compressed, tocBuilder, settings, blockIndex, new Span<byte>(data.Data, (int)data.DataLength),
                asCopy);
        }
    }

    /// <inheritdoc />
    public CompressionPreference Compression => State.Compression;
}

/// <summary>
///     This item stores the shared state of all chunks.
/// </summary>
internal class ChunkedBlockState<T> where T : IHasFileSize, ICanProvideFileData, IHasRelativePath
{
    /// <summary>
    ///     Compression used by all chunks of this file.
    /// </summary>
    public CompressionPreference Compression;

    /// <summary>
    ///     Number of total chunks in this chunked block.
    /// </summary>
    public int NumChunks = 0;

    /// <summary>
    ///     File associated with this chunked block.
    /// </summary>
    public T File { get; init; } = default!;

    /// <summary>
    ///     Instance of the Microsoft xxHash64 hasher.
    /// </summary>
    private XxHash64Algorithm _hash = new();

    /// <summary>
    ///     Sets the specific index as processed and updates internal state.
    /// </summary>
    /// <param name="chunkIndex">The index to set.</param>
    /// <param name="compData">The compressed data for block.</param>
    /// <param name="compressedSize">Size of the data after compression.</param>
    /// <param name="tocBuilder">Builds table of contents.</param>
    /// <param name="settings">Packer settings.</param>
    /// <param name="blockIndex">Index of currently packed block.</param>
    /// <param name="rawChunkData">Raw data of decompressed chunk (before compression)</param>
    /// <param name="asCopy">Whether block was compressed using 'copy' compression.</param>
    public void UpdateState(int chunkIndex, PackerPoolRental compData, int compressedSize, TableOfContentsBuilder<T> tocBuilder,
        PackerSettings settings, int blockIndex, Span<byte> rawChunkData, bool asCopy)
    {
        UpdateHash(rawChunkData);

        // Write out actual block.
        BlockHelpers.WriteToOutputLocked(tocBuilder, blockIndex, settings.Output, compData, compressedSize, settings.Progress);

        // Update Block Details
        var toc = tocBuilder.Toc;
        ref var blockSize = ref toc.Blocks.DangerousGetReferenceAt(blockIndex);
        blockSize.CompressedSize = compressedSize;

        ref var blockCompression = ref toc.BlockCompressions.DangerousGetReferenceAt(blockIndex);
        blockCompression = asCopy ? CompressionPreference.Copy : Compression;

        // Update file details once all chunks are done..
        if (chunkIndex != NumChunks - 1)
            return;

        ref var file = ref tocBuilder.GetAndIncrementFileAtomic();
        file.FilePathIndex = tocBuilder.FileNameToIndexDictionary[File.RelativePath];
        file.FirstBlockIndex = blockIndex + 1 - NumChunks; // All chunks (blocks) are sequentially queued/written.
        file.DecompressedSize = (ulong)File.FileSize;
        file.DecompressedBlockOffset = 0;
        var rounded = (rawChunkData.Length >> 5) << 5;
        file.Hash = (Hash)GetFinalHash(rawChunkData.SliceFast(rounded, rawChunkData.Length - rounded));
    }

    /// <summary>
    ///     Updates the current hash.
    /// </summary>
    /// <param name="data">The data to be hashed.</param>
    private void UpdateHash(Span<byte> data)
    {
        Debug.Assert(data.Length % 32 == 0);
        _hash.TransformByteGroupsInternal(data);
    }

    /// <summary>
    ///     Receive the final hash.
    /// </summary>
    private ulong GetFinalHash(Span<byte> remainingData) => _hash.FinalizeHashValueInternal(remainingData);
}
