using System.Runtime.CompilerServices;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Headers.Structs;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing.Unpack;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Packing;

/// <summary>
///     Helper methods that can be used for manually constructing reused chunks to be used
///     with <see cref="NxPackerBuilder"/>. For internal low level use, and testing.
/// </summary>
internal class PackerBuilderHelpers
{
    /// <summary/>
    /// <param name="nxSource">Provides the ability to read from an .nx archive.</param>
    /// <param name="header">The header of the NX file represented by <paramref name="nxSource"/></param>
    /// <param name="blockIndex">Index of the block to be copied verbatim.</param>
    internal static SolidBlockFromExistingNxBlock<PackerFile> CreateSolidBlockFromExistingNxBlock(IFileDataProvider nxSource, ParsedHeader header,
        int blockIndex)
    {
        var block = header.Blocks[blockIndex];
        var blockOffset = header.BlockOffsets.DangerousGetReferenceAt(blockIndex);
        var compression = header.BlockCompressions.DangerousGetReferenceAt(blockIndex);

        var items = new List<PathedFileEntry>();
        foreach (var entry in header.Entries)
        {
            if (entry.FirstBlockIndex == blockIndex)
            {
                items.Add(new PathedFileEntry
                {
                    Entry = entry,
                    FilePath = header.Pool[entry.FilePathIndex]
                });
            }
        }

        return new SolidBlockFromExistingNxBlock<PackerFile>(
            items.ToArray(),
            nxSource,
            blockOffset,
            block.CompressedSize,
            compression
        );
    }

    /// <summary/>
    /// <param name="nxSource">Provides the ability to read from an .nx archive.</param>
    /// <param name="header">The header of the NX file represented by <paramref name="nxSource"/>. Used for metadata only.</param>
    /// <param name="entry">The entry of the individual item in the archive.</param>
    /// <param name="blocks"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CreateChunkedFileFromExistingNxBlock(IFileDataProvider nxSource, ParsedHeader header,
        FileEntry entry, List<IBlock<PackerFile>> blocks)
    {
        var chunkSize = header.Header.ChunkSizeBytes;
        var numChunks = entry.GetChunkCount(chunkSize);
        var sharedState = new ChunkedBlockFromExistingNxState
        {
            NumChunks = numChunks,
            NxSource = nxSource,
            RelativePath = header.Pool.DangerousGetReferenceAt(entry.FilePathIndex),
            FileLength = entry.DecompressedSize,
            FileHash = entry.Hash
        };

        for (var chunkIndex = 0; chunkIndex < numChunks; chunkIndex++)
        {
            var blockIndex = entry.FirstBlockIndex + chunkIndex;
            var block = header.Blocks[blockIndex]; // Indices below guaranteed by virtue of length equality.
            var blockOffset = header.BlockOffsets.DangerousGetReferenceAt(blockIndex);
            var compression = header.BlockCompressions.DangerousGetReferenceAt(blockIndex);

            blocks.Add(new ChunkedFileFromExistingNxBlock<PackerFile>(
                blockOffset,
                block.CompressedSize,
                chunkIndex,
                sharedState,
                compression
            ));
        }
    }

    /// <summary>
    ///     Adds a number of files from a SOLID block in an existing .nx archive
    ///     to a <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The builder to add the files to.</param>
    /// <param name="nxSource">Allows you to access the source archive.*</param>
    /// <param name="blockOffset">Offset of the block in the <paramref name="nxSource"/></param>
    /// <param name="blockSize">Size of the block at <paramref name="blockOffset"/> in <paramref name="nxSource"/></param>
    /// <param name="compression">The compression used in this block.</param>
    /// <param name="items">The files belonging to this block that should be added.</param>
    internal static LazyRefCounterDecompressedNxBlock AddPartialSolidBlock(NxPackerBuilder builder, FromStreamProvider nxSource, ulong blockOffset,
        BlockSize blockSize, CompressionPreference compression, List<PathedFileEntry> items)
    {
        var lazyBlock = new LazyRefCounterDecompressedNxBlock(nxSource, blockOffset, (ulong)blockSize.CompressedSize, compression);

        // Add multiple files from the SOLID block
        foreach (var item in items)
        {
            var fromExistingNxBlock = new FromExistingNxBlock(lazyBlock, item.Entry);
            lazyBlock.ConsiderFile(item.Entry);
            builder.AddPackerFile(new PackerFile
            {
                RelativePath = item.FilePath,
                FileSize = (long)item.Entry.DecompressedSize,
                FileDataProvider = fromExistingNxBlock
            });
        }

        return lazyBlock;
    }
}
