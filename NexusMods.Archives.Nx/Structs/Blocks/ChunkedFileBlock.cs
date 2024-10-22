using System.IO.Hashing;
using System.Runtime.CompilerServices;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;
using static NexusMods.Archives.Nx.Structs.Blocks.ChunkedCommon;

namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     A block that represents a slice of an existing file.
/// </summary>
/// <typeparam name="T">Type of item stored inside the block.</typeparam>
/// <param name="StartOffset">Start offset of the file.</param>
/// <param name="ChunkIndex">Zero based index of this chunk.</param>
/// <param name="ChunkSize">Size of the file segment.</param>
/// <param name="State">Stores the shared state of all chunks.</param>
internal record ChunkedFileBlock<T>(ulong StartOffset, int ChunkSize, int ChunkIndex, ChunkedBlockState<T> State) : IBlock<T>
    where T : IHasFileSize, ICanProvideFileData, IHasRelativePath
{
    /// <inheritdoc />
    public ulong LargestItemSize() => (ulong)State.File.FileSize;

    /// <inheritdoc />
    public bool CanCreateChunks() => true;

    /// <inheritdoc />
    public int FileCount() => 1;

    /// <inheritdoc />
    public void AppendFilesUnsafe(ref int currentIndex, HasRelativePathWrapper[] paths)
    {
        paths.DangerousGetReferenceAt(currentIndex++) = State.File.RelativePath;
    }

    /// <inheritdoc />
    public void ProcessBlock(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools)
    {
        var isDedupeEnabled = settings.ChunkedDeduplicationState != null;
        if (!isDedupeEnabled)
        {
            ProcessBlockWithoutDeduplication(tocBuilder, settings, blockIndex, pools);
            return;
        }

        if (ChunkIndex == 0)
            ProcessFirstChunkDedupe(tocBuilder, settings, blockIndex, pools);
        else
            ProcessRemainingChunksDedupe(tocBuilder, settings, blockIndex, pools);
    }

    private unsafe void ProcessBlockWithoutDeduplication(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools)
    {
        using var data = State.File.FileDataProvider.GetFileData(StartOffset, (uint)ChunkSize);

        // If we reach here, it means we need to compress and process the block
        using var allocation = pools.ChunkPool.Rent(settings.ChunkSize);
        var allocSpan = allocation.Span;
        fixed (byte* allocationPtr = allocSpan)
        {
            // Process the shared mutable state (hash) of this chunk.
            //
            // It's desireable to process this outside the lock marked by WaitForBlockTurn
            // because the chunked file has its own shared state independent of the file
            // as a whole. We could be hashing the file while another thread is writing the
            // disk.
            var numChunks = State.NumChunks;
            var isLastChunk = IsLastChunk(numChunks, ChunkIndex);
            State.WaitForChunkIndexTurn(ChunkIndex);
            if (!isLastChunk)
            {
                State.Hash.Append(data.Data, data.DataLength);
            }
            else
            {
                ulong finalHash;
                if (State.FinalHash == 0)
                {
                    State.Hash.Append(data.Data, data.DataLength);
                    finalHash = State.Hash.GetCurrentHashAsUInt64();
                }
                else
                    finalHash = State.FinalHash;
                State.AddFileEntryToTocAtomic(tocBuilder, FirstBlockIndex(blockIndex, numChunks), finalHash);
            }
            State.EndProcessingChunk();

            // Proceed with normal block processing
            var compressed = BlockHelpers.Compress(Compression, settings.ChunkedCompressionLevel, data, allocationPtr, allocSpan.Length, out var asCopy);

            // Take lock.
            BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);
            WriteBlock(allocation, compressed, tocBuilder, settings, blockIndex, asCopy, State.Compression);
            BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
        }
    }

    private unsafe void ProcessRemainingChunksDedupe(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools)
    {
        // Cancel early if we're a duplicate and deduping is enabled.
        if (State.DuplicateState == DeduplicationCheckState.Duplicate)
        {
            ReturnWhenDuplicate(tocBuilder, settings, blockIndex);
            return;
        }

        using var data = State.File.FileDataProvider.GetFileData(StartOffset, (uint)ChunkSize);

        // If we reach here, it means we need to compress and process the block
        using var allocation = pools.ChunkPool.Rent(settings.ChunkSize);
        var allocSpan = allocation.Span;
        fixed (byte* allocationPtr = allocSpan)
        {
            // Process the shared mutable state (hash) of this chunk.
            // Note: The first chunk can also be the last, because files can have 1 chunk.
            var numChunks = State.NumChunks;
            var isLastChunk = IsLastChunk(numChunks, ChunkIndex);
            if (State.WaitForChunkIndexTurnCancelOnDuplicate(ChunkIndex))
            {
                // Return early on found duplicate.
                ReturnWhenDuplicate(tocBuilder, settings, blockIndex);
                return;
            }

            if (State.FinalHash == 0)
            {
                State.Hash.Append(data.Data, data.DataLength);
                if (isLastChunk)
                {
                    State.FinalHash = State.Hash.GetCurrentHashAsUInt64();
                }
            }

            State.EndProcessingChunk();

            // Proceed with normal block processing
            var compressed = BlockHelpers.CompressStreamed(Compression, settings.ChunkedCompressionLevel, data, allocationPtr, allocSpan.Length, () =>
            {
                // Try to detect early if we can bail out
                if (State.DuplicateState == DeduplicationCheckState.Duplicate)
                {
                    ReturnWhenDuplicate(tocBuilder, settings, blockIndex);
                    return CompressNonFirstBlockIsDuplicated;
                }

                return 0;
            }, out var asCopy);

            if (compressed is CompressNonFirstBlockIsDuplicated)
                return;

            // Take lock.
            BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);

            // Check one last time. In case last block hasn't previously
            // finished processing.
            if (State.DuplicateState == DeduplicationCheckState.Duplicate)
            {
                ReturnWhenDuplicate(tocBuilder, settings, blockIndex, false);
                return;
            }

            WriteBlock(allocation, compressed, tocBuilder, settings, blockIndex, asCopy, State.Compression);

            // If last block, we need to write out to ToC.
            if (isLastChunk)
            {
                ref var fileEntry = ref State.AddFileEntryToTocAtomic(tocBuilder, FirstBlockIndex(blockIndex, numChunks), State.FinalHash);
                AddToDeduplicator(ref fileEntry, settings.ChunkedDeduplicationState!, State.ShortHash);
            }

            BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
        }
    }

    private unsafe void ProcessFirstChunkDedupe(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools)
    {
        using var data = State.File.FileDataProvider.GetFileData(StartOffset, (uint)ChunkSize);
        var dataPtr = data.Data;
        var dataLen = data.DataLength;
        var duplState = settings.ChunkedDeduplicationState;

        // There's a possibility that last block has finished processing, so
        // we can set the state to NotDuplicate early.
        // If this is not the case however, we need to re-check later.
        var previousBlocksProcessed = tocBuilder.CurrentBlock == blockIndex;
        State.ShortHash = CalculateShortHash(dataLen, dataPtr);
        if (IsDuplicate(duplState!, State.ShortHash, ref State.FinalHash, out var deduplicatedFile))
        {
            ProcessDeduplicate(tocBuilder, settings, blockIndex, deduplicatedFile, State.FinalHash);
            return;
        }

        if (previousBlocksProcessed)
            State.DuplicateState = DeduplicationCheckState.NotDuplicate;

        // If we reach here, it means we need to compress and process the block
        using var allocation = pools.ChunkPool.Rent(settings.ChunkSize);
        var allocSpan = allocation.Span;
        fixed (byte* allocationPtr = allocSpan)
        {
            // Process the shared mutable state (hash) of this chunk.
            var numChunks = State.NumChunks;
            var isLastChunk = IsLastChunk(numChunks, ChunkIndex);
            State.WaitForChunkIndexTurn(ChunkIndex);
            State.Hash.Append(dataPtr, dataLen);
            if (isLastChunk)
                State.FinalHash = State.Hash.GetCurrentHashAsUInt64();
            State.EndProcessingChunk();

            // Proceed with normal block processing
            var compressed = BlockHelpers.CompressStreamed(Compression, settings.ChunkedCompressionLevel, data, allocationPtr, allocSpan.Length, () =>
            {
                if (previousBlocksProcessed)
                    return 0;

                // Check if we're a duplicate on every zstd stream increment.
                // Usually this kicks in every 128K compressed, though may vary
                // with zstd version as zstd is dictating this.
                previousBlocksProcessed = tocBuilder.CurrentBlock == blockIndex;
                // ReSharper disable once AccessToModifiedClosure
                if (IsDuplicate(duplState!, State.ShortHash, ref State.FinalHash, out deduplicatedFile))
                {
                    ProcessDeduplicate(tocBuilder, settings, blockIndex, deduplicatedFile, State.FinalHash);
                    return CompressFirstBlockIsDuplicateError;
                }

                if (previousBlocksProcessed)
                    State.DuplicateState = DeduplicationCheckState.NotDuplicate;

                return 0;
            }, out var asCopy);

            if (compressed is CompressFirstBlockIsDuplicateError)
                return;

            // Take lock.
            BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);

            // In the rare event, if the previous block gets locked up processing for a long time
            // we have to check for possible duplicates one final time. Now that it's guaranteed
            // that 'previousBlocksProcessed == true' by definition.
            if (IsDuplicate(duplState!, State.ShortHash, ref State.FinalHash, out deduplicatedFile))
            {
                ProcessDeduplicate(tocBuilder, settings, blockIndex, deduplicatedFile, State.FinalHash, false);
                return;
            }

            State.DuplicateState = DeduplicationCheckState.NotDuplicate;
            WriteBlock(allocation, compressed, tocBuilder, settings, blockIndex, asCopy, State.Compression);

            // If last block, we need to write out to ToC.
            if (isLastChunk)
            {
                ref var fileEntry = ref State.AddFileEntryToTocAtomic(tocBuilder, FirstBlockIndex(blockIndex, numChunks), State.FinalHash);
                AddToDeduplicator(ref fileEntry, duplState!, State.ShortHash);
            }

            BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsDuplicate(ChunkedDeduplicationState duplState, ulong shortHash,
        ref ulong fullHash, out DeduplicatedChunkedFile deduplicatedChunkedFile)
    {
        deduplicatedChunkedFile = default;
        if (!duplState.HasPotentialDuplicate(shortHash))
            return false;

        if (!IsHashValid(fullHash))
            fullHash = CalculateFullFileHash();

        return duplState.TryFindDuplicateByFullHash(fullHash, out deduplicatedChunkedFile);
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

        State.AddFileEntryToTocAtomic(tocBuilder, deduplicatedChunkedFile.BlockIndex, fullHash);
        State.DuplicateState = DeduplicationCheckState.Duplicate;

        // Note: Blocks, BlockCompressions etc. were allocated from uninitialized
        //       memory. Therefore we need to provide a value in the case that
        //       they don't default to 0.
        var chunkCount = State.NumChunks;
        for (var x = 0; x < chunkCount; x++)
            WriteBlockDetailsToToc(0, tocBuilder, blockIndex + x, CompressionPreference.Copy);

        BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
    }

    private static unsafe ulong CalculateShortHash(ulong dataLen, byte* dataPtr)
    {
        var shortLen = Math.Min(ShortHashLength, dataLen);
        return HashUtilities.HashToUInt64(dataPtr, shortLen);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe ulong CalculateFullFileHash()
    {
        using var data = State.File.FileDataProvider.GetFileData(StartOffset, (ulong)State.File.FileSize);
        var fullHash = HashUtilities.HashToUInt64(data.Data, data.DataLength);
        return fullHash;
    }

    private static void ReturnWhenDuplicate(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, bool waitForBlockTurn = true)
    {
        if (waitForBlockTurn)
            BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);
        BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
    }

    /// <inheritdoc />
    public CompressionPreference Compression => State.Compression;
}

/// <summary>
///     This item stores the shared state of all <see cref="ChunkedFileBlock{T}"/> chunks.
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
    /// <remarks>
    ///     If this is -1, skip processing all blocks.
    /// </remarks>
    public int NumChunks;

    /// <summary>
    ///     When deduplication is on, this flag confirms that the file is not a duplicate.
    /// </summary>
    public DeduplicationCheckState DuplicateState = DeduplicationCheckState.Unknown;

    // If we make more data, shrink DeduplicationCheckState to byte.

    /// <summary>
    ///     File associated with this chunked block.
    /// </summary>
    public T File { get; init; } = default!;

    /// <summary>
    ///     Instance of the Nexus xxHash64 hasher.
    /// </summary>
    internal XxHash3 Hash = new(0);

    /// <summary>
    ///     The final hash of the file.
    /// </summary>
    /// <remarks>
    ///     This is only used when deduplication is enabled.
    ///     This is because either the first or last chunk can generate the final hash.
    /// </remarks>
    internal ulong FinalHash;

    /// <summary>
    ///     The 'short' hash of the file (used for deduplication).
    /// </summary>
    /// <remarks>
    ///     This should technically be volatile, but synchronization is guaranteed
    ///     by the fact we're waiting for <see cref="_currentChunkIndex"/>. So variable
    ///     is likely to carry over.
    /// </remarks>
    internal ulong ShortHash;

    /// <summary>
    ///     Current index of the chunk which is holding the lock.
    /// </summary>
    /// <remarks>
    ///     If this is -1, skip processing all blocks.
    /// </remarks>
    private int _currentChunkIndex;

    /// <summary>
    ///     Adds an entry to the table of contents for the current file.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref FileEntry AddFileEntryToTocAtomic(TableOfContentsBuilder<T> tocBuilder, int firstBlockIndex, ulong hash)
        => ref ChunkedCommon.AddFileEntryToTocAtomic(tocBuilder, firstBlockIndex, hash, File.RelativePath, (ulong)File.FileSize);

    /// <summary>
    ///     Locks processing of inner mutable shared data (e.g. hash) until
    ///     it is time for chunk with <paramref name="chunkIndex"/> to be processed.
    ///
    ///     Call <see cref="EndProcessingChunk"/> when done.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WaitForChunkIndexTurn(int chunkIndex)
    {
        // Wait until it's our turn to write.
        var spinWait = new SpinWait();
        while (_currentChunkIndex != chunkIndex)
        {
#if NETCOREAPP3_0_OR_GREATER
            spinWait.SpinOnce(-1);
#else
            spinWait.SpinOnce();
#endif
        }
    }

    /// <summary>
    ///     Locks processing of inner mutable shared data (e.g. hash) until
    ///     it is time for chunk with <paramref name="chunkIndex"/> to be processed.
    ///
    ///     Call <see cref="EndProcessingChunk"/> when done.
    /// </summary>
    /// <returns>
    ///     True if the operation was canceled due to a duplicate.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool WaitForChunkIndexTurnCancelOnDuplicate(int chunkIndex)
    {
        // Wait until it's our turn to write.
        var spinWait = new SpinWait();
        while (_currentChunkIndex != chunkIndex && DuplicateState != DeduplicationCheckState.Duplicate)
        {
#if NETCOREAPP3_0_OR_GREATER
            spinWait.SpinOnce(-1);
#else
            spinWait.SpinOnce();
#endif
        }

        return DuplicateState == DeduplicationCheckState.Duplicate;
    }

    /// <summary>
    ///     Warning: Calling this from multiple threads in parallel is not legal.
    ///     It will cause a deadlock in <see cref="WaitForChunkIndexTurn"/>
    ///     as individual threads' chunkindex can be skipped.
    /// </summary>
    internal void EndProcessingChunk() => Interlocked.Increment(ref _currentChunkIndex);
}


