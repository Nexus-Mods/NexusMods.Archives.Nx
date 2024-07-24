using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.InteropServices;

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
        return GC.AllocateUninitializedArray<T>(size, pinned);
    }

    /// <summary>
    ///     Allocates an array in the pinned object heap (if possible).
    /// </summary>
    public static T[] AllocatePinnedArray<T>(int size)
    {
        var result = GC.AllocateUninitializedArray<T>(size, true);
        Array.Fill(result, default);
        return result;
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

        return (int)BitOperations.RoundUpToPowerOf2((uint)value);
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
        return stream.ReadAtLeast(buffer.AsSpan(0, minimumBytes), minimumBytes, throwOnEndOfStream);
    }

    /// <summary>
    ///     Checks if the current platform is Windows.
    ///     On modern runtimes, this is trimmer friendly, and evaluates to a constant.
    ///     On older runtimes, this is dynamically checked.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWindows()
    {
        return OperatingSystem.IsWindows();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSpan(this Stream stream, ReadOnlySpan<byte> buffer)
    {
        stream.Write(buffer);
    }

    /// <summary>
    ///     Allocates a block of native memory with the specified size.
    /// </summary>
    /// <param name="size">Size of the memory to allocate.</param>
    /// <returns>The block of memory.</returns>
    public static unsafe void* AllocNativeMemory(nuint size)
    {
        return NativeMemory.Alloc(size);
    }

    /// <summary>
    ///     Frees a block of memory at the specified address.
    /// </summary>
    /// <param name="addr">Address of the native allocation.</param>
    /// <returns>The block of memory.</returns>
    public static unsafe void FreeNativeMemory(void* addr)
    {
        NativeMemory.Free(addr);
    }
}
