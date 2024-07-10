using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing.Unpack;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Packing;

/// <summary>
///     Helper methods for the <see cref="NxPackerBuilder"/>.
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
        var blockOffset = header.BlockOffsets[blockIndex];
        var compression = header.BlockCompressions[blockIndex];

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
            items,
            nxSource,
            blockOffset,
            block.CompressedSize,
            compression
        );
    }

    /// <summary/>
    /// <param name="nxSource">Provides the ability to read from an .nx archive.</param>
    /// <param name="header">The header of the NX file represented by <paramref name="nxSource"/></param>
    /// <param name="entry">The entry of the individual item in the archive.</param>
    internal static ChunkedFileFromExistingNxBlock<PackerFile>[] CreateChunkedFileFromExistingNxBlock(IFileDataProvider nxSource, ParsedHeader header,
        FileEntry entry)
    {
        var chunkSize = header.Header.ChunkSizeBytes;
        var numChunks = entry.GetChunkCount(chunkSize);
        var result = Polyfills.AllocateUninitializedArray<ChunkedFileFromExistingNxBlock<PackerFile>>(numChunks);

        for (var chunkIndex = 0; chunkIndex < numChunks; chunkIndex++)
        {
            var blockIndex = entry.FirstBlockIndex + chunkIndex;
            var block = header.Blocks[blockIndex];
            var blockOffset = header.BlockOffsets[blockIndex];
            var compression = header.BlockCompressions[blockIndex];

            result.DangerousGetReferenceAt(chunkIndex) = new ChunkedFileFromExistingNxBlock<PackerFile>(
                blockOffset,
                block.CompressedSize,
                chunkIndex,
                new ChunkedBlockFromExistingNxState
                {
                    NumChunks = numChunks,
                    NxSource = nxSource,
                    RelativePath = header.Pool.DangerousGetReferenceAt(entry.FilePathIndex),
                    FileLength = entry.DecompressedSize,
                    FileHash = entry.Hash
                },
                compression
            );
        }

        return result;
    }
}
