using System.Text;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.TOC;

/// <summary>
///     Provides an abstraction over the string pool used in the application.
/// </summary>
public struct StringPool
{
    /// <summary>
    ///     Used directory separator.
    /// </summary>
    public const char Separator = '/';

    /// <summary>
    ///     Default compression level for table of contents. [Currently non-customizable]
    /// </summary>
    public const int DefaultCompressionLevel = 16;

    /// <summary>
    ///     Maximum size of uncompressed stringpool allowed.
    /// </summary>
    public const int MaxUncompressedSize = 268435456;

    /// <summary>
    ///     Packs a stringpool given the file paths provided.
    /// </summary>
    /// <param name="items">The items to be packed.</param>
    /// <typeparam name="T">Some type which has file names.</typeparam>
    /// <returns>Packed bytes. Make sure to dispose them!</returns>
    /// <exception cref="InsufficientStringPoolSizeException">Size of string pool, exceeds maximum allowed.</exception>
    public static unsafe ArrayRentalSlice Pack<T>(Span<T> items) where T : IHasFilePath
    {
        // Sort-in-place.
        items.SortLexicographically();

        // Pack Items
        var totalPathSize = 0;
        foreach (var item in items)
            totalPathSize += item.RelativePath.Length;

        // Null terminators.
        totalPathSize += items.Length;

        if (totalPathSize > MaxUncompressedSize)
            ThrowHelpers.ThrowInsufficientStringPoolSizeException(totalPathSize);

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

                    // Assert within size.
                    if (poolBuf.AvailableSize - numLeft > MaxUncompressedSize)
                        ThrowHelpers.ThrowInsufficientStringPoolSizeException();
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
        return Compression.CompressZStd(poolBuf.PinnedArray.AsSpan(0, numBytes));
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
    public static unsafe string[] Unpack(Span<byte> poolSpan, int fileCountHint = 0)
    {
        // Okay time to deconstruct the pool.
        using var decompressed = Compression.DecompressZStd(poolSpan);
        var decompressedSpan = decompressed.Span;
        var offsets = decompressedSpan.FindAllOffsetsOfByte(0, fileCountHint);
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
}

internal static class StringPoolExtensions
{
    public static void SortLexicographically<T>(this Span<T> items) where T : IHasFilePath
    {
#if NET5_0_OR_GREATER
        items.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.Ordinal));
#else
        // No way to sort a span on older frameworks; this is going to suck, but I guess we have to.
        var copy = items.ToArray();
        Array.Sort(copy, (a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.Ordinal));
        copy.CopyTo(items);
#endif
    }
}
