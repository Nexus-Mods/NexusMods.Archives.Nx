using System.Runtime.CompilerServices;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     Represents an individual block..
/// </summary>
// ReSharper disable once UnusedTypeParameter
internal interface IBlock<T> where T : IHasFileSize, ICanProvideFileData, IHasRelativePath
{
    /// <summary>
    ///     Retrieves largest size of <typeparamref name="T" /> item in bytes.
    /// </summary>
    public ulong LargestItemSize();

    /// <summary>
    ///     True if this kind of block can create/compress chunks.
    /// </summary>
    public bool CanCreateChunks();

    /// <summary>
    ///     Processes this block during the packing operation with the specified settings.
    /// </summary>
    /// <param name="tocBuilder">Used for updating the table of contents..</param>
    /// <param name="settings">Settings used for the buffer.</param>
    /// <param name="blockIndex">Index of the block being processed.</param>
    /// <param name="pools">Used for renting blocks and chunks.</param>
    /// <returns>
    ///     Compressed block data. Whether this should be disposed depends on implementation of <see cref="IBlock{T}" />.
    /// </returns>
    public void ProcessBlock(TableOfContentsBuilder<T> tocBuilder, PackerSettings settings, int blockIndex, PackerArrayPools pools);

    /// <summary>
    ///     Compression method used to pack the block.
    /// </summary>
    // ReSharper disable once UnusedMemberInSuper.Global
    public CompressionPreference Compression { get; }
}

/// <summary>
///     Reused code between different <see cref="IBlock{T}" /> Implementations.
/// </summary>
internal static class BlockHelpers
{
    internal static unsafe int Compress(CompressionPreference compression, int compressionLevel, IFileData data, byte* destinationPtr,
        int destinationLength, out bool asCopy) => Compression.Compress(compression, compressionLevel, data.Data, (int)data.DataLength,
        destinationPtr, destinationLength, out asCopy);

    /// <summary>
    ///     Calls to this method should be wrapped with <see cref="StartProcessingBlock{T}"/> and <see cref="EndProcessingBlock{T}"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteToOutput(Stream output, PackerPoolRental compressedBlock, int numBytes)
    {
        // Copy to output stream and pad.
        output.Write(compressedBlock.Array, 0, numBytes);
        output.SetLength(output.Length.RoundUp4096());
        output.Position = output.Length;
    }

    internal static void StartProcessingBlock<T>(TableOfContentsBuilder<T> builder, int blockIndex) where T : IHasRelativePath, IHasFileSize, ICanProvideFileData
    {
        // Wait until it's our turn to write.
        var spinWait = new SpinWait();
        while (builder.CurrentBlock != blockIndex)
        {
#if NETCOREAPP3_0_OR_GREATER
            spinWait.SpinOnce(-1);
#else
            spinWait.SpinOnce();
#endif
        }
    }

    internal static void EndProcessingBlock<T>(TableOfContentsBuilder<T> builder, IProgress<double>? progress) where T : IHasRelativePath, IHasFileSize, ICanProvideFileData
    {
        // Advance to next block.
        var lastBlock = builder.GetAndIncrementBlockIndexAtomic();

        // Report progress.
        progress?.Report(lastBlock / (float)builder.Toc.Blocks.Length);
    }
}
