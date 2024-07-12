using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Traits;
using System.Runtime.InteropServices;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Packing.Pack.Steps;

/// <summary>
///     This is a step of the .NX packing process that involves creating
///     blocks from groups of files created by <see cref="GroupFiles"/>.
///     (In ascending size order)
///
///     The input is a dictionary of file groups, where the key is the file extension.
///     Inside each group, is a sorted list of files by size.
///
///     For example, suppose you have the following files in the `.txt` group:
///     - `file1.txt` (1 KiB)
///     - `file2.txt` (2 KiB)
///     - `file3.txt` (4 KiB)
///
///     In this scenario, the `MakeBlocks` step will create a block of `file1.txt`
///     and `file2.txt`. And another block of `file3.txt`.
///     (Files bigger than block size are compressed in single block)
///
///     Sizes of individual blocks can be further constrained by 'chunk size'.
///     Suppose you have a file which is 100 KiB in size. And the chunk size is 32K.
///
///     This will create 3 (chunk) blocks of 32 KiB each. And 1 (chunk) block of 4KiB.
///
///     The Nx packing pipeline typically starts with the following steps:
///     - Sort Files Ascending by Size
///     - Group File by Extension
///     - Make Blocks from File Groups (👈 This Class ‼️)
/// </summary>
internal static class MakeBlocks
{
    internal static List<IBlock<T>> Do<T>(Dictionary<string, List<T>> groups, int blockSize, int chunkSize,
        CompressionPreference solidBlockAlgorithm = CompressionPreference.NoPreference,
        CompressionPreference chunkedBlockAlgorithm = CompressionPreference.NoPreference)
        where T : IHasFileSize, IHasSolidType, IHasCompressionPreference, ICanProvideFileData, IHasRelativePath
    {
        var chunkedBlocks = new List<IBlock<T>>();
        var solidBlocks = new List<(int size, IBlock<T> block)>();
        var currentBlock = new List<T>();
        long currentBlockSize = 0;

        // Default algorithms if no preference is specified.
        if (solidBlockAlgorithm == CompressionPreference.NoPreference)
            solidBlockAlgorithm = CompressionPreference.Lz4;

        if (chunkedBlockAlgorithm == CompressionPreference.NoPreference)
            chunkedBlockAlgorithm = CompressionPreference.ZStandard;

        // Make the blocks.
        foreach (var keyValue in groups)
        {
            var values = keyValue.Value;

            foreach (var item in values)
            {
                // If the item is too big, it's getting chunked, regardless of preference.
                // Note: This is not a typo; we treat items above block size as chunked, for convenience.
                if (item.FileSize > blockSize)
                {
                    ChunkItem(item, chunkedBlocks, chunkSize, chunkedBlockAlgorithm);
                    continue;
                }

                if (item.SolidType == SolidPreference.NoSolid)
                {
                    solidBlocks.Add(((int)item.FileSize, new SolidBlock<T>(new List<T> { item }, item.CompressionPreference)));
                    continue;
                }

                // Check if the item fits in the current block
                if (currentBlockSize + item.FileSize <= blockSize)
                {
                    // [Hot Path] Add item to SOLID block.
                    currentBlock.Add(item);
                    currentBlockSize += item.FileSize;
                }
                else
                {
                    // [Cold Path] Add the current block if it has any items and start a new block
                    if (currentBlock.Count > 0)
                        solidBlocks.Add(((int)currentBlockSize, new SolidBlock<T>(currentBlock, solidBlockAlgorithm)));

                    currentBlock = new List<T> { item };
                    currentBlockSize = item.FileSize;
                }
            }
        }

        // If we have any items left, make sure to append them.
        if (currentBlock.Count > 0)
            solidBlocks.Add(((int)currentBlockSize, new SolidBlock<T>(currentBlock, solidBlockAlgorithm)));

        // Sort the SOLID blocks by size in descending order
        solidBlocks.Sort((a, b) => b.size.CompareTo(a.size));

        // Note(sewer): Chunked blocks cannot be reordered due to their nature of being
        // sequential. However we can sort the solid blocks to improve compression efficiency.
        // Append the solid blocks to the chunked blocks.
#if NET5_0_OR_GREATER
        var sortedBlocksSpan = CollectionsMarshal.AsSpan(solidBlocks);
        for (var x = 0; x < sortedBlocksSpan.Length; x++)
        {
            chunkedBlocks.Add(sortedBlocksSpan[x].block);
#else
        for (var x = 0; x < solidBlocks.Count; x++)
        {
            chunkedBlocks.Add(solidBlocks[x].block);
#endif
        }

        // Sort the final blocks by size, to improve compression efficiency.
        return chunkedBlocks;
    }

    private static void ChunkItem<T>(T item, List<IBlock<T>> blocks,
        int chunkSize, CompressionPreference chunkedBlockAlgorithm)
        where T : IHasFileSize, IHasSolidType, IHasCompressionPreference, ICanProvideFileData, IHasRelativePath
    {
        var sizeLeft = (ulong)item.FileSize;
        ulong currentOffset = 0;

        if (chunkedBlockAlgorithm == CompressionPreference.NoPreference)
            chunkedBlockAlgorithm = CompressionPreference.ZStandard;

        var numIterations = sizeLeft / (uint)chunkSize;
        var remainingSize = sizeLeft % (uint)chunkSize;
        var numChunks = remainingSize > 0 ? numIterations + 1 : numIterations;

        var state = new ChunkedBlockState<T>
        {
            Compression = chunkedBlockAlgorithm,
            NumChunks = (int)numChunks,
            File = item
        };

        var x = (ulong)0;
        for (; x < numIterations; x++)
        {
            blocks.Add(new ChunkedFileBlock<T>(currentOffset, chunkSize, (int)x, state));
            currentOffset += (ulong)chunkSize;
        }

        if (remainingSize > 0)
            blocks.Add(new ChunkedFileBlock<T>(currentOffset, (int)remainingSize, (int)x, state));
    }
}
