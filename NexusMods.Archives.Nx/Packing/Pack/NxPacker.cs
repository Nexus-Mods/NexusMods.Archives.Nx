using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Packing.Pack.Steps;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Packing.Pack;

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
        var groups = GroupFiles.Do(files);
        var blocks = MakeBlocks.Do(groups, settings.BlockSize, settings.ChunkSize, settings.SolidBlockAlgorithm, settings.ChunkedFileAlgorithm);
        using var toc = new TableOfContentsBuilder<PackerFile>(blocks, files);

        // Let's go!
        var stream = settings.Output;
        var tocSize = toc.CalculateTableSize();
        var headerData = Polyfills.AllocatePinnedArray<byte>(tocSize + sizeof(NativeFileHeader));
        stream.SetLength(((long)headerData.Length).RoundUp4096());
        stream.Position = stream.Length;

        fixed (byte* headerDataPtr = headerData)
        {
            // Pack the Blocks
            // Note: Blocks must be packed 'in-order' for chunked files; because their blocks need to be sequential.
            using var sched = new OrderedTaskScheduler(settings.MaxNumThreads);
            using var pool = new PackerArrayPools(settings.MaxNumThreads, settings.BlockSize, toc.CanCreateChunks ? settings.ChunkSize : null);
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
            toc.Build(headerDataPtr + sizeof(NativeFileHeader), sizeof(NativeFileHeader), tocSize);
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
}

internal static class PackerExtensions
{
    internal static void SortBySizeAscending<T>(this T[] items) where T : IHasFileSize =>
        Array.Sort(items, (a, b) => a.FileSize.CompareTo(b.FileSize));
}
