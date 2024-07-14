using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;
#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

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
#if NET5_0_OR_GREATER
        foreach (var item in CollectionsMarshal.AsSpan(Items))
#else
        foreach (var item in Items)
#endif
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
            var deduplicationState = settings.DeduplicationState;
#if NET5_0_OR_GREATER
            var itemSpan = CollectionsMarshal.AsSpan(Items);
            for (var x = 0; x < itemSpan.Length; x++)
            {
                var item = itemSpan[x];
#else
            for (var x = 0; x < Items.Count; x++)
            {
                var item = Items[x];
#endif

                // Write file info
                using var data = item.FileDataProvider.GetFileData(0, (uint)item.FileSize);
                ref var file = ref tocBuilder.GetAndIncrementFileAtomic();
                file.FilePathIndex = tocBuilder.FileNameToIndexDictionary[item.RelativePath];
                file.DecompressedSize = data.DataLength;
                file.Hash = XxHash64Algorithm.HashBytes(data.Data, data.DataLength);

                // Check for deduplication
                if (deduplicationState != null)
                {
                    var length = (ulong)Math.Min(4096, (int)data.DataLength);
                    var hash4096 = XxHash64Algorithm.HashBytes(data.Data, length);
                    if (deduplicationState.TryFindDuplicateByFullHash(file.Hash, out var existingFile))
                    {
                        // If a duplicate is found, update the file entry to point to the existing file
                        file.FirstBlockIndex = existingFile.BlockIndex;
                        file.DecompressedBlockOffset = 0; // The offset in the original file
                        continue; // Skip copying data for this file
                    }

                    // If not a duplicate, add to deduplication state
                    lock (deduplicationState)
                        deduplicationState.AddFileHash(hash4096, file.Hash, blockIndex);
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
