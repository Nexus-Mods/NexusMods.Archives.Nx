using System.Diagnostics;
using System.Text;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;
using static SharpZstd.Interop.Zstd;

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
    ///     Default compression level. [Currently non-customizable]
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
    /// <returns>Packed bytes.</returns>
    /// <exception cref="InsufficientStringPoolSizeException"></exception>
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

        return Compress(poolBuf.PinnedArray.AsSpan());
    }

    private static unsafe ArrayRentalSlice Compress(Span<byte> input)
    {
        var result = new ArrayRental((int)ZSTD_compressBound((nuint)input.Length));
        fixed (byte* inputPtr = input)
        fixed (byte* resultPtr = result.Span)
        {
            var numCompressed = (int)ZSTD_compress(resultPtr, (nuint)result.Array.Length, inputPtr, (nuint)input.Length,
                DefaultCompressionLevel);
            return new ArrayRentalSlice(result, numCompressed);
        }
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
