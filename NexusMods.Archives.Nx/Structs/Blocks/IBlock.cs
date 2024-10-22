using System.Runtime.CompilerServices;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     Represents an individual block.
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
    ///     Returns the number of files in this block.
    /// </summary>
    public int FileCount();

    /// <summary>
    ///     Unsafely appends relative file paths in Nx to the current array of paths.
    ///     Ignores bound checks.
    /// </summary>
    /// <param name="currentIndex">Current index to insert into.</param>
    /// <param name="paths">The array to insert paths into.</param>
    public void AppendFilesUnsafe(ref int currentIndex, HasRelativePathWrapper[] paths);

    /// <summary>
    ///     Processes this block during the packing operation with the specified settings.
    /// </summary>
    /// <param name="tocBuilder">Used for updating the table of contents.</param>
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
    private static readonly ThreadLocal<bool> CurThreadIsWaitingForTurn = new(() => false);

    internal static unsafe int Compress(CompressionPreference compression, int compressionLevel, IFileData data, byte* destinationPtr,
        int destinationLength, out bool asCopy) => Compression.Compress(compression, compressionLevel, data.Data, (int)data.DataLength,
        destinationPtr, destinationLength, out asCopy);

    internal static unsafe int CompressStreamed(CompressionPreference compression, int compressionLevel, IFileData data, byte* destinationPtr,
        int destinationLength, Func<int> terminateEarly, out bool asCopy) => Compression.CompressStreamed(compression, compressionLevel, data.Data, (int)data.DataLength,
        destinationPtr, destinationLength, terminateEarly, out asCopy);

    /// <summary>
    ///     Calls to this method should be wrapped with <see cref="WaitForBlockTurn{T}"/> and <see cref="EndProcessingBlock{T}"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteToOutput(Stream output, PackerPoolRental compressedBlock, int numBytes)
    {
        // Note: On NS 2.0, span is not natively supported. So this can't be deduped with overload.
        output.Write(compressedBlock.Array, 0, numBytes);
        AddPaddingAfterBlockWrite(output);
    }

    /// <summary>
    ///     Calls to this method should be wrapped with <see cref="WaitForBlockTurn{T}"/> and <see cref="EndProcessingBlock{T}"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteToOutput(Stream output, Span<byte> compressedBlock)
    {
        output.Write(compressedBlock);
        AddPaddingAfterBlockWrite(output);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddPaddingAfterBlockWrite(Stream output)
    {
        output.SetLength(output.Length.RoundUp4096());
        output.Position = output.Length;
    }

    /// <summary>
    ///     Locks the output stream to which the raw compressed block data is being
    ///     written to. The lock happens on the <paramref name="builder"/>
    ///     with the currently processed block index being used.
    ///
    ///     Call <see cref="EndProcessingBlock{T}"/> when done.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WaitForBlockTurn<T>(TableOfContentsBuilder<T> builder, int blockIndex) where T : IHasRelativePath, IHasFileSize, ICanProvideFileData
    {
#if DEBUG
        if (CurThreadIsWaitingForTurn.Value)
            throw new InvalidOperationException("WaitForBlockTurn called without a corresponding EndProcessingBlock");

        CurThreadIsWaitingForTurn.Value = true;
#endif

        // Wait until it's our turn to write.
        var spinWait = new SpinWait();
        while (builder.CurrentBlock != blockIndex)
        {
            spinWait.SpinOnce(-1);
        }
    }

    /// <summary>
    ///     Warning: Calling this from multiple threads in parallel is not legal.
    ///     It will cause a deadlock in <see cref="WaitForBlockTurn{T}"/>
    ///     as individual threads' blockindex can be skipped.
    /// </summary>
    internal static void EndProcessingBlock<T>(TableOfContentsBuilder<T> builder, IProgress<double>? progress) where T : IHasRelativePath, IHasFileSize, ICanProvideFileData
    {
#if DEBUG
        try
        {
            if (CurThreadIsWaitingForTurn.Value == false)
                throw new InvalidOperationException("EndProcessingBlock called without a corresponding WaitForBlockTurn");
#endif
            // Advance to next block.
            var lastBlock = builder.GetAndIncrementBlockIndexAtomic();

            // Report progress.
            progress?.Report(lastBlock / (float)builder.Toc.Blocks.Length);
#if DEBUG
        }
        finally
        {
            CurThreadIsWaitingForTurn.Value = false;
        }
#endif
    }
}
