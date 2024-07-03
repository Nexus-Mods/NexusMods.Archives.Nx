using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if !NET7_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
#if NET7_0_OR_GREATER
using System.Numerics;
using System.Runtime.InteropServices;
#endif
#if NETSTANDARD2_0
using System.Buffers;
#endif

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Method wrappers for newer runtime features that delegate back to older approaches on unsupported runtimes.
/// </summary>
[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
internal static class Polyfills
{
    /// <summary>
    ///     Allocates an array without zero filling it.
    /// </summary>
    /// <returns></returns>
    public static T[] AllocateUninitializedArray<T>(int size, bool pinned = false)
    {
#if NET5_0_OR_GREATER
        return GC.AllocateUninitializedArray<T>(size, pinned);
#else
        return new T[size];
#endif
    }

    /// <summary>
    ///     Allocates an array in the pinned object heap (if possible).
    /// </summary>
    public static T[] AllocatePinnedArray<T>(int size)
    {
#if NET5_0_OR_GREATER
        var result = GC.AllocateUninitializedArray<T>(size, true);
        Array.Fill(result, default);
        return result;
#else
        return new T[size];
#endif
    }

    /// <summary>
    ///     Rounds up the number to next power of 2. Does not overflow.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RoundUpToPowerOf2NoOverflow(int value)
    {
        if (value == int.MaxValue)
            return value;

        if (value > int.MaxValue >> 1)
            return int.MaxValue;

#if NET7_0_OR_GREATER
        return (int)BitOperations.RoundUpToPowerOf2((uint)value);
#else
        // Based on https://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
        --value;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
#endif
    }

    /// <summary>
    ///     Clamps the value between a lower and upper bound.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(int value, int min, int max)
    {
        if (value < min)
            return min;

        if (value > max)
            return max;

        return value;
    }

    /// <summary>
    ///     Reads at least a minimum number of bytes from the current stream and advances the position within the stream by the
    ///     number of bytes read.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="buffer">
    ///     A region of memory. When this method returns, the contents of this region are replaced by the
    ///     bytes read from the current stream.
    /// </param>
    /// <param name="minimumBytes">The minimum number of bytes to read into the buffer.</param>
    /// <param name="throwOnEndOfStream">
    ///     <see langword="true" /> to throw an exception if the end of the stream is reached before reading
    ///     <paramref name="minimumBytes" /> of bytes;
    ///     <see langword="false" /> to return less than <paramref name="minimumBytes" /> when the end of the stream is
    ///     reached.
    ///     The default is <see langword="true" />.
    /// </param>
    /// <returns>
    ///     The total number of bytes read into the buffer. This is guaranteed to be greater than or equal to
    ///     <paramref name="minimumBytes" />
    ///     when <paramref name="throwOnEndOfStream" /> is <see langword="true" />. This will be less than
    ///     <paramref name="minimumBytes" /> when the
    ///     end of the stream is reached and <paramref name="throwOnEndOfStream" /> is <see langword="false" />. This can be
    ///     less than the number
    ///     of bytes allocated in the buffer if that many bytes are not currently available.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="minimumBytes" /> is negative, or is greater than the length of <paramref name="buffer" />.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    ///     <paramref name="minimumBytes" /> bytes of data.
    /// </exception>
    /// <remarks>
    ///     When <paramref name="minimumBytes" /> is 0 (zero), this read operation will be completed without waiting for
    ///     available data in the stream.
    /// </remarks>
    public static int ReadAtLeast(Stream stream, byte[] buffer, int minimumBytes, bool throwOnEndOfStream = true)
    {
#if NET7_0_OR_GREATER
        return stream.ReadAtLeast(buffer.AsSpan(0, minimumBytes), minimumBytes, throwOnEndOfStream);
#else
        // Taken from Runtime.
        var totalRead = 0;
        while (totalRead < minimumBytes)
        {
            var read = stream.Read(buffer, totalRead, minimumBytes - totalRead);
            if (read == 0)
            {
                if (throwOnEndOfStream)
                    ThrowHelpers.ThrowEndOfFileException();

                return totalRead;
            }

            totalRead += read;
        }

        return totalRead;
#endif
    }

    /// <summary>
    ///     Checks if the current platform is Windows.
    ///     On modern runtimes, this is trimmer friendly, and evaluates to a constant.
    ///     On older runtimes, this is dynamically checked.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWindows()
    {
#if NET5_0_OR_GREATER
        return OperatingSystem.IsWindows();
#else
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSpan(this Stream stream, ReadOnlySpan<byte> buffer)
    {
#if NETSTANDARD2_0
        var numArray = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            buffer.CopyTo((Span<byte>) numArray);
            stream.Write(numArray, 0, buffer.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(numArray);
        }
#else
        stream.Write(buffer);
#endif
    }

    /// <summary>
    ///     Allocates a block of native memory with the specified size.
    /// </summary>
    /// <param name="size">Size of the memory to allocate.</param>
    /// <returns>The block of memory.</returns>
    public static unsafe void* AllocNativeMemory(nuint size)
    {
#if NET7_0_OR_GREATER
        return NativeMemory.Alloc(size);
#else
        return (void*)Marshal.AllocHGlobal((nint)size);
#endif
    }

    /// <summary>
    ///     Frees a block of memory at the specified address.
    /// </summary>
    /// <param name="addr">Address of the native allocation.</param>
    /// <returns>The block of memory.</returns>
    public static unsafe void FreeNativeMemory(void* addr)
    {
#if NET7_0_OR_GREATER
        NativeMemory.Free(addr);
#else
        Marshal.FreeHGlobal((IntPtr)addr);
#endif
    }
}
