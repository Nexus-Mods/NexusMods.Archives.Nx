using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NETCOREAPP2_1_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

// ReSharper disable MemberCanBePrivate.Global

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Extension methods tied to spans.
/// </summary>
internal static class SpanExtensions
{
    /// <summary>
    ///     Casts a span to another type without bounds checks.
    /// </summary>
    [ExcludeFromCodeCoverage] // "Taken from runtime."
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<TTo> CastFast<TFrom, TTo>(this Span<TFrom> data) where TFrom : struct where TTo : struct
    {
#if NETSTANDARD2_0
        return MemoryMarshal.Cast<TFrom, TTo>(data);
#else
        // Taken from the runtime.
        // Use unsigned integers - unsigned division by constant (especially by power of 2)
        // and checked casts are faster and smaller.
        var fromSize = (uint)Unsafe.SizeOf<TFrom>();
        var toSize = (uint)Unsafe.SizeOf<TTo>();
        var fromLength = (uint)data.Length;
        int toLength;
        if (fromSize == toSize)
        {
            // Special case for same size types - `(ulong)fromLength * (ulong)fromSize / (ulong)toSize`
            // should be optimized to just `length` but the JIT doesn't do that today.
            toLength = (int)fromLength;
        }
        else if (fromSize == 1)
        {
            // Special case for byte sized TFrom - `(ulong)fromLength * (ulong)fromSize / (ulong)toSize`
            // becomes `(ulong)fromLength / (ulong)toSize` but the JIT can't narrow it down to `int`
            // and can't eliminate the checked cast. This also avoids a 32 bit specific issue,
            // the JIT can't eliminate long multiply by 1.
            toLength = (int)(fromLength / toSize);
        }
        else
        {
            // Ensure that casts are done in such a way that the JIT is able to "see"
            // the uint->ulong casts and the multiply together so that on 32 bit targets
            // 32x32to64 multiplication is used.
            var toLengthUInt64 = fromLength * (ulong)fromSize / toSize;
            toLength = (int)toLengthUInt64;
        }

        return MemoryMarshal.CreateSpan(
            ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(data)),
            toLength);
#endif
    }

    /// <summary>
    ///     Slices a span without any bounds checks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> SliceFast<T>(this Span<T> data, int start, int length)
    {
#if NETSTANDARD2_0
        return data.Slice(start, length);
#else
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(data), start), length);
#endif
    }

    /// <summary>
    ///     Replaces the occurrences of one character with another in a span.
    /// </summary>
    /// <param name="data">The data to replace the value in.</param>
    /// <param name="oldValue">The original value to be replaced.</param>
    /// <param name="newValue">The new replaced value.</param>
    /// <param name="buffer">
    ///     The buffer to place the result in.
    ///     This can be the original <paramref name="data" /> buffer if required.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Replace(this Span<char> data, char oldValue, char newValue, Span<char> buffer) =>
        // char is not supported by Vector; but ushort is.
        Replace(data.CastFast<char, ushort>(), oldValue, newValue, buffer.CastFast<char, ushort>()).CastFast<ushort, char>();

    /// <summary>
    ///     Replaces the occurrences of one value with another in a span.
    /// </summary>
    /// <param name="data">The data to replace the value in.</param>
    /// <param name="oldValue">The original value to be replaced.</param>
    /// <param name="newValue">The new replaced value.</param>
    /// <param name="buffer">
    ///     The buffer to place the result in.
    ///     This can be the original <paramref name="data" /> buffer if required.
    /// </param>
    /// <typeparamref name="T">MUST BE POWER OF TWO IN SIZE. Type of value to replace.</typeparamref>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> Replace<T>(this Span<T> data, T oldValue, T newValue, Span<T> buffer)
        where T : unmanaged, IEquatable<T>
    {
        // In the case they are the same, do nothing.
        if (oldValue.Equals(newValue))
            return data;

        // Slice our output buffer.
        buffer = buffer.SliceFast(0, data.Length);
        var remainingLength = (nuint)data.Length;

        // Copy the remaining characters, doing the replacement as we go.
        // Note: We can index 0 directly since we know length is >0 given length check from earlier.
        ref var pSrc = ref data[0];
        ref var pDst = ref buffer[0];
        nuint x = 0;

        if (Vector.IsHardwareAccelerated && data.Length >= Vector<T>.Count)
        {
            Vector<T> oldValues = new(oldValue);
            Vector<T> newValues = new(newValue);

            Vector<T> original;
            Vector<T> equals;
            Vector<T> results;

            if (remainingLength > (nuint)Vector<T>.Count)
            {
                var lengthToExamine = remainingLength - (nuint)Vector<T>.Count;

                do
                {
                    original = VectorExtensions.LoadUnsafe(ref pSrc, x);
                    equals = Vector.Equals(original, oldValues); // Generate Mask
                    results = Vector.ConditionalSelect(equals, newValues, original); // Swap in Values
                    results.StoreUnsafe(ref pDst, x);

                    x += (nuint)Vector<T>.Count;
                } while (x < lengthToExamine);
            }

            // There are between 0 to Vector<T>.Count elements remaining now.  

            // Since our operation can be applied multiple times without changing the result
            // [applying the replacement twice is non destructive]. We can avoid non-vectorised code
            // here and simply do the vectorised logic in an unaligned fashion, doing just the chunk
            // at the end of the original buffer.
            x = (uint)(data.Length - Vector<T>.Count);
            original = VectorExtensions.LoadUnsafe(ref data[0], x);
            equals = Vector.Equals(original, oldValues);
            results = Vector.ConditionalSelect(equals, newValues, original);
            results.StoreUnsafe(ref buffer[0], x);
        }
        else
        {
            // Non-vector fallback, slow.
            for (; x < remainingLength; ++x)
            {
                var currentChar = Unsafe.Add(ref pSrc, (nint)x);
                Unsafe.Add(ref pDst, (nint)x) = currentChar.Equals(oldValue) ? newValue : currentChar;
            }
        }

        return buffer;
    }

    /// <summary>
    ///     Finds all offsets of a given value within the specified data.
    /// </summary>
    /// <param name="data">The data to search within.</param>
    /// <param name="value">Value to listen to.</param>
    /// <param name="offsetCountHint">Hint for likely amount of offsets.</param>
    /// <returns>A list of all offsets of a given value within the span.</returns>
    public static unsafe List<int> FindAllOffsetsOfByte(this Span<byte> data, byte value, int offsetCountHint = 0)
    {
        // Note: A generic implementation wouldn't look too different here; just would need another fallback in case
        // sizeof(T) is bigger than nint.

        // TODO: Unrolled CPU version for non-AMD64 platforms.
        // Note: I wrote this in SSE/AVX directly because System.Numerics.Vectors does not have equivalent of MoveMask
        //       which means getting offset of matched value is slow.
        var offsets = new List<int>(offsetCountHint);
        fixed (byte* dataPtr = data)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (Avx2.IsSupported)
            {
                FindAllOffsetsOfByteAvx2(dataPtr, data.Length, value, offsets);
                return offsets;
            }

            if (Sse2.IsSupported) // all AMD64 CPUs
            {
                FindAllOffsetsOfByteSse2(dataPtr, data.Length, value, offsets);
                return offsets;
            }
#endif

            // Otherwise probably not a x64 CPU.
            FindAllOffsetsOfByteFallback(dataPtr, data.Length, value, 0, offsets);
            return offsets;
        }
    }

    internal static unsafe void FindAllOffsetsOfByteFallback(byte* data, int length, byte value, int addToResults,
        List<int> results)
    {
        var dataPtr = data;
        var dataMaxPtr = dataPtr + length;
        while (dataPtr < dataMaxPtr)
        {
            var item = *dataPtr;
            if (item == value)
                results.Add((int)(dataPtr - data) + addToResults);

            dataPtr++;
        }
    }

#if NETCOREAPP3_1_OR_GREATER
    internal static unsafe void FindAllOffsetsOfByteAvx2(byte* data, int length, byte value, List<int> results)
    {
        const int avxRegisterLength = 32;

        // Byte to search for.
        var byteVec = Vector256.Create(value);
        var dataPtr = data;
        var dataMaxPtr = dataPtr + (length - avxRegisterLength);
        const int simdJump = avxRegisterLength - 1;

        while (dataPtr < dataMaxPtr)
        {
            var rhs = Avx.LoadVector256(dataPtr);
            var equal = Avx2.CompareEqual(byteVec, rhs);
            var findFirstByte = Avx2.MoveMask(equal);

            // All 0s, so none of them had desired value.
            if (findFirstByte == 0)
            {
                dataPtr += simdJump;
                continue;
            }

            // Shift up until first byte found.
            dataPtr += BitOperations.TrailingZeroCount((uint)findFirstByte);
            results.Add((int)(dataPtr - data));
            dataPtr++; // go to next element
        }

        // Check last few bytes using byte by byte comparison.
        var position = (int)(dataPtr - data);
        FindAllOffsetsOfByteFallback(data + position, length - position, value, position, results);
    }

    internal static unsafe void FindAllOffsetsOfByteSse2(byte* data, int length, byte value, List<int> results)
    {
        const int sseRegisterLength = 16;

        // Byte to search for.
        var byteVec = Vector128.Create(value);
        var dataPtr = data;
        var dataMaxPtr = dataPtr + (length - sseRegisterLength);
        const int simdJump = sseRegisterLength - 1;

        while (dataPtr < dataMaxPtr)
        {
            var rhs = Sse2.LoadVector128(dataPtr);
            var equal = Sse2.CompareEqual(byteVec, rhs);
            var findFirstByte = Sse2.MoveMask(equal);

            // All 0s, so none of them had desired value.
            if (findFirstByte == 0)
            {
                dataPtr += simdJump;
                continue;
            }

            // Shift up until first byte found.
            dataPtr += BitOperations.TrailingZeroCount((uint)findFirstByte);
            results.Add((int)(dataPtr - data));
            dataPtr++; // go to next element
        }

        // Check last few bytes using byte by byte comparison.
        var position = (int)(dataPtr - data);
        FindAllOffsetsOfByteFallback(data + position, length - position, value, position, results);
    }
#endif
}
