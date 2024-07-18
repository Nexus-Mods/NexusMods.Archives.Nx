using System.Runtime.CompilerServices;
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
/// <param name="Compression">The compression method used by the existing chunks.</param>
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
       var isDedupeEnabled = settings.ChunkedDeduplicationState != null;

       if (!isDedupeEnabled)
       {
           ProcessBlockWithoutDeduplication(tocBuilder, settings, blockIndex);
           return;
       }

       if (ChunkIndex == 0)
           ProcessFirstChunkDedupe(tocBuilder, settings, blockIndex);
       else
           ProcessRemainingChunksDedupe(tocBuilder, settings, blockIndex);
    }

    private unsafe void ProcessRemainingChunksDedupe(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex)
    {
        // Cancel early if we're a duplicate and deduping is enabled.
        if (State.DuplicateState == DeduplicationCheckState.Duplicate)
        {
            ReturnWhenDuplicate(tocBuilder, settings, blockIndex);
            return;
        }

        var dupeState = settings.ChunkedDeduplicationState;

        using var data = State.NxSource.GetFileData(StartOffset, (uint)ChunkSize);
        var dataSpan = new Span<byte>(data.Data, (int)data.DataLength);

        // Take lock.
        BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);

        // Check one last time. In case last block hasn't previously
        // finished processing.
        if (State.DuplicateState == DeduplicationCheckState.Duplicate)
        {
            ReturnWhenDuplicate(tocBuilder, settings, blockIndex, false);
            return;
        }

        WriteBlock(dataSpan, tocBuilder, settings, blockIndex, false, Compression);

        if (IsLastChunk(State.NumChunks, ChunkIndex))
        {
            ref var fileEntry = ref AddFileEntryToTocAtomic(tocBuilder, FirstBlockIndex(blockIndex, State.NumChunks), State.FileHash, State.RelativePath, State.FileLength);
            AddToDeduplicator(ref fileEntry, dupeState!, CalcShortHash(data.Data, data.DataLength));
        }

        BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
    }

    private unsafe void ProcessFirstChunkDedupe(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex)
    {
        var fileHash = State.FileHash;
        var dupeState = settings.ChunkedDeduplicationState;

        // Check for existing duplicate, in the case we are dealing with repacking
        // a deduplicate file.
        var previousBlocksProcessed = tocBuilder.CurrentBlock == blockIndex;
        if (dupeState!.TryFindDuplicateByFullHash(fileHash, out var existingChunkedFile))
        {
            ProcessDeduplicate(tocBuilder, settings, blockIndex, existingChunkedFile, fileHash);
            return;
        }

        if (previousBlocksProcessed)
            State.DuplicateState = DeduplicationCheckState.NotDuplicate;

        using var data = State.NxSource.GetFileData(StartOffset, (uint)ChunkSize);
        var dataSpan = new Span<byte>(data.Data, (int)data.DataLength);

        // Take lock.
        BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);

        // Re-check duplicate in case one of the blocks directly before was a duplicate.
        if (dupeState.TryFindDuplicateByFullHash(fileHash, out existingChunkedFile))
        {
            ProcessDeduplicate(tocBuilder, settings, blockIndex, existingChunkedFile, fileHash, false);
            return;
        }
        State.DuplicateState = DeduplicationCheckState.NotDuplicate;

        WriteBlock(dataSpan, tocBuilder, settings, blockIndex, false, Compression);

        if (IsLastChunk(State.NumChunks, ChunkIndex))
        {
            ref var fileEntry = ref AddFileEntryToTocAtomic(tocBuilder, FirstBlockIndex(blockIndex, State.NumChunks), State.FileHash, State.RelativePath, State.FileLength);
            AddToDeduplicator(ref fileEntry, dupeState, CalcShortHash(data.Data, data.DataLength));
        }

        BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
    }

    /// <summary>
    ///     Calculates the short hash of an existing pre-compressed file.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    private unsafe ulong CalcShortHash(byte* compressedPtr, ulong compressedLength)
    {
        var length = Math.Min(ShortHashLength, State.FileLength);
        var bytes = stackalloc byte[ShortHashLength];
        Utilities.Compression.Decompress(Compression, compressedPtr, (int)compressedLength, bytes, (int)length);
        return XxHash64Algorithm.HashBytes(bytes, length);
    }

    private unsafe void ProcessBlockWithoutDeduplication(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex)
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

    /// <summary/>
    /// <remarks>
    ///     This sets `State.ShouldSkipProcessing()` == true.
    ///     Meaning all other chunks will skip processing.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessDeduplicate(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, DeduplicatedChunkedFile deduplicatedChunkedFile, ulong fullHash, bool waitForBlock = true)
    {
        if (waitForBlock)
            BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);

        AddFileEntryToTocAtomic(tocBuilder, deduplicatedChunkedFile.BlockIndex, fullHash, State.RelativePath, State.FileLength);
        State.DuplicateState = DeduplicationCheckState.Duplicate;

        // Note: Blocks, BlockCompressions etc. were allocated from uninitialized
        //       memory. Therefore we need to provide a value in the case that
        //       they don't default to 0.
        var chunkCount = State.NumChunks;
        for (var x = 0; x < chunkCount; x++)
            WriteBlockDetailsToToc(0, tocBuilder, blockIndex + x, CompressionPreference.Copy);

        BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
    }

    private static void ReturnWhenDuplicate(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, bool waitForBlockTurn = true)
    {
        if (waitForBlockTurn)
            BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);
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

    /// <summary>
    ///     When deduplication is on, this flag confirms that the file is not a duplicate.
    /// </summary>
    public DeduplicationCheckState DuplicateState = DeduplicationCheckState.Unknown;
}
