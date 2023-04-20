using static SharpZstd.Interop.Zstd;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Utility methods for performing compression and decompression.
/// </summary>
internal static class Compression
{
    /// <summary>
    ///     Compresses a given slice of data using ZStandard.
    /// </summary>
    /// <param name="input">The data to compress.</param>
    /// <param name="level">Level at which to compress at.</param>
    /// <returns>The compressed data. Make sure to dispose it!</returns>
    public static unsafe ArrayRentalSlice CompressZStd(Span<byte> input, int level = 16)
    {
        var result = new ArrayRental((int)ZSTD_compressBound((nuint)input.Length));
        fixed (byte* inputPtr = input)
        fixed (byte* resultPtr = result.Span)
        {
            var numCompressed = (int)ZSTD_compress(resultPtr, (nuint)result.Array.Length, inputPtr, (nuint)input.Length,
                level);
            return new ArrayRentalSlice(result, numCompressed);
        }
    }

    /// <summary>
    ///     Decompresses the given data using ZStandard.
    /// </summary>
    /// <param name="input">The data to decompress.</param>
    /// <returns>The decompressed data. Make sure to dispose it!</returns>
    public static unsafe ArrayRentalSlice DecompressZStd(Span<byte> input)
    {
        fixed (byte* compressedPtr = input)
        {
            var decompSize = ZSTD_findDecompressedSize(compressedPtr, (UIntPtr)input.Length);
            var result = new ArrayRental((int)decompSize);
            var resultSpan = result.Span;
            fixed (byte* resultPtr = resultSpan)
            {
                var decompressed = (int)ZSTD_decompress(resultPtr, (UIntPtr)resultSpan.Length, compressedPtr,
                    (UIntPtr)input.Length);
                
                return new ArrayRentalSlice(result, decompressed);
            }
        }
    }
}
