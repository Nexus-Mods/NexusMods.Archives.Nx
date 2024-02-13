using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;
using NexusMods.Hashing.xxHash64;
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
                file.FirstBlockIndex = blockIndex;
                file.DecompressedSize = data.DataLength;
                file.DecompressedBlockOffset = decompressedBlockOffset;
                file.Hash = new Span<byte>(data.Data, (int)data.DataLength).XxHash64().Value;

                // Copy to SOLID block
                Buffer.MemoryCopy(data.Data, decompressedPtr + decompressedBlockOffset,
                    (ulong)decompressedAlloc.Array.Length - (ulong)decompressedBlockOffset, data.DataLength);

                // Next file time!
                decompressedBlockOffset += (int)data.DataLength;
            }

            var compressedSpan = compressedAlloc.Span;
            fixed (byte* compressedPtr = compressedSpan)
            {
                ref var blockSize = ref toc.Blocks.DangerousGetReferenceAt(blockIndex);
                blockSize.CompressedSize = Utilities.Compression.Compress(Compression, settings.SolidCompressionLevel, decompressedPtr,
                    decompressedBlockOffset, compressedPtr, compressedAlloc.Array.Length, out var asCopy);

                ref var blockCompression = ref toc.BlockCompressions.DangerousGetReferenceAt(blockIndex);
                blockCompression = asCopy ? CompressionPreference.Copy : Compression;

                BlockHelpers.StartProcessingBlock(tocBuilder, blockIndex);
                BlockHelpers.WriteToOutput(settings.Output, compressedAlloc, blockSize.CompressedSize);
                BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
            }
        }
    }
}
