using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using K4os.Compression.LZ4;
using NexusMods.Archives.Nx.Enums;
using SharpZstd.Interop;
using static SharpZstd.Interop.Zstd;

// ReSharper disable MemberCanBePrivate.Global

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Utility methods for performing compression and decompression.
/// </summary>
public static class Compression
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
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (method)
        {
            case CompressionPreference.Copy:
                return sourceLength;
            case CompressionPreference.ZStandard:
                return (int)ZSTD_COMPRESSBOUND((nuint)sourceLength);
            case CompressionPreference.Lz4:
                return LZ4Codec.MaximumOutputSize(sourceLength);
            case CompressionPreference.NoPreference:
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
    /// <param name="defaultedToCopy">If this is true, data was uncompressible and default compression was used instead.</param>
    public static unsafe int Compress(CompressionPreference method, int level, byte* source, int sourceLength, byte* destination,
        int destinationLength, out bool defaultedToCopy)
    {
        defaultedToCopy = false;
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (method)
        {
            case CompressionPreference.Copy:
                defaultedToCopy = true;
                Unsafe.CopyBlockUnaligned(destination, source, (uint)sourceLength);
                return sourceLength;
            case CompressionPreference.ZStandard:
            {
                var result = ZSTD_compress(destination, (nuint)destinationLength, source, (nuint)sourceLength, level);
                // -70 means destination buffer is too small, in other words, we failed to compress this data.
                if ((int)result > sourceLength || (int)result == -70)
                    goto case CompressionPreference.Copy;

                var error = ZSTD_isError(result);
                if (error <= 0)
                    return (int)result;

                var namePtr = (nint)ZSTD_getErrorName(result);
                var str = Marshal.PtrToStringAnsi(namePtr);
                throw new InvalidOperationException($"ZStd Compression error: {str}");
            }
            case CompressionPreference.Lz4:
            {
                var bytes = LZ4Codec.Encode(source, sourceLength, destination, destinationLength, (LZ4Level)level);
                if (bytes > sourceLength || bytes < 0) // 'negative value if buffer is too small'
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
    public static unsafe void Decompress(CompressionPreference method, byte* source, int sourceLength, byte* destination,
        int destinationLength)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (method)
        {
            case CompressionPreference.Copy:
                Debug.Assert(sourceLength >= destinationLength);
                Buffer.MemoryCopy(source, destination, (uint)destinationLength, (uint)destinationLength);
                return;
            case CompressionPreference.ZStandard:
            {
                // Initialize output buffer
                nuint result;
                var dStream = ZSTD_createDStream();
                var outBuf = new ZSTD_outBuffer
                {
                    dst = destination,
                    pos = 0,
                    size = (nuint)destinationLength
                };

                var inBuf = new ZSTD_inBuffer
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

                    // Not decompressed everything.
                    if (outBuf.pos != outBuf.size)
                        continue;

                    // To quote the docs:
                    // But if `output.pos == output.size`, there might be some data left within internal buffers.,
                    // In which case, call ZSTD_decompressStream() again to flush whatever remains in the buffer.
                    ZSTD_decompressStream(dStream, &outBuf, &inBuf);
                    break;
                } while (outBuf.pos < (nuint)destinationLength);

                ZSTD_freeDStream(dStream);
                return;
            }
            case CompressionPreference.Lz4:
            {
                // Fastest API with minimal alloc.
                var result = LZ4Codec.PartialDecode(source, sourceLength, destination, destinationLength);
                if (result < 0)
                    throw new InvalidOperationException($"LZ4 Decompression error: {result}");

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
    internal static unsafe ArrayRentalSlice CompressZStd(byte* input, int length, int level = 16)
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
    internal static unsafe ArrayRentalSlice CompressZStd(Span<byte> input, int level = 16)
    {
        fixed (byte* inputPtr = input)
        {
            return CompressZStd(inputPtr, input.Length, level);
        }
    }

    /// <summary>
    ///     Decompresses the given data using ZStandard.
    /// </summary>
    /// <param name="compressedDataPtr">Pointer to compressed data.</param>
    /// <param name="compressedSize">Size of compressed data at <paramref name="compressedDataPtr" />.</param>
    /// <returns>The decompressed data. Make sure to dispose it!</returns>
    internal static unsafe ArrayRentalSlice DecompressZStd(byte* compressedDataPtr, int compressedSize) => DecompressZStd(compressedDataPtr,
        compressedSize, (int)ZSTD_findDecompressedSize(compressedDataPtr, (nuint)compressedSize));

    /// <summary>
    ///     Decompresses the given data using ZStandard.
    /// </summary>
    /// <param name="compressedDataPtr">Pointer to compressed data.</param>
    /// <param name="compressedSize">Size of compressed data at <paramref name="compressedDataPtr" />.</param>
    /// <param name="decompressedSize">Known ahead of time size for decompressed data.</param>
    /// <returns>The decompressed data. Make sure to dispose it!</returns>
    internal static unsafe ArrayRentalSlice DecompressZStd(byte* compressedDataPtr, int compressedSize, int decompressedSize)
    {
        var result = new ArrayRental(decompressedSize);
        var resultSpan = result.Span;
        fixed (byte* resultPtr = resultSpan)
        {
            var decompressed = (int)ZSTD_decompress(resultPtr, (nuint)resultSpan.Length, compressedDataPtr, (nuint)compressedSize);
            return new ArrayRentalSlice(result, decompressed);
        }
    }
}
