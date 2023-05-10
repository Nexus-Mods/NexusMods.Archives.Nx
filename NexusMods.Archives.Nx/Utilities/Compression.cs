using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using NexusMods.Archives.Nx.Enums;
using SharpZstd.Interop;
using static SharpZstd.Interop.Zstd;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Utility methods for performing compression and decompression.
/// </summary>
internal static class Compression
{
    /// <summary>
    ///     Determines maximum memory needed to alloc to compress data with any method.
    /// </summary>
    /// <param name="sourceLength">Number of bytes at source.</param>
    public static int MaxAllocForCompressSize(int sourceLength)
    {
        var lz4 = AllocForCompressSize(CompressionPreference.Lz4, sourceLength);
        var zstd = AllocForCompressSize(CompressionPreference.ZStandard, sourceLength);
        return Math.Max(lz4, zstd);
    }

    /// <summary>
    ///     Determines memory needed to alloc to compress data with a specified method.
    /// </summary>
    /// <param name="method">Method we compress with.</param>
    /// <param name="sourceLength">Number of bytes at source.</param>
    public static int AllocForCompressSize(CompressionPreference method, int sourceLength)
    {
        switch (method)
        {
            case CompressionPreference.Copy:
                return sourceLength;
            case CompressionPreference.ZStandard:
                return (int)ZSTD_COMPRESSBOUND((nuint)sourceLength);
            case CompressionPreference.Lz4:
                return LZ4Codec.MaximumOutputSize(sourceLength);
            default:
                ThrowHelpers.ThrowUnsupportedCompressionMethod(method);
                return default;
        }
    }

    /// <summary>
    ///     Compresses data with a specific method.
    /// </summary>
    /// <param name="method">Method we compress with.</param>
    /// <param name="level">Level at which we are compressing.</param>
    /// <param name="source">Length of the source in bytes.</param>
    /// <param name="sourceLength">Number of bytes at source.</param>
    /// <param name="destination">Pointer to destination.</param>
    /// <param name="destinationLength">Length of bytes at destination.</param>
    /// <param name="defaultedToCopy">If this is true, data was uncompressable and default compression was used instead.</param>
    public static unsafe int Compress(CompressionPreference method, int level, byte* source, int sourceLength, byte* destination,
        int destinationLength, out bool defaultedToCopy)
    {
        defaultedToCopy = false;
        switch (method)
        {
            case CompressionPreference.Copy:
                defaultedToCopy = true;
                Unsafe.CopyBlockUnaligned(destination, source, (uint)sourceLength);
                return sourceLength;
            case CompressionPreference.ZStandard:
            {
                var bytes = (int)ZSTD_compress(destination, (nuint)destinationLength, source, (nuint)sourceLength, level);
                if (bytes > sourceLength) // default to 
                    goto case CompressionPreference.Copy;

                return bytes;
            }
            case CompressionPreference.Lz4:
            {
                var bytes = LZ4Codec.Encode(source, sourceLength, destination, destinationLength, (LZ4Level)level);
                if (bytes > sourceLength)
                    goto case CompressionPreference.Copy;

                return bytes;
            }

            default:
                ThrowHelpers.ThrowUnsupportedCompressionMethod(method);
                return 0;
        }
    }

    /// <summary>
    ///     Decompresses data with a specific method.
    /// </summary>
    /// <param name="method">Method we compress with.</param>
    /// <param name="source">Length of the source (compressed) in bytes.</param>
    /// <param name="sourceLength">Number of bytes at source.</param>
    /// <param name="destination">Pointer to destination (decompressed).</param>
    /// <param name="destinationLength">Length of bytes at destination.</param>
    public static unsafe void DecompressPartial(CompressionPreference method, byte* source, int sourceLength, byte* destination,
        int destinationLength)
    {
        switch (method)
        {
            case CompressionPreference.Copy:
                Unsafe.CopyBlockUnaligned(destination, source, (uint)sourceLength);
                return;
            case CompressionPreference.ZStandard:
            {
                // Initialize output buffer
                nuint result = 0;
                var dStream = ZSTD_createDStream();
                var outBuf = new ZSTD_outBuffer()
                {
                    dst = destination,
                    pos = 0,
                    size = (nuint)destinationLength
                };
                
                var inBuf = new ZSTD_inBuffer()
                {
                    src = source,
                    pos = 0,
                    size = (nuint)sourceLength
                };

                do
                {
                    result = ZSTD_decompressStream(dStream, &outBuf, &inBuf);
                    var error = ZSTD_isError(result);
                    if (error > 0)
                    {
                        var namePtr = (nint)ZSTD_getErrorName(result);
                        var str = Marshal.PtrToStringAnsi(namePtr);
                        ZSTD_freeDStream(dStream);
                        throw new InvalidOperationException($"ZStd Decompression error: {str}");
                    }
                }
                while (result != 0 || outBuf.pos < (nuint)destinationLength);
                ZSTD_freeDStream(dStream);
                return;
            }
            case CompressionPreference.Lz4:
            {
                // Fastest API with minimal alloc.
                LZ4Codec.Decode(source, sourceLength, destination, destinationLength);
                return;
            }

            default:
                ThrowHelpers.ThrowUnsupportedCompressionMethod(method);
                return;
        }
    }

    /// <summary>
    ///     Compresses a given slice of data using ZStandard.
    /// </summary>
    /// <param name="input">The data to compress.</param>
    /// <param name="length">Length of data at 'input'.</param>
    /// <param name="level">Level at which to compress at.</param>
    /// <returns>The compressed data. Make sure to dispose it!</returns>
    public static unsafe ArrayRentalSlice CompressZStd(byte* input, int length, int level = 16)
    {
        var result = new ArrayRental((int)ZSTD_compressBound((nuint)length));
        fixed (byte* resultPtr = result.Span)
        {
            var numCompressed = (int)ZSTD_compress(resultPtr, (nuint)result.Array.Length, input, (nuint)length, level);
            return new ArrayRentalSlice(result, numCompressed);
        }
    }

    /// <summary>
    ///     Compresses a given slice of data using ZStandard.
    /// </summary>
    /// <param name="input">The data to compress.</param>
    /// <param name="level">Level at which to compress at.</param>
    /// <returns>The compressed data. Make sure to dispose it!</returns>
    public static unsafe ArrayRentalSlice CompressZStd(Span<byte> input, int level = 16)
    {
        fixed (byte* inputPtr = input)
        {
            return CompressZStd(inputPtr, input.Length, level);
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
            return DecompressZStd(compressedPtr, input.Length, (int)ZSTD_findDecompressedSize(compressedPtr, (UIntPtr)input.Length));
        }
    }

    /// <summary>
    ///     Decompresses the given data using ZStandard.
    /// </summary>
    /// <param name="compressedDataPtr">Pointer to compressed data.</param>
    /// <param name="compressedSize">Size of compressed data at <paramref name="compressedDataPtr"/>.</param>
    /// <returns>The decompressed data. Make sure to dispose it!</returns>
    public static unsafe ArrayRentalSlice DecompressZStd(byte* compressedDataPtr, int compressedSize)
    {
        return DecompressZStd(compressedDataPtr, compressedSize, (int)ZSTD_findDecompressedSize(compressedDataPtr, (UIntPtr)compressedSize));
    }
    
    /// <summary>
    ///     Decompresses the given data using ZStandard.
    /// </summary>
    /// <param name="compressedDataPtr">Pointer to compressed data.</param>
    /// <param name="compressedSize">Size of compressed data at <paramref name="compressedDataPtr"/>.</param>
    /// <param name="decompressedSize">Known ahead of time size for decompressed data.</param>
    /// <returns>The decompressed data. Make sure to dispose it!</returns>
    public static unsafe ArrayRentalSlice DecompressZStd(byte* compressedDataPtr, int compressedSize, int decompressedSize)
    {
        var result = new ArrayRental(decompressedSize);
        var resultSpan = result.Span;
        fixed (byte* resultPtr = resultSpan)
        {
            var decompressed = (int)ZSTD_decompress(resultPtr, (UIntPtr)resultSpan.Length, compressedDataPtr, (UIntPtr)compressedSize);
            return new ArrayRentalSlice(result, decompressed);
        }
    }
}
