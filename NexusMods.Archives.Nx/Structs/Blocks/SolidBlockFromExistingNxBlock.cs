using System.Diagnostics.CodeAnalysis;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing.Unpack;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;
#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     Represents a SOLID block that's backed by an existing SOLID block inside
///     another Nx archive.
/// </summary>
/// <param name="Items">Items tied to the given block.</param>
/// <param name="NxSource">Compression method to use.</param>
/// <param name="StartOffset">Starting offset of the compressed block.</param>
/// <param name="CompressedBlockSize">Size of the compressed block at <paramref name="StartOffset"/>.</param>
/// <param name="Compression">Compression method used by the data.</param>
internal record SolidBlockFromExistingNxBlock<T>(PathedFileEntry[] Items, IFileDataProvider NxSource, ulong StartOffset, int CompressedBlockSize, CompressionPreference Compression) : IBlock<T>
    where T : IHasFileSize, ICanProvideFileData, IHasRelativePath
{
    /// <inheritdoc />
    public ulong LargestItemSize()
    {
        ulong largestSize = 0;

        // Skips IEnumerator.
        foreach (var item in Items)
        {
            if (item.Entry.DecompressedSize > largestSize)
                largestSize = item.Entry.DecompressedSize;
        }

        return largestSize;
    }

    /// <inheritdoc />
    public int FileCount() => Items.Length;

    /// <inheritdoc />
    public void AppendFilesUnsafe(ref int currentIndex, HasRelativePathWrapper[] paths)
    {
        foreach (var item in Items)
            paths.DangerousGetReferenceAt(currentIndex++) = item.FilePath;
    }

    /// <inheritdoc />
    public bool CanCreateChunks() => false;

    /// <inheritdoc />
    public void ProcessBlock(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools)
    {
        var deduplicationState = settings.SolidDeduplicationState;
        if (deduplicationState != null)
            ProcessBlockWithDeduplication(tocBuilder, settings, blockIndex, pools);
        else
            ProcessBlockWithoutDeduplication(tocBuilder, settings, blockIndex);
    }

    private unsafe void ProcessBlockWithoutDeduplication(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex)
    {
        var toc = tocBuilder.Toc;
        foreach (var item in Items)
        {
            // Write file info
            ref var file = ref tocBuilder.GetAndIncrementFileAtomic();
            file.FilePathIndex = tocBuilder.FileNameToIndexDictionary[item.FilePath];
            file.FirstBlockIndex = blockIndex;
            file.DecompressedSize = item.Entry.DecompressedSize;
            file.DecompressedBlockOffset = item.Entry.DecompressedBlockOffset;
            file.Hash = item.Entry.Hash;
        }

        using var rawCompressedData = NxSource.GetFileData(StartOffset, (uint)CompressedBlockSize);
        WriteBlockWithData(tocBuilder, settings, blockIndex, toc, new Span<byte>(rawCompressedData.Data, CompressedBlockSize));
    }

    private void WriteBlockWithData(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings,
        int blockIndex, TableOfContents toc, Span<byte> compressedBlockData)
    {
        ref var blockSize = ref toc.Blocks.DangerousGetReferenceAt(blockIndex);
        blockSize.CompressedSize = CompressedBlockSize;

        ref var blockCompression = ref toc.BlockCompressions.DangerousGetReferenceAt(blockIndex);
        blockCompression = Compression;

        BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);
        BlockHelpers.WriteToOutput(settings.Output, compressedBlockData);
        BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
    }

    private unsafe void ProcessBlockWithDeduplication(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools)
    {
        var deduplicationState = settings.SolidDeduplicationState!;
        var toc = tocBuilder.Toc;

        using var rawCompressedData = NxSource.GetFileData(StartOffset, (uint)CompressedBlockSize);

        // Indices of items to not.
        var recompressIndices = new List<int>(Items.Length);
        var numBytesToDecompress = 0;
        var numBytesToCompress = 0;

        for (var x = 0; x < Items.Length; x++)
        {
            var item = Items[x];
            lock (deduplicationState)
            {
                ref var file = ref tocBuilder.GetAndIncrementFileAtomic();
                file.FilePathIndex = tocBuilder.FileNameToIndexDictionary[item.FilePath];
                file.DecompressedSize = item.Entry.DecompressedSize;
                file.Hash = item.Entry.Hash;

                // Update the TOC with either deduplicated or (original/recompressed).
                if (deduplicationState.TryFindDuplicateByFullHash(item.Entry.Hash, out var existingFile))
                {
                    file.FirstBlockIndex = existingFile.BlockIndex;
                    file.DecompressedBlockOffset = existingFile.DecompressedBlockOffset;
                }
                else
                {
                    recompressIndices.Add(x);
                    var itemEndOffset = item.Entry.DecompressedBlockOffset + (int)item.Entry.DecompressedSize;
                    numBytesToDecompress = Math.Max(numBytesToDecompress, itemEndOffset);

                    // Add a new file entry in the ToC pointing to where we will.
                    file.FirstBlockIndex = blockIndex;
                    file.DecompressedBlockOffset = numBytesToCompress;

                    // Add to deduplicator input
                    deduplicationState.AddFileHash(file.Hash, blockIndex, numBytesToCompress);

                    // This cast is fine because block size is limited by ToC.
                    numBytesToCompress += (int)item.Entry.DecompressedSize;
                }
            }
        }

        // All blocks were deduplicated successfully.
        // so we write an empty block and carry on.
        if (recompressIndices.Count <= 0)
        {
            WriteEmptyBlock(tocBuilder, settings, blockIndex, toc);
            return;
        }

        // No blocks were deduplicated, so we copy the data verbatim.
        if (recompressIndices.Count == Items.Length)
        {
            WriteBlockWithData(tocBuilder, settings, blockIndex, toc, new Span<byte>(rawCompressedData.Data, CompressedBlockSize));
            return;
        }

        // If we're here, we have partial deduplication to do.
        // We must decompress the content, copy the relevant files' data and recompress.
        // We have duplicates, we must compress them, and append them to output.
        using var toRecompressAlloc = RentBlock(pools, settings, numBytesToCompress);
        using var decompressedAlloc = RentBlock(pools, settings, numBytesToDecompress);
        var decompressedSpan = decompressedAlloc.AsSpan();
        var toRecompressSpan = toRecompressAlloc.AsSpan();
        fixed (byte* toRecompressPtr = toRecompressSpan)
        fixed (byte* decompressedPtr = decompressedSpan)
        {
            Utilities.Compression.Decompress(Compression, rawCompressedData.Data, (int)rawCompressedData.DataLength, decompressedPtr, numBytesToDecompress);

            // Write the new data to the recompressed buffer.
            var destinationOfs = toRecompressPtr;
            var destinationBytesLeft = numBytesToCompress;
#if NET5_0_OR_GREATER
            var indexSpan = CollectionsMarshal.AsSpan(recompressIndices);
            for (var x = 0; x < indexSpan.Length; x++)
            {
                var idx = indexSpan[x];
#else
            for (var x = 0; x < recompressIndices.Count; x++)
            {
                var idx = recompressIndices[x];
#endif

                var item = Items.DangerousGetReferenceAt(idx);
                var itemOfs = item.Entry.DecompressedBlockOffset;
                var numBytesToCopy = (int)item.Entry.DecompressedSize;
                Buffer.MemoryCopy(decompressedPtr + itemOfs, destinationOfs, destinationBytesLeft, numBytesToCopy);
                destinationOfs += numBytesToCopy;
                destinationBytesLeft -= numBytesToCopy;
            }

            // Compress the new buffer (to no longer needed decompressed data buffer),
            // and write it to the output.
            ref var blockSize = ref toc.Blocks.DangerousGetReferenceAt(blockIndex);
            blockSize.CompressedSize = Utilities.Compression.Compress(Compression, settings.SolidCompressionLevel, toRecompressPtr,
                numBytesToCompress, decompressedPtr, decompressedSpan.Length, out var asCopy);

            ref var blockCompression = ref toc.BlockCompressions.DangerousGetReferenceAt(blockIndex);
            blockCompression = asCopy ? CompressionPreference.Copy : Compression;

            BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);
            BlockHelpers.WriteToOutput(settings.Output, new Span<byte>(decompressedPtr, blockSize.CompressedSize));
            BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
        }
    }

    private static void WriteEmptyBlock(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex,
        TableOfContents toc)
    {
        // Note: Blocks, BlockCompressions etc. were allocated from uninitialized
        //       memory. Therefore, we need to provide a value in the case that
        //       they don't default to 0.
        ref var blockSize = ref toc.Blocks.DangerousGetReferenceAt(blockIndex);
        blockSize.CompressedSize = 0;

        ref var blockCompression = ref toc.BlockCompressions.DangerousGetReferenceAt(blockIndex);
        blockCompression = CompressionPreference.Copy;

        BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);
        BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
    }

    /// <summary>
    ///     Rents a block from the pool, allocating the block if it happens to be
    ///     that the block we're repacking is larger than the one in the existing file.
    /// </summary>
    private MaybePackerArrayPoolRental RentBlock(PackerArrayPools pools, PackerSettings settings, int size)
    {
        // Detect whether the block size of the copied file is larger
        // than the block size we're packing with.
        if (size <= settings.BlockSize)
            return new MaybePackerArrayPoolRental { Rental = pools.BlockPool.Rent(size) };

        var allocation = Polyfills.AllocateUninitializedArray<byte>(size);
        return new MaybePackerArrayPoolRental { Allocation = allocation };
    }

    /// <summary>
    ///     Represents a memory allocation that can be backed either by <see cref="PackerPoolRental"/>
    ///     or by a plain array.
    /// </summary>
    /// <remarks>
    ///     When deduplicating SOLID blocks, it's possible that the two files were packed with different
    ///     SOLID block sizes. In which case the <see cref="PackerArrayPools"/> may
    ///     not have been initialized with sufficient size. If this is the case, we will instead
    ///     allocate a new raw array.
    /// </remarks>
    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    private struct MaybePackerArrayPoolRental : IDisposable
    {
        public PackerPoolRental Rental;
        public byte[] Allocation;

        public Span<byte> AsSpan()
        {
            return Rental.Array != null ? Rental.Span : Allocation.AsSpan();
        }

        public void Dispose()
        {
            if (Rental.Array != null)
                Rental.Dispose();
        }
    }
}
