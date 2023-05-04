using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Packing;

/// <summary>
///     Utility for creating `.nx` archives.
/// </summary>
public static class NxPacker
{
    /// <summary>
    ///     Packs a new '.nx' file using the specified settings.
    /// </summary>
    /// <param name="files">The files to be packed.</param>
    /// <param name="settings">Settings to use in the packing operation.</param>
    public static unsafe void Pack(PackerFile[] files, PackerSettings settings)
    {
        // Init Packing Code
        settings.Sanitize();
        files.SortBySizeAscending();
        var groups = MakeGroups(files);
        var blocks = MakeBlocks(groups, settings.BlockSize, settings.ChunkSize, settings.SolidBlockAlgorithm, settings.ChunkedFileAlgorithm);
        using var toc = new TableOfContentsBuilder<PackerFile>(blocks, files);

        // Let's go!
        var stream = settings.Output;
        var headerData = Polyfills.AllocatePinnedArray<byte>(toc.CalculateTableSize() + sizeof(NativeFileHeader));
        stream.SetLength(((long)headerData.Length).RoundUp4096());
        stream.Position = stream.Length;

        fixed (byte* headerDataPtr = headerData)
        {
            // Pack the Blocks
            // Note: Blocks must be packed 'in-order' for chunked files; because their blocks need to be sequential.
            var sched = new OrderedTaskScheduler(settings.MaxNumThreads);
            var pool = new PackerArrayPools(settings.MaxNumThreads, settings.BlockSize, toc.CanCreateChunks ? settings.ChunkSize : null);
            var context = new BlockContext
            {
                Settings = settings,
                TocBuilder = toc,
                PackerPool = pool
            };

            for (var x = 0; x < blocks.Count; x++)
            {
                var block = blocks[x];
                Task.Factory.StartNew(PackBlock, new BlockData
                {
                    Block = block,
                    BlockIndex = x,
                    Context = context
                }, CancellationToken.None, TaskCreationOptions.None, sched);
            }

            // Waits for all jobs to complete.
            sched.Dispose();

            // Truncate stream.
            stream.SetLength(stream.Position);

            // Write headers.
            stream.Seek(0, SeekOrigin.Begin);
            NativeFileHeader.Init((NativeFileHeader*)headerDataPtr, toc.Version, settings.BlockSize, settings.ChunkSize, headerData.Length);
            toc.Build(headerDataPtr + sizeof(NativeFileHeader));
            stream.Write(headerData, 0, headerData.Length);
        }
    }

    private class BlockData
    {
        public required IBlock<PackerFile> Block;
        public required int BlockIndex;
        public required BlockContext Context;
    }

    private struct BlockContext
    {
        public required PackerSettings Settings;
        public required TableOfContentsBuilder<PackerFile> TocBuilder;
        public required PackerArrayPools PackerPool;
    }

    private static void PackBlock(object? obj)
    {
        var data = (BlockData)obj!;
        var context = data.Context;
        var settings = context.Settings;
        var tocBuilder = context.TocBuilder;

        // Compress that block!
        data.Block.ProcessBlock(tocBuilder, settings, data.BlockIndex, context.PackerPool);
    }

    // Note: Items inside each dictionary must preserve ascending order.
    internal static Dictionary<string, List<T>> MakeGroups<T>(T[] files) where T : IHasRelativePath
    {
        // Note: This could probably do with some fewer allocations, but it's okay for now.
        // Throwing in SpanOfCharDict from VFS is probably overkill here.
        var results = new Dictionary<string, List<T>>();
        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.RelativePath);
            if (!results.TryGetValue(extension, out var items))
            {
                items = new List<T>();
                results[extension] = items;
            }

            items.Add(file);
        }

        return results;
    }

    internal static List<IBlock<T>> MakeBlocks<T>(Dictionary<string, List<T>> items, int blockSize, int chunkSize,
        CompressionPreference solidBlockAlgorithm = CompressionPreference.NoPreference,
        CompressionPreference chunkedBlockAlgorithm = CompressionPreference.NoPreference)
        where T : IHasFileSize, IHasSolidType, IHasCompressionPreference, ICanProvideFileData, IHasRelativePath
    {
        var blocks = new List<IBlock<T>>();
        var currentBlock = new List<T>();

        // Default algorithms if no preference is specified.
        if (solidBlockAlgorithm == CompressionPreference.NoPreference)
            solidBlockAlgorithm = CompressionPreference.Lz4;

        if (chunkedBlockAlgorithm == CompressionPreference.NoPreference)
            chunkedBlockAlgorithm = CompressionPreference.ZStandard;

        // Make the blocks.
        foreach (var keyValue in items)
        {
            var values = keyValue.Value;
            long currentBlockSize = 0;

            foreach (var item in values)
            {
                // If the item is too big, it's getting chunked, regardless of preference.
                if (item.FileSize > chunkSize)
                {
                    ChunkItem(item, blocks, chunkSize, chunkedBlockAlgorithm);
                    continue;
                }

                if (item.SolidType == SolidPreference.NoSolid)
                {
                    blocks.Add(new SolidBlock<T>(new List<T> { item }, item.CompressionPreference));
                    continue;
                }

                // Add item to SOLID block.
                currentBlock.Add(item);
                currentBlockSize += item.FileSize;
                if (currentBlockSize < blockSize)
                    continue;

                // Add SOLID block, and reset items in block.
                blocks.Add(new SolidBlock<T>(currentBlock, solidBlockAlgorithm));

                currentBlock = new List<T>();
                currentBlockSize = 0;
            }
        }

        // If we have any items left, make sure to append them.
        if (currentBlock.Count > 0)
            blocks.Add(new SolidBlock<T>(currentBlock, solidBlockAlgorithm));

        return blocks;
    }

    private static void ChunkItem<T>(T item, List<IBlock<T>> blocks, int chunkSize,
        CompressionPreference chunkedBlockAlgorithm)
        where T : IHasFileSize, IHasSolidType, IHasCompressionPreference, ICanProvideFileData, IHasRelativePath
    {
        var sizeLeft = item.FileSize;
        long currentOffset = 0;

        if (chunkedBlockAlgorithm == CompressionPreference.NoPreference)
            chunkedBlockAlgorithm = CompressionPreference.ZStandard;

        var numIterations = sizeLeft / chunkSize;
        var remainingSize = sizeLeft % chunkSize;
        var numChunks = remainingSize > 0 ? numIterations + 1 : numIterations;

        var state = new ChunkedBlockState<T>
        {
            Compression = chunkedBlockAlgorithm,
            NumChunks = (int)numChunks,
            File = item
        };

        var x = 0;
        for (; x < numIterations; x++)
        {
            blocks.Add(new ChunkedFileBlock<T>(currentOffset, chunkSize, x, state));
            currentOffset += chunkSize;
        }

        if (remainingSize > 0)
            blocks.Add(new ChunkedFileBlock<T>(currentOffset, (int)remainingSize, x, state));
    }
}

internal static class PackerExtensions
{
    internal static void SortBySizeAscending<T>(this T[] items) where T : IHasFileSize =>
        Array.Sort(items, (a, b) => a.FileSize.CompareTo(b.FileSize));
}
