using System.Text;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Headers;

/// <summary>
///     Provides an abstraction over the string pool used in the application.
/// </summary>
[PublicAPI]
public struct StringPool
{
    /// <summary>
    ///     Default compression level for table of contents. [Currently non-customizable]
    /// </summary>
    private const int DefaultCompressionLevel = 16;

    /// <summary>
    ///     Packs a stringpool given the file paths provided.
    /// </summary>
    /// <param name="items">
    ///     The items to be packed.
    ///     These will be sorted in-place according to their packing order.
    /// </param>
    /// <typeparam name="T">Some type which has file names.</typeparam>
    /// <returns>Packed bytes. Make sure to dispose them!</returns>
    /// <exception cref="InsufficientStringPoolSizeException">Size of string pool, exceeds maximum allowed.</exception>
    public static unsafe ArrayRentalSlice Pack<T>(Span<T> items) where T : IHasRelativePath
    {
        // Sort-in-place.
        items.SortLexicographically();

        // Pack Items
        var totalPathSize = 0;
        foreach (var item in items)
            totalPathSize += item.RelativePath.Length;

        // Null terminators.
        totalPathSize += items.Length;

        // Chances are the paths are entirely ANSI.
        // If they are not, a realloc might happen, slowing things down.
        // Note: The pool uses powers of 2, so for minimal non-ANSI characters, we will actually have a bit of extra space.
        using var poolBuf = new GrowableMemoryPool(totalPathSize, true);

        var availableSize = poolBuf.AvailableSize;
        var ptr = poolBuf.Pointer;
        var numLeft = availableSize;

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var x = 0; x < items.Length; x++)
        {
            var item = items[x].RelativePath;
            fixed (char* pathPtr = item)
            {
            tryAgain:
                try
                {
                    // We do +1 and -1 to add zero terminator.
                    var written = Encoding.UTF8.GetBytes(pathPtr, item.Length, ptr, numLeft - 1);
                    written += 1;
                    numLeft -= written;
                    ptr += written;
                }
                catch (ArgumentException)
                {
                    // Insufficient space, let's grow!
                    var numWritten = poolBuf.AvailableSize - numLeft;
                    poolBuf.Grow(true);

                    availableSize = poolBuf.AvailableSize;
                    ptr = poolBuf.Pointer + numWritten;
                    numLeft = availableSize - numWritten;

                    goto tryAgain;
                }
            }
        }

        var numBytes = poolBuf.AvailableSize - numLeft;
        // ReSharper disable once RedundantArgumentDefaultValue
        var result = Compression.CompressZStd(poolBuf.PinnedArray.AsSpan(0, numBytes), DefaultCompressionLevel);
        if (result.Length <= NativeTocHeader.MaxStringPoolSize)
            return result;

        result.Dispose();
        ThrowHelpers.ThrowInsufficientStringPoolSizeException(result.Length);
        return result;
    }

    /// <summary>
    ///     Unpacks strings from a given pool.
    /// </summary>
    /// <param name="poolPtr">The compressed stringpool.</param>
    /// <param name="compressedDataSize">Size of compressed data at the address.</param>
    /// <returns>The strings in the pool.</returns>
    /// <remarks>
    ///     The number of expected strings in the pool is obtained from
    /// </remarks>
    public static unsafe string[] Unpack(byte* poolPtr, int compressedDataSize) => Unpack(poolPtr, compressedDataSize, 0);

    /// <summary>
    ///     Unpacks strings from a given pool.
    /// </summary>
    /// <param name="poolPtr">The compressed stringpool.</param>
    /// <param name="compressedDataSize">Size of compressed data at the address.</param>
    /// <param name="fileCountHint">Hint for file count.</param>
    /// <returns>The strings in the pool.</returns>
    /// <remarks>
    ///     The number of expected strings in the pool is obtained from
    /// </remarks>
    public static unsafe string[] Unpack(byte* poolPtr, int compressedDataSize, int fileCountHint)
    {
        // Okay time to deconstruct the pool.
        using var decompressed = Compression.DecompressZStd(poolPtr, compressedDataSize);
        var decompressedSpan = decompressed.Span;
        var offsets = decompressedSpan.Length > 0 ? decompressedSpan.FindAllOffsetsOfByte(0, fileCountHint) : new List<int>();
        var items = Polyfills.AllocateUninitializedArray<string>(offsets.Count);

        var currentOffset = 0;
        fixed (byte* spanPtr = decompressedSpan)
        {
            for (var x = 0; x < items.Length; x++)
            {
                var offset = offsets[x];
                var length = offset - currentOffset;
                items[x] = Encoding.UTF8.GetString(spanPtr + currentOffset, length);
                currentOffset = offset + 1;
            }
        }

        return items;
    }

    /// <summary>
    ///     Unpacks strings from a given pool.
    /// </summary>
    /// <param name="poolSpan">The compressed stringpool.</param>
    /// <returns>The strings in the pool.</returns>
    /// <remarks>
    ///     The number of expected strings in the pool is obtained from
    /// </remarks>
    public static unsafe string[] Unpack(Span<byte> poolSpan)
    {
        fixed (byte* poolSpanPtr = poolSpan)
            return Unpack(poolSpanPtr, poolSpan.Length, 0);
    }

    /// <summary>
    ///     Unpacks strings from a given pool.
    /// </summary>
    /// <param name="poolSpan">The compressed stringpool.</param>
    /// <param name="fileCountHint">Hint for file count.</param>
    /// <returns>The strings in the pool.</returns>
    /// <remarks>
    ///     The number of expected strings in the pool is obtained from
    /// </remarks>
    public static unsafe string[] Unpack(Span<byte> poolSpan, int fileCountHint)
    {
        fixed (byte* poolSpanPtr = poolSpan)
            return Unpack(poolSpanPtr, poolSpan.Length, fileCountHint);
    }
}

internal static class StringPoolExtensions
{
    public static void SortLexicographically<T>(this Span<T> items) where T : IHasRelativePath
    {
        items.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.Ordinal));
    }
}
