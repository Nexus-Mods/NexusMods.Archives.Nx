using System.Diagnostics;
using System.Runtime.CompilerServices;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     This stores reusable logic between implementations of <see cref="ChunkedFileBlock{T}"/>.
///     i.e. <see cref="ChunkedFileBlock{T}"/>. and <see cref="ChunkedFileFromExistingNxBlock{T}"/>
/// </summary>
internal static class ChunkedCommon
{
    internal const int ShortHashLength = 4096;
    internal const int CompressFirstBlockIsDuplicateError = -1;
    internal const int CompressNonFirstBlockIsDuplicated = -2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsLastChunk(int numChunks, int chunkIndex) => chunkIndex == numChunks - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int FirstBlockIndex(int currentBlockIndex, int numChunks) => currentBlockIndex + 1 - numChunks;

    /// <summary>
    ///     Sets the specific index as processed and updates internal state.
    /// </summary>
    /// <param name="compData">The compressed data for block.</param>
    /// <param name="compressedSize">Size of the data after compression.</param>
    /// <param name="tocBuilder">Builds table of contents.</param>
    /// <param name="settings">Packer settings.</param>
    /// <param name="blockIndex">Index of currently packed block.</param>
    /// <param name="asCopy">Whether block was compressed using 'copy' compression.</param>
    /// <param name="compression">The compression algorithm used when processing the block.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteBlock<T>(PackerPoolRental compData, int compressedSize, TableOfContentsBuilder<T> tocBuilder,
        PackerSettings settings, int blockIndex, bool asCopy, CompressionPreference compression)
        where T : IHasRelativePath, IHasFileSize, ICanProvideFileData
    {
        // Write out actual block.
        BlockHelpers.WriteToOutput(settings.Output, compData, compressedSize);
        WriteBlockDetailsToToc(compressedSize, tocBuilder, blockIndex, asCopy ? CompressionPreference.Copy : compression);
    }

    /// <summary>
    ///     Sets the specific index as processed and updates internal state.
    /// </summary>
    /// <param name="compData">The compressed data for block.</param>
    /// <param name="tocBuilder">Builds table of contents.</param>
    /// <param name="settings">Packer settings.</param>
    /// <param name="blockIndex">Index of currently packed block.</param>
    /// <param name="asCopy">Whether block was compressed using 'copy' compression.</param>
    /// <param name="compression">The compression algorithm used when processing the block.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteBlock<T>(Span<byte> compData, TableOfContentsBuilder<T> tocBuilder,
        PackerSettings settings, int blockIndex, bool asCopy, CompressionPreference compression)
        where T : IHasRelativePath, IHasFileSize, ICanProvideFileData
    {
        // Write out actual block.
        BlockHelpers.WriteToOutput(settings.Output, compData);
        WriteBlockDetailsToToc(compData.Length, tocBuilder, blockIndex, asCopy ? CompressionPreference.Copy : compression);
    }

    /// <summary>
    ///     Writes the block details (size, compression) to the table of contents in the
    ///     <paramref name="tocBuilder"/>.
    /// </summary>
    /// <param name="compressedSize">The size of the compressed chunk.</param>
    /// <param name="tocBuilder">The table of contents builder.</param>
    /// <param name="blockIndex">Index of the current block.</param>
    /// <param name="compression">
    ///     Method used on the block.
    ///     This is 'copy' if it is a stub left over a deduplicated block.
    /// </param>
    /// <typeparam name="T"></typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteBlockDetailsToToc<T>(int compressedSize, TableOfContentsBuilder<T> tocBuilder, int blockIndex, CompressionPreference compression)
        where T : IHasRelativePath, IHasFileSize, ICanProvideFileData
    {
        // Update Block Details
        var toc = tocBuilder.Toc;
        ref var blockSize = ref toc.Blocks.DangerousGetReferenceAt(blockIndex);
        blockSize.CompressedSize = compressedSize;

        ref var blockCompression = ref toc.BlockCompressions.DangerousGetReferenceAt(blockIndex);
        blockCompression = compression;
    }

    /// <summary>
    ///     Adds an entry to the table of contents for the current file.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref FileEntry AddFileEntryToTocAtomic<T>(TableOfContentsBuilder<T> tocBuilder, int firstBlockIndex, ulong hash, string relativePath, ulong fileSize)
        where T : IHasRelativePath, IHasFileSize, ICanProvideFileData
    {
        Debug.Assert(hash != 0);
        ref var file = ref tocBuilder.GetAndIncrementFileAtomic();
        file.FilePathIndex = tocBuilder.FileNameToIndexDictionary[relativePath];
        file.FirstBlockIndex = firstBlockIndex;
        file.DecompressedSize = fileSize;
        file.DecompressedBlockOffset = 0;
        file.Hash = hash;
        return ref file;
    }

    /// <summary>
    ///     Disclaimer: I *am* aware this is buggy behaviour if a real file has a
    ///     hash of 0. I am choosing to ignore this due to the probability being
    ///     too unlikely to matter in practice. Deduplication by hash only is by
    ///     nature not perfectly safe.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsHashValid(ulong hash) => hash != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void AddToDeduplicator(ref FileEntry fileEntry, ChunkedDeduplicationState duplState, ulong shortHash)
    {
        Debug.Assert(shortHash != 0);
        Debug.Assert(fileEntry.Hash != 0);
        duplState.AddFileHash(shortHash, fileEntry.Hash, fileEntry.FirstBlockIndex);
    }
}

// ReSharper disable once EnumUnderlyingTypeIsInt
internal enum DeduplicationCheckState : int
{
    Unknown,
    Duplicate,
    NotDuplicate
}
