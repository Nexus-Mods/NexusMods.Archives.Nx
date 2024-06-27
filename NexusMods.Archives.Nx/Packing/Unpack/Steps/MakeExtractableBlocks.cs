using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;

namespace NexusMods.Archives.Nx.Packing.Unpack.Steps;

/// <summary>
///     This step takes the specified outputs and determines which blocks need
///     to be extracted from the Nx archive.
///
///     The parameter (outputs) specifies which files we need extracted and where,
///     and the return value is the blocks that need, with the corresponding
///     outputs where the decompressed data from the blocks should be copied to.
///
///     == An Example ==
///
///     Suppose we have a SOLID 4KB block composed of:
///     - File 0 (1KB)
///     - File 1 (1KB)
///     - File 2 (1KB)
///     - File 3 (1KB)
///
///     If you request to extract File 1, we only need to decompress
///     the first 2KB of the block, up to the end of File 1. The rest of the
///     data we can skip decompressing. We can then copy the decompressed data
///     from the buffer into the output file directly.
///
///     For Chunked files, the blocks are stored sequentially in the Nx archive,
///     and each block will map to a different slice of the <see cref="IOutputDataProvider"/>.
/// </summary>
internal static class MakeExtractableBlocks
{
    /// <summary>
    ///     Groups blocks for extraction based on the <see cref="FileEntry.FirstBlockIndex" /> of the files.
    /// </summary>
    internal static List<ExtractableBlock> Do(IOutputDataProvider[] outputs, int chunkSize)
    {
        var result = new List<ExtractableBlock>(outputs.Length);
        var blockDict = new Dictionary<int, ExtractableBlock>(outputs.Length);

        for (var x = 0; x < outputs.Length; x++)
        {
            // Slow due to copy to stack, but not that big a deal here.
            var output = outputs[x];
            var entry = output.Entry;
            var chunkCount = entry.GetChunkCount(chunkSize);
            var remainingDecompSize = entry.DecompressedSize;

            for (var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                var blockIndex = entry.FirstBlockIndex + chunkIndex;
                if (!blockDict.TryGetValue(blockIndex, out var block))
                {
                    /*
                        This branch is hit for the first file in a block.
                        Or the first chunk of a chunked file.

                        For chunked files, the `outputs` will contain only the
                        output listed here.

                        For SOLID blocks, the `outputs` will be updated with
                        future files when `blockDict.TryGetValue` is true.
                    */
                    block = new ExtractableBlock
                    {
                        BlockIndex = blockIndex,
                        Outputs = new List<IOutputDataProvider> { output },
                        DecompressSize = entry.DecompressedBlockOffset + (int)Math.Min(remainingDecompSize, (ulong)chunkSize)
                    };

                    blockDict[blockIndex] = block;
                    result.Add(block);
                }
                else
                {
                    var decompSize = entry.DecompressedBlockOffset + (int)Math.Min(remainingDecompSize, (ulong)chunkSize);
                    block.DecompressSize = Math.Max(block.DecompressSize, decompSize);
                    block.Outputs.Add(output);
                }

                remainingDecompSize -= (ulong)chunkSize;
            }
        }

        return result;
    }

    internal class ExtractableBlock
    {
        /// <summary>
        ///     Index of block to decompress.
        /// </summary>
        public required int BlockIndex { get; init; }

        /// <summary>
        ///     Amount of data to decompress in this block.
        ///
        ///     This is equivalent to largest <see cref="FileEntry.DecompressedBlockOffset" /> +
        ///     <see cref="FileEntry.DecompressedSize" /> for a file within the block.
        /// </summary>
        public required int DecompressSize { get; set; }

        /// <summary>
        ///     The files being output to disk.
        /// </summary>
        public required List<IOutputDataProvider> Outputs { get; init; }
    }
}
