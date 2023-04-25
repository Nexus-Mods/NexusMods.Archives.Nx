using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Traits;

namespace NexusMods.Archives.Nx;

/// <summary>
///     Utility for creating `.nx` archives.
///     This represents the library's Public API.
/// </summary>
public class NxPacker
{
    /// <summary>
    ///     Packs a new '.nx' file.
    /// </summary>
    /// <param name="files">The files to be packed.</param>
    /// <param name="settings">Settings to use in the packing operation.</param>
    public async Task PackAsync(PackerFile[] files, PackerSettings settings)
    {
        // TODO: Packing Code
        files.SortBySizeAscending();
        var groups = MakeGroups(files);
        var blocks = MakeBlocks(groups, settings.BlockSize, settings.ChunkSize, settings.SolidBlockAlgorithm,
            settings.ChunkedFileAlgorithm);

        //var pool = StringPool.Pack(files.AsSpan());
        //var toc  = new TableOfContents(blocks, pool);
        throw new NotImplementedException();
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
        CompressionPreference solidBlockAlgorithm, CompressionPreference chunkedBlockAlgorithm)
        where T : IHasFileSize, IHasSolidType, IHasCompressionPreference
    {
        var blocks = new List<IBlock<T>>();
        var currentBlock = new List<T>();

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
        where T : IHasFileSize, IHasSolidType, IHasCompressionPreference
    {
        var sizeLeft = item.FileSize;
        long currentOffset = 0;

        while (sizeLeft > 0)
        {
            var readSize = Math.Min(sizeLeft, chunkSize);
            var preference = item.CompressionPreference;
            if (preference == CompressionPreference.NoPreference)
                preference = chunkedBlockAlgorithm;

            blocks.Add(new ChunkedFileBlock<T>(item, currentOffset, (int)readSize, preference));

            currentOffset += readSize;
            sizeLeft -= readSize;
        }
    }
}

internal static class PackerExtensions
{
    internal static void SortBySizeAscending<T>(this T[] items) where T : IHasFileSize =>
        Array.Sort(items, (a, b) => a.FileSize.CompareTo(b.FileSize));
}
