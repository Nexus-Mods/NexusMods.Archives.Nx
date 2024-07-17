using System.Runtime.CompilerServices;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing.Pack;
using NexusMods.Archives.Nx.Packing.Unpack;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Packing;

/// <summary>
///     This is an extended version of the <see cref="NxPackerBuilder"/>
///     that allows for the repacking of existing Nx archives.
/// </summary>
public class NxRepackerBuilder : NxPackerBuilder
{
    private const byte DefaultChunkSizeByte = byte.MaxValue;

    /// <summary>
    ///     All existing blocks that are sourced from external, existing Nx archives.
    /// </summary>
    private List<IBlock<PackerFile>> ExistingBlocks { get; } = new();

    /// <summary>
    ///     Contains all the data to be repacked grouped by Nx source.
    /// </summary>
    private Dictionary<IFileDataProvider, NxSourceData> RepackerData { get; } = new();

    private byte _chunkSizeByte = byte.MaxValue;

    /// <summary>
    ///     Adds an existing file from an Nx archive to the repack operation.
    /// </summary>
    /// <param name="nxSource">The source of the Nx archive data.</param>
    /// <param name="header">The parsed header of the source Nx archive.</param>
    /// <param name="entry">The file entry to add from the source archive.</param>
    /// <returns>The current instance of NxRepackerBuilder.</returns>
    public NxRepackerBuilder AddFileFromNxArchive(IFileDataProvider nxSource, ParsedHeader header, FileEntry entry)
    {
        UpdateChunkSize(header);
        if (!RepackerData.TryGetValue(nxSource, out var sourceData))
        {
            sourceData = new NxSourceData { Header = header, Entries = new() { entry } };
            RepackerData[nxSource] = sourceData;
            return this;
        }

        sourceData.Entries.Add(entry);
        return this;
    }

    /// <summary>
    ///     Adds multiple existing files from an Nx archive to the repack operation.
    /// </summary>
    /// <param name="nxSource">The source of the Nx archive data.</param>
    /// <param name="header">The parsed header of the source Nx archive.</param>
    /// <param name="entries">A span of file entries to add from the source archive.</param>
    /// <returns>The current instance of NxRepackerBuilder.</returns>
    public NxRepackerBuilder AddFilesFromNxArchive(IFileDataProvider nxSource, ParsedHeader header, Span<FileEntry> entries)
    {
        UpdateChunkSize(header);
        if (!RepackerData.TryGetValue(nxSource, out var sourceData))
        {
            sourceData = new NxSourceData { Header = header, Entries = new() };
            RepackerData[nxSource] = sourceData;
        }

        foreach (var entry in entries)
            sourceData.Entries.Add(entry);

        return this;
    }

    /// <summary>
    ///     Adds multiple existing files from an Nx archive to the repack operation.
    /// </summary>
    /// <param name="nxSource">The source of the Nx archive data.</param>
    /// <param name="header">The parsed header of the source Nx archive.</param>
    /// <param name="entries">An enumerable collection of file entries to add from the source archive.</param>
    /// <returns>The current instance of NxRepackerBuilder.</returns>
    public NxRepackerBuilder AddFilesFromNxArchive(IFileDataProvider nxSource, ParsedHeader header, IEnumerable<FileEntry> entries)
    {
        UpdateChunkSize(header);
        if (!RepackerData.TryGetValue(nxSource, out var sourceData))
        {
            sourceData = new NxSourceData { Header = header, Entries = new(entries) };
            RepackerData[nxSource] = sourceData;
            return this;
        }

        foreach (var entry in entries)
            sourceData.Entries.Add(entry);

        return this;
    }

    /// <summary>
    ///     Builds the archive in the configured destination.
    /// </summary>
    /// <returns>The output stream.</returns>
    public new Stream Build(bool disposeOutput = true)
    {
        // Update ChunkSize if not already set.
        if (_chunkSizeByte != byte.MaxValue)
            WithChunkSize(32768 << _chunkSizeByte);

        var blocks = new List<IBlock<PackerFile>>();
        var files = new List<PackerFile>();
        MakeItemsFromNxFiles(blocks, files);
        blocks.AddRange(ExistingBlocks);
        files.AddRange(Files);

        if (blocks.Count > 0)
            NxPacker.PackWithExistingBlocks(files.ToArray(), blocks, Settings);
        else
            NxPacker.Pack(files.ToArray(), Settings);

        if (disposeOutput)
            Settings.Output.Dispose();

        return Settings.Output;
    }

    /// <summary>
    ///     Adds an existing block that's been externally generated to the archive.
    /// </summary>
    /// <param name="block">The block to add to the compressor input.</param>
    internal NxPackerBuilder AddBlock(IBlock<PackerFile> block)
    {
        ExistingBlocks.Add(block);
        return this;
    }

    private void MakeItemsFromNxFiles(List<IBlock<PackerFile>> blocks, List<PackerFile> files)
    {
        // This function makes blocks from our existing inputs, i.e.
        // that which is contained in RepackerData
        foreach (var data in RepackerData)
            AddBlocks(data, blocks, files);
    }

    private static unsafe void AddBlocks(KeyValuePair<IFileDataProvider, NxSourceData> data, List<IBlock<PackerFile>> blocks, List<PackerFile> files)
    {
        var nxSource = data.Key;
        var sourceData = data.Value;

        // First count the number of files that exist in each block of the source
        // archive, this will allow us to determine if we can copy SOLID blocks
        // verbatim.
        var blockCount = sourceData.Header.Blocks.Length;
        var origArchiveBlockFileCounts = new int[blockCount];

        foreach (var entry in sourceData.Header.Entries)
            origArchiveBlockFileCounts.DangerousGetReferenceAt(entry.FirstBlockIndex)++;

        // Now we group all the file entries by their block index.
        // Later down the road, if the file count per block matches the old file,
        // the blocks may be copied verbatim.

        using var blockList = new BlockList<FileEntry>(blockCount, sourceData.Header.Entries.Length);
        foreach (var entry in sourceData.Entries)
        {
            var blockIndex = entry.FirstBlockIndex;
            var itemCount = origArchiveBlockFileCounts.DangerousGetReferenceAt(entry.FirstBlockIndex);
            var list = blockList.GetOrCreateList(blockIndex, itemCount);
            list->Push(entry);
        }

        // Now create the blocks for the new archive.
        foreach (var block in blockList.GetAllBlocks())
        {
            if (!block.IsValid || block.Count <= 0)
                continue;

            var isChunkedFile = IsChunkedFile(block);
            if (isChunkedFile)
            {
                PackerBuilderHelpers.CreateChunkedFileFromExistingNxBlock(nxSource, sourceData.Header, block.AsSpan()[0], blocks);
            }
            else
            {
                // Determine if the file belongs to a full SOLID block that requires copying verbatim.
                var firstFile = block.AsSpan()[0];
                var blockIndex = firstFile.FirstBlockIndex;
                var isFullBlockCopy = origArchiveBlockFileCounts.DangerousGetReferenceAt(blockIndex) == block.Count;

                var blockSize = sourceData.Header.Blocks[blockIndex]; // bounds check
                var blockOffset = sourceData.Header.BlockOffsets.DangerousGetReferenceAt(blockIndex); // same length by definition
                var compression = sourceData.Header.BlockCompressions.DangerousGetReferenceAt(blockIndex); // same length by definition

                if (isFullBlockCopy)
                {
                    var items = Polyfills.AllocateUninitializedArray<PathedFileEntry>(block.Count);
                    var span = block.AsSpan();
                    for (var x = 0; x < span.Length; x++)
                    {
                        ref var entry = ref span[x];
                        items[x] = new PathedFileEntry
                        {
                            Entry = entry,
                            FilePath = sourceData.Header.Pool[entry.FilePathIndex]
                        };
                    }

                    blocks.Add(new SolidBlockFromExistingNxBlock<PackerFile>(
                        items,
                        nxSource,
                        blockOffset,
                        blockSize.CompressedSize,
                        compression
                    ));
                }
                else
                {
                    var lazyBlock = new LazyRefCounterDecompressedNxBlock(nxSource, blockOffset, (ulong)blockSize.CompressedSize, compression);

                    // Add multiple files from the SOLID block
                    foreach (var entry in block.AsSpan())
                    {
                        var fromExistingNxBlock = new FromExistingNxBlock(lazyBlock, entry);
                        lazyBlock.ConsiderFile(entry);
                        files.Add(new PackerFile
                        {
                            RelativePath = sourceData.Header.Pool[entry.FilePathIndex],
                            FileSize = (long)entry.DecompressedSize,
                            FileDataProvider = fromExistingNxBlock
                        });
                    }
                }
            }
        }
    }

    private static bool IsChunkedFile(FixedSizeList<FileEntry> block)
    {
        // If the file is a chunked file, one of the following has to be true:
        // - There is only one file in the block.
        //   - This could also mean it's just a file with a single block.
        // OR
        // - All files in the block refer to same block index (int) and offset (0).
        //   - This means files got deduplicated.
        var isChunkedFile = true;
        if (block.Count <= 1)
            return isChunkedFile;

        var blockSpan = block.AsSpan();
        var firstBlockIndex = blockSpan[0].FirstBlockIndex;
        long decompressedBlockOffset = blockSpan[0].DecompressedBlockOffset;

        for (var x = 1; x < blockSpan.Length; x++)
        {
            if (blockSpan[x].FirstBlockIndex == firstBlockIndex &&
                blockSpan[x].DecompressedBlockOffset == decompressedBlockOffset)
                continue;

            isChunkedFile = false;
            break;
        }

        return isChunkedFile;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateChunkSize(ParsedHeader header)
    {
        if (_chunkSizeByte == DefaultChunkSizeByte)
            _chunkSizeByte = header.Header.ChunkSize;
        else if (_chunkSizeByte != header.Header.ChunkSize)
            ThrowMixedChunkSizes();
    }

    private static void ThrowMixedChunkSizes() => throw new ArgumentException("All chunked files must have the same chunk size.");

    /// <summary>
    ///     Contains all the data associated with a single Nx source.
    /// </summary>
    private class NxSourceData
    {
        /// <summary>
        ///     The pre-parsed header associated with this source.
        /// </summary>
        public required ParsedHeader Header { get; init; }

        /// <summary>
        ///     The entries from the Nx archive to be repacked.
        /// </summary>
        public required List<FileEntry> Entries { get; init; }
    }
}
