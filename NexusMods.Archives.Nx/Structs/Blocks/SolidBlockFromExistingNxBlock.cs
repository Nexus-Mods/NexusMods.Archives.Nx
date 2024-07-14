using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing.Unpack;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     Represents a SOLID block that's backed by an existing SOLID block inside
///     another Nx archive.
/// </summary>
/// <param name="Items">Items tied to the given block.</param>
/// <param name="NxSource">Compression method to use.</param>
/// <param name="StartOffset">Starting offset of the compressed block.</param>
/// <param name="BlockSize">Size of the compressed block at <paramref name="StartOffset"/>.</param>
/// <param name="Compression">Compression method used by the data.</param>
internal record SolidBlockFromExistingNxBlock<T>(PathedFileEntry[] Items, IFileDataProvider NxSource, ulong StartOffset, int BlockSize, CompressionPreference Compression) : IBlock<T>
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
    public unsafe void ProcessBlock(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools)
    {
        var toc = tocBuilder.Toc;
        for (var x = 0; x < Items.Length; x++)
        {
            var item = Items[x];

            // Write file info
            ref var file = ref tocBuilder.GetAndIncrementFileAtomic();
            file.FilePathIndex = tocBuilder.FileNameToIndexDictionary[item.FilePath];
            file.FirstBlockIndex = blockIndex;
            file.DecompressedSize = item.Entry.DecompressedSize;
            file.DecompressedBlockOffset = item.Entry.DecompressedBlockOffset;
            file.Hash = item.Entry.Hash;
        }

        ref var blockSize = ref toc.Blocks.DangerousGetReferenceAt(blockIndex);
        blockSize.CompressedSize = BlockSize;

        ref var blockCompression = ref toc.BlockCompressions.DangerousGetReferenceAt(blockIndex);
        blockCompression = Compression;

        using var rawCompressedData = NxSource.GetFileData(StartOffset, (uint)BlockSize);
        BlockHelpers.WaitForBlockTurn(tocBuilder, blockIndex);
        BlockHelpers.WriteToOutput(settings.Output, new Span<byte>(rawCompressedData.Data, BlockSize));
        BlockHelpers.EndProcessingBlock(tocBuilder, settings.Progress);
    }
}
