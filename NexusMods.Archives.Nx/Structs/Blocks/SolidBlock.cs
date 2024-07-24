using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;
using System.Runtime.InteropServices;

namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     Represents an individual SOLID block.
/// </summary>
/// <param name="Items">Items tied to the given block.</param>
/// <param name="Compression">Compression method to use.</param>
internal record SolidBlock<T>(List<T> Items, CompressionPreference Compression) : IBlock<T>
    where T : IHasFileSize, ICanProvideFileData, IHasRelativePath
{
    /// <inheritdoc />
    public ulong LargestItemSize()
    {
        long largestSize = 0;

        // Skips IEnumerator.
        foreach (var item in CollectionsMarshal.AsSpan(Items))
        {
            if (item.FileSize > largestSize)
                largestSize = item.FileSize;
        }

        return (ulong)largestSize;
    }

    /// <inheritdoc />
    public int FileCount() => Items.Count;

    /// <inheritdoc />
    public void AppendFilesUnsafe(ref int currentIndex, HasRelativePathWrapper[] paths)
    {
        foreach (var item in Items)
            paths.DangerousGetReferenceAt(currentIndex++) = item.RelativePath;
    }

    /// <inheritdoc />
    public bool CanCreateChunks() => false;

    /// <inheritdoc />
    public unsafe void ProcessBlock(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools)
    {
        var toc = tocBuilder.Toc;
        using var compressedAlloc = pools.BlockPool.Rent(settings.BlockSize);
        using var decompressedAlloc = pools.BlockPool.Rent(settings.BlockSize);
        var decompressedBlockOffset = 0;

        var decompressedSpan = decompressedAlloc.Span;
        fixed (byte* decompressedPtr = decompressedSpan)
        {
            var deduplicationState = settings.SolidDeduplicationState;
            var itemSpan = CollectionsMarshal.AsSpan(Items);
            for (var x = 0; x < itemSpan.Length; x++)
            {
                var item = itemSpan[x];

                // Write file info
                using var data = item.FileDataProvider.GetFileData(0, (uint)item.FileSize);
                ref var file = ref tocBuilder.GetAndIncrementFileAtomic();
                file.FilePathIndex = tocBuilder.FileNameToIndexDictionary[item.RelativePath];
                file.DecompressedSize = data.DataLength;
                file.Hash = XxHash64Algorithm.HashBytes(data.Data, data.DataLength);

                // Check for deduplication
                if (deduplicationState != null)
                {
                    /*
                        Note:

                        If you are reading this code (myself included), you may
                        think that it's technically possible to miss a duplicate here.

                        It is not.

                        When grouping files in blocks for packing, the files are
                        sorted by size (ascending). Files above block size
                        (but below chunk size) are treated as chunked files
                        with a single chunk.

                        In other words, duplicates will always be placed adjacent
                        in a SOLID block, i.e. as the next item in this 'for loop'
                        on the same thread.

                        If a duplicate file happens to be at the end of one block
                        then the start of the next block, that is also not an issue.
                        Access to the state is synchronized, so regardless of which
                        block is processed first, the file will be deduplicated.
                    */

                    lock (deduplicationState)
                    {
                        if (deduplicationState.TryFindDuplicateByFullHash(file.Hash, out var existingFile))
                        {
                            file.FirstBlockIndex = existingFile.BlockIndex;
                            file.DecompressedBlockOffset = existingFile.DecompressedBlockOffset;
                            continue;
                        }

                        deduplicationState.AddFileHash(file.Hash, blockIndex, decompressedBlockOffset);
                    }
                }

                file.FirstBlockIndex = blockIndex;
                file.DecompressedBlockOffset = decompressedBlockOffset;

                // Copy to SOLID block
                Buffer.MemoryCopy(data.Data, decompressedPtr + decompressedBlockOffset,
                    (ulong)decompressedAlloc.Array.Length - (ulong)decompressedBlockOffset, data.DataLength);

                // Next file time!
                decompressedBlockOffset += (int)data.DataLength;
            }

            // This can happen if a whole block is deduplicated
            if (decompressedBlockOffset == 0)
            {
                // Note: Blocks, BlockCompressions etc. were allocated from uninitialized
                //       memory. Therefore we need to provide a value in the case that
                //       they don't default to 0.
                ref var blockSize = ref toc.Blocks.DangerousGetReferenceAt(blockIndex);
                blockSize.CompressedSize = 0;

                ref var blockCompression = ref toc.BlockCompressions.DangerousGetReferenceAt(blockIndex);
                blockCompression = CompressionPreference.Copy;

                BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);
                BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
                return;
            }

            var compressedSpan = compressedAlloc.Span;
            fixed (byte* compressedPtr = compressedSpan)
            {
                ref var blockSize = ref toc.Blocks.DangerousGetReferenceAt(blockIndex);
                blockSize.CompressedSize = Utilities.Compression.Compress(Compression, settings.SolidCompressionLevel, decompressedPtr,
                    decompressedBlockOffset, compressedPtr, compressedAlloc.Array.Length, out var asCopy);

                ref var blockCompression = ref toc.BlockCompressions.DangerousGetReferenceAt(blockIndex);
                blockCompression = asCopy ? CompressionPreference.Copy : Compression;

                BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);
                BlockHelpers.WriteToOutput(settings.Output, compressedAlloc, blockSize.CompressedSize);
                BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
            }
        }
    }
}
