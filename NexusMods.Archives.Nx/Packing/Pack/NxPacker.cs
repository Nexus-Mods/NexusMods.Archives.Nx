using System.Diagnostics;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Packing.Pack.Steps;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;

#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

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
    public static void Pack(PackerFile[] files, PackerSettings settings)
    {
        // Init Packing Code
        var blocks = InitPack(files, settings);
        PackWithBlocksAndFiles(files.AsSpan(), settings, blocks);
    }

    /// <summary>
    ///     Packs a new '.nx' file using the specified settings.
    /// </summary>
    /// <param name="files">The files to be packed.</param>
    /// <param name="copiedBlocks">Existing blocks that are sourced from external sources.</param>
    /// <param name="settings">Settings to use in the packing operation.</param>
    internal static void PackWithExistingBlocks(PackerFile[] files, List<IBlock<PackerFile>> copiedBlocks, PackerSettings settings)
    {
        // Init Packing Code
        var blocks = InitPack(files, settings);

        // Add existing files to the list.
        var newFileCount = 0;
        foreach (var block in blocks)
            newFileCount += block.FileCount();

        foreach (var block in copiedBlocks)
            newFileCount += block.FileCount();

        var newFiles = Polyfills.AllocateUninitializedArray<HasRelativePathWrapper>(newFileCount);
        var insertIdx = 0;
        for (; insertIdx < files.Length; insertIdx++)
        {
            Debug.Assert(!string.IsNullOrEmpty(files[insertIdx].RelativePath), "We're adding an empty file path to the list of files to pack.");
            newFiles.DangerousGetReferenceAt(insertIdx) = files[insertIdx].RelativePath;
        }

        // Skips IEnumerator.
#if NET5_0_OR_GREATER
        foreach (var block in CollectionsMarshal.AsSpan(copiedBlocks))
#else
        foreach (var block in copiedBlocks)
#endif
            block.AppendFilesUnsafe(ref insertIdx, newFiles);

        // Note: We handle the copied blocks first, even though they take
        //       the shortest to process because deduplication means we may
        //       reuse copied blocks in the new files.
        copiedBlocks.AddRange(blocks);
        PackWithBlocksAndFiles(newFiles.AsSpan(), settings, copiedBlocks);
    }

    /// <summary>
    ///     Packs an `.nx` file using the specified relative paths and blocks to pack.
    /// </summary>
    /// <param name="relativePaths">
    ///     Contains all of the relative paths which should exist within the archive.
    ///     This should cover all relative paths to be packed by the <paramref name="blocks"/>.
    /// </param>
    /// <param name="settings">The settings used to pack the archive.</param>
    /// <param name="blocks">
    ///     Listing of all blocks to be packed.
    ///     These blocks should contain all files listed in <paramref name="relativePaths"/>.
    /// </param>
    private static unsafe void PackWithBlocksAndFiles<TWithRelativePath>(Span<TWithRelativePath> relativePaths, PackerSettings settings,
        List<IBlock<PackerFile>> blocks)
        where TWithRelativePath : IHasRelativePath
    {
        using var toc = TableOfContentsBuilder<PackerFile>.Create(blocks, relativePaths);

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
            using (var pool = new PackerArrayPools(settings.MaxNumThreads, settings.BlockSize, toc.CanCreateChunks ? settings.ChunkSize : null))
            using (var sched = new OrderedTaskScheduler(settings.MaxNumThreads))
            {
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

                // Implicit dispose of `sched` waits for all jobs to complete.
            }

            // Truncate stream.
            stream.SetLength(stream.Position);

            // Write headers.
            stream.Seek(0, SeekOrigin.Begin);
            NativeFileHeader.Init((NativeFileHeader*)headerDataPtr, settings.ChunkSize, headerData.Length);
            toc.Build(headerDataPtr + sizeof(NativeFileHeader), tocSize);
            stream.Write(headerData, 0, headerData.Length);
        }
    }

    private static List<IBlock<PackerFile>> InitPack(PackerFile[] files, PackerSettings settings)
    {
        settings.Sanitize();

        // Sort into groups and blocks.
        files.SortBySizeAscending();
        var groups = GroupFiles.Do(files);
        return MakeBlocks.Do(groups, settings.BlockSize, settings.ChunkSize, settings.SolidBlockAlgorithm, settings.ChunkedFileAlgorithm, settings.SolidDeduplicationState, settings.ChunkedDeduplicationState);
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
