using System.Runtime.CompilerServices;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;
using static NexusMods.Archives.Nx.Structs.Blocks.ChunkedFileBlockConstants;

namespace NexusMods.Archives.Nx.Structs.Blocks;

internal class ChunkedFileBlockConstants
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsLastChunk(int numChunks, int chunkIndex) => chunkIndex == numChunks - 1;
}

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
    public unsafe void ProcessBlock(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools)
    {
        // By definition, this is only true after the entire file is processed via
        // deduplication, therefore simply finishing processing to increment index is valid.
        var duplState = settings.ChunkedDeduplicationState;
        if (CanDeduplicateOnNonFirstChunk(duplState))
        {
            State.WaitForDeduplicationResult();

            if (State.DuplicateState == DeduplicationCheckState.Duplicate)
            {
                BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);
                BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
                return;
            }
        }

        using var data = State.File.FileDataProvider.GetFileData(StartOffset, (uint)ChunkSize);
        var dataPtr = data.Data;
        var dataLen = data.DataLength;

        // Check if there's already a duplicate file.
        ulong hash4096 = 0;
        if (CanDeduplicateOnFirstChunk(duplState))
        {
            hash4096 = CalculateHash4096(dataLen, dataPtr);
            if (IsDuplicate(duplState, dataLen, dataPtr, hash4096, out var deduplicatedFile, out var fullHash))
            {
                ProcessDeduplicate(tocBuilder, settings, blockIndex, deduplicatedFile, fullHash);
                return;
            }

            // Note: Setting to NotDuplicate here is invalid, because it may be a duplicate
            // later in the second check inside the WaitForBlockTurn block.
        }

        // If we reach here, it means we need to compress and process the block
        using var allocation = pools.ChunkPool.Rent(settings.ChunkSize);
        var allocSpan = allocation.Span;
        fixed (byte* allocationPtr = allocSpan)
        {
            // Proceed with normal block processing
            var compressed = BlockHelpers.Compress(Compression, settings.ChunkedCompressionLevel, data, allocationPtr, allocSpan.Length,
                out var asCopy);

            // Take lock.
            BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);

            // Note: It's possible a previous thread hasn't called AddToDeduplicator
            // by the time this thread performed the same check above.
            // Reaching this does indicate some performance loss, since
            // we called Compress already, however in practice it should only
            // happen when the very previous file is a duplicate.
            if (CanDeduplicateOnFirstChunk(duplState))
            {
                if (IsDuplicate(duplState, dataLen, dataPtr, hash4096, out var deduplicatedFile, out var fullHash))
                {
                    ProcessDeduplicate(tocBuilder, settings, blockIndex, deduplicatedFile, fullHash);
                    return;
                }

                State.DuplicateState = DeduplicationCheckState.NotDuplicate;
            }

            ref var fileEntry = ref State.UpdateState(ChunkIndex, allocation, compressed, tocBuilder, settings, blockIndex,
                new Span<byte>(data.Data, (int)data.DataLength),
                asCopy);
            AddToDeduplicator(ref fileEntry, duplState);
            BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe bool IsDuplicate(ChunkedDeduplicationState? duplState, ulong dataLen, byte* dataPtr, ulong hash4096, out DeduplicatedChunkedFile deduplicatedChunkedFile, out ulong fullHash)
    {
        deduplicatedChunkedFile = default;
        fullHash = default;
        lock (duplState!)
        {
            if (!duplState.HasPotentialDuplicate(hash4096))
                return false;

            fullHash = CalculateFullFileHash(dataPtr, dataLen);
            return duplState.TryFindDuplicateByFullHash(fullHash, out deduplicatedChunkedFile);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanDeduplicateOnFirstChunk(ChunkedDeduplicationState? duplState) => duplState != null && ChunkIndex == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanDeduplicateOnNonFirstChunk(ChunkedDeduplicationState? duplState) => duplState != null && ChunkIndex != 0;

    /// <summary/>
    /// <remarks>
    ///     This sets `State.ShouldSkipProcessing()` == true.
    ///     Meaning all other chunks will skip processing.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessDeduplicate(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, DeduplicatedChunkedFile deduplicatedChunkedFile,
        ulong fullHash)
    {
        BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);
        State.AddFileEntryToToc(tocBuilder, deduplicatedChunkedFile.BlockIndex, fullHash);
        State.DuplicateState = DeduplicationCheckState.Duplicate;
        BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
    }

    private static unsafe ulong CalculateHash4096(ulong dataLen, byte* dataPtr)
    {
        var shortLen = Math.Min(4096, dataLen);
        var hash4096 = XxHash64Algorithm.HashBytes(dataPtr, shortLen);
        return hash4096;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe ulong CalculateFullFileHash(byte* dataPtr, ulong dataLen)
    {
        ulong fullHash;
        var numChunks = State.NumChunks;
        var chunkIndex = ChunkIndex;
        if (IsLastChunk(numChunks, chunkIndex))
            fullHash = XxHash64Algorithm.HashBytes(dataPtr, dataLen);
        else
        {
            var hashAlgo = new XxHash64Algorithm(0);
            hashAlgo.AppendHash(dataPtr, dataLen);
            chunkIndex++;
            var currentStartOffset = (ulong)ChunkSize;
            while (!IsLastChunk(numChunks, chunkIndex))
            {
                using var curChunkData = State.File.FileDataProvider.GetFileData(currentStartOffset, (uint)ChunkSize);
                hashAlgo.AppendHash(curChunkData.Data, curChunkData.DataLength);
                currentStartOffset += (ulong)ChunkSize;
                chunkIndex++;
            }

            var remainingFileLength = (ulong)State.File.FileSize - currentStartOffset;
            using var lastChunkData = State.File.FileDataProvider.GetFileData(currentStartOffset, remainingFileLength);
            fullHash = hashAlgo.GetFinalHash(lastChunkData.Data, lastChunkData.DataLength);
        }

        return fullHash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void AddToDeduplicator(ref FileEntry fileEntry, ChunkedDeduplicationState? duplState)
    {
        if (Unsafe.IsNullRef(ref fileEntry))
            return;

        if (duplState == null)
            return;

        // File is at least ChunkSize long, by definition.
        var shortLen = Math.Min(4096, (ulong)ChunkSize);
        using var first4096 = State.File.FileDataProvider.GetFileData(0, (uint)shortLen);
        var hash4096 = XxHash64Algorithm.HashBytes(first4096.Data, shortLen);
        lock (duplState)
        {
            duplState.AddFileHash(hash4096, fileEntry.Hash, fileEntry.FirstBlockIndex);
        }
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
    private XxHash64Algorithm _hash = new(0);

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref FileEntry UpdateState(int chunkIndex, PackerPoolRental compData, int compressedSize, TableOfContentsBuilder<T> tocBuilder,
        PackerSettings settings, int blockIndex, Span<byte> rawChunkData, bool asCopy)
    {
        // Write out actual block.
        BlockHelpers.WriteToOutput(settings.Output, compData, compressedSize);

        // Update Block Details
        var toc = tocBuilder.Toc;
        ref var blockSize = ref toc.Blocks.DangerousGetReferenceAt(blockIndex);
        blockSize.CompressedSize = compressedSize;

        ref var blockCompression = ref toc.BlockCompressions.DangerousGetReferenceAt(blockIndex);
        blockCompression = asCopy ? CompressionPreference.Copy : Compression;

        // Update file details once all chunks are done.
        if (!IsLastChunk(NumChunks, chunkIndex))
        {
            _hash.AppendHash(rawChunkData);
            return ref Unsafe.NullRef<FileEntry>();
        }

        // Only executed on final thread, so we can end and increment early.
        return ref AddFileEntryToToc(tocBuilder, blockIndex - chunkIndex, _hash.GetFinalHash(rawChunkData));
    }

    /// <summary>
    ///     Adds an entry to the table of contents for the current file.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref FileEntry AddFileEntryToToc(TableOfContentsBuilder<T> tocBuilder, int firstBlockIndex, ulong hash)
    {
        ref var file = ref tocBuilder.GetAndIncrementFileAtomic();
        file.FilePathIndex = tocBuilder.FileNameToIndexDictionary[File.RelativePath];
        file.FirstBlockIndex = firstBlockIndex;
        file.DecompressedSize = (ulong)File.FileSize;
        file.DecompressedBlockOffset = 0;
        file.Hash = hash;
        return ref file;
    }

    /// <summary>
    ///     Waits until we know if a non-first chunk can be deduplicated.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WaitForDeduplicationResult()
    {
        // Wait until it's our turn to write.
        var spinWait = new SpinWait();
        while (DuplicateState == DeduplicationCheckState.Unknown)
        {
#if NETCOREAPP3_0_OR_GREATER
            spinWait.SpinOnce(-1);
#else
            spinWait.SpinOnce();
#endif
        }
    }
}

// ReSharper disable once EnumUnderlyingTypeIsInt
internal enum DeduplicationCheckState : int
{
    Unknown,
    Duplicate,
    NotDuplicate
}
