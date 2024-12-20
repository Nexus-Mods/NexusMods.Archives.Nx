using System.Runtime.CompilerServices;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Headers.Managed;

/// <summary>
///     This contains the parsed header data needed for file extraction.
/// </summary>
public class ParsedHeader : TableOfContents
{
    /// <summary>
    ///     Native file header.
    /// </summary>
    public NativeFileHeader Header;

    /// <summary>
    ///     Stores the raw offsets of the compressed blocks.
    /// </summary>
    public ulong[] BlockOffsets = null!;

    /// <summary>
    ///     Initializes this header. This must be called manually.
    /// </summary>
    public void Init()
    {
        var currentOffset = (ulong)Header.HeaderPageBytes;
        var numBlocks = Blocks.Length;
        BlockOffsets = GC.AllocateUninitializedArray<ulong>(numBlocks, false);
        if (numBlocks <= 0)
            return;

        ref var blockOffsetsRef = ref BlockOffsets[0];
        blockOffsetsRef = (nuint)Header.HeaderPageBytes; // pre-init first one.
        ref var blocksRef = ref Blocks[0];

        // Manually unrolled to speed up header parse because the JIT can't.
        var unrolledBlocks = numBlocks - numBlocks % 4;
        int x;
        for (x = 0; x < unrolledBlocks; x += 4)
        {
            currentOffset += (ulong)Unsafe.Add(ref blocksRef, x).CompressedSize;
            currentOffset = currentOffset.RoundUp4096();
            Unsafe.Add(ref blockOffsetsRef, x + 1) = currentOffset;

            currentOffset += (ulong)Unsafe.Add(ref blocksRef, x + 1).CompressedSize;
            currentOffset = currentOffset.RoundUp4096();
            Unsafe.Add(ref blockOffsetsRef, x + 2) = currentOffset;

            currentOffset += (ulong)Unsafe.Add(ref blocksRef, x + 2).CompressedSize;
            currentOffset = currentOffset.RoundUp4096();
            Unsafe.Add(ref blockOffsetsRef, x + 3) = currentOffset;

            currentOffset += (ulong)Unsafe.Add(ref blocksRef, x + 3).CompressedSize;
            currentOffset = currentOffset.RoundUp4096();
            if (x + 4 < numBlocks)
                Unsafe.Add(ref blockOffsetsRef, x + 4) = currentOffset;
        }

        // Process the remaining elements
        for (; x < numBlocks; x++)
        {
            currentOffset += (nuint)Unsafe.Add(ref blocksRef, x).CompressedSize;
            currentOffset = currentOffset.RoundUp4096();
            if (x < numBlocks - 1)
                Unsafe.Add(ref blockOffsetsRef, x + 1) = currentOffset;
        }
    }
}
