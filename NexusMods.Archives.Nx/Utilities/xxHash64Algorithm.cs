using System.Runtime.CompilerServices;
using static System.Numerics.BitOperations;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Based on the code found at (https://github.com/brandondahler/Data.HashFunction/)
///     The MIT License (MIT)
///     Copyright (c) 2014 Data.HashFunction Developers
///     Permission is hereby granted, free of charge, to any person obtaining a copy
///     of this software and associated documentation files (the "Software"), to deal
///     in the Software without restriction, including without limitation the rights
///     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
///     copies of the Software, and to permit persons to whom the Software is
///     furnished to do so, subject to the following conditions:
///     The above copyright notice and this permission notice shall be included in all
///     copies or substantial portions of the Software.
///     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
///     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
///     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
///     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
///     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
///     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
///     SOFTWARE.
/// </summary>
internal unsafe struct XxHash64Algorithm
{
    // ReSharper disable InconsistentNaming
    private const ulong Primes64_0 = 11400714785074694791UL;
    private const ulong Primes64_1 = 14029467366897019727UL;
    private const ulong Primes64_2 = 1609587929392839161UL;
    private const ulong Primes64_3 = 9650029242287828579UL;
    private const ulong Primes64_4 = 2870177450012600261UL;
    // ReSharper restore InconsistentNaming

    private readonly ulong _seed;

    private ulong _a;
    private ulong _b;
    private ulong _c;
    private ulong _d;

    private ulong _bytesProcessed;

    /// <summary>
    /// Creates a new implementation of the XxHash64 hasher.
    /// </summary>
    /// <param name="seed">The seed for the hashing function.</param>
    public XxHash64Algorithm(ulong seed)
    {
        _seed = seed;
        _a = _seed + Primes64_0 + Primes64_1;
        _b = _seed + Primes64_1;
        _c = _seed;
        _d = _seed - Primes64_0;
        _bytesProcessed = 0;
    }

    /// <summary>
    /// Creates a new implementation of the XxHash64 hasher with a default seed of 0.
    /// </summary>
    public XxHash64Algorithm() : this(0) { }

    /// <summary>
    /// Hashes the given span of bytes.
    /// </summary>
    /// <param name="data">The complete data to hash.</param>
    /// <param name="seed">The seed for the hash.</param>
    /// <returns>Hash for the given bytes.</returns>
    /// <remarks>
    ///     Assumes the given bytes form a complete object you want to hash.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong HashBytes(ReadOnlySpan<byte> data, ulong seed = 0)
    {
        fixed (byte* ptr = data)
        {
            return HashBytes(ptr, (ulong)data.Length, seed);
        }
    }

    /// <summary>
    /// Hashes the given pointer to bytes.
    /// </summary>
    /// <param name="data">Pointer to the complete data to hash.</param>
    /// <param name="length">Length of the data to hash.</param>
    /// <param name="seed">The seed for the hash.</param>
    /// <returns>Hash for the given bytes.</returns>
    /// <remarks>
    ///     Assumes the given bytes form a complete object you want to hash.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong HashBytes(byte* data, ulong length, ulong seed = 0)
    {
        var hash = new XxHash64Algorithm(seed);
        var initialSize = (length >> 5) << 5;
        if (initialSize > 0)
            hash.TransformByteGroupsInternal(data, initialSize);

        return hash.FinalizeHashValueInternal(data + initialSize, (int)(length - initialSize));
    }

    /// <summary>
    /// Updates the internal hasher state.
    /// </summary>
    /// <param name="data">
    ///     Pointer to the data to feed into the hasher.
    ///     Size of this data must be a multiple of 32, and be directly after
    ///     the data used in the last call to `TransformByteGroupsInternal`.
    /// </param>
    /// <param name="length">Length of the data to process.</param>
    /// <remarks>
    ///    The last &lt;32 bytes should be sent to <see cref="FinalizeHashValueInternal(byte*,int)"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TransformByteGroupsInternal(byte* data, ulong length)
    {
#if DEBUG
        if (length % 32 > 0)
            throw new Exception("Input is not a multiple of 32");
#endif

        var tempA = _a;
        var tempB = _b;
        var tempC = _c;
        var tempD = _d;

        var len = length / 8;
        var maxAddr = (ulong*)data + len;
        var dataPtr = (ulong*)data;

        while (dataPtr < maxAddr)
        {
            tempA += dataPtr[0] * Primes64_1;
            tempA = RotateLeft(tempA, 31);
            tempA *= Primes64_0;

            tempB += dataPtr[1] * Primes64_1;
            tempB = RotateLeft(tempB, 31);
            tempB *= Primes64_0;

            tempC += dataPtr[2] * Primes64_1;
            tempC = RotateLeft(tempC, 31);
            tempC *= Primes64_0;

            tempD += dataPtr[3] * Primes64_1;
            tempD = RotateLeft(tempD, 31);
            tempD *= Primes64_0;

            dataPtr += 4;
        }

        _a = tempA;
        _b = tempB;
        _c = tempC;
        _d = tempD;

        _bytesProcessed += length;
    }

    /// <summary>
    /// Updates the internal hasher state for the last &lt;32 bytes.
    /// After this function is completed, the final hash is returned.
    /// </summary>
    /// <param name="data">Pointer to the last &lt;32 bytes to hash.</param>
    /// <param name="length">Length of the remaining data to hash.</param>
    /// <returns>The final hash for the object.</returns>
    /// <remarks>
    ///    This should only be called once.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong FinalizeHashValueInternal(byte* data, int length)
    {
        ulong hashValue;
        {
            if (_bytesProcessed > 0)
            {
                var tempA = _a;
                var tempB = _b;
                var tempC = _c;
                var tempD = _d;

                hashValue = RotateLeft(_a, 1) + RotateLeft(_b, 7) + RotateLeft(_c, 12) + RotateLeft(_d, 18);

                // A
                tempA *= Primes64_1;
                tempA = RotateLeft(tempA, 31);
                tempA *= Primes64_0;

                hashValue ^= tempA;
                hashValue = hashValue * Primes64_0 + Primes64_3;

                // B
                tempB *= Primes64_1;
                tempB = RotateLeft(tempB, 31);
                tempB *= Primes64_0;

                hashValue ^= tempB;
                hashValue = hashValue * Primes64_0 + Primes64_3;

                // C
                tempC *= Primes64_1;
                tempC = RotateLeft(tempC, 31);
                tempC *= Primes64_0;

                hashValue ^= tempC;
                hashValue = hashValue * Primes64_0 + Primes64_3;

                // D
                tempD *= Primes64_1;
                tempD = RotateLeft(tempD, 31);
                tempD *= Primes64_0;

                hashValue ^= tempD;
                hashValue = hashValue * Primes64_0 + Primes64_3;
            }
            else
            {
                hashValue = _seed + Primes64_4;
            }
        }

        var remainderLength = length;
        hashValue += _bytesProcessed + (ulong)remainderLength;

        if (remainderLength > 0)
        {
            // In 8-byte chunks, process all full chunks
            for (var x = 0; x < length / 8; ++x)
            {
                hashValue ^= RotateLeft(*(ulong*)(data + x * 8) * Primes64_1, 31) * Primes64_0;
                hashValue = RotateLeft(hashValue, 27) * Primes64_0 + Primes64_3;
            }

            // Process a 4-byte chunk if it exists
            if (remainderLength % 8 >= 4)
            {
                var startOffset = remainderLength - remainderLength % 8;

                hashValue ^= *(uint*)(data + startOffset) * Primes64_0;
                hashValue = RotateLeft(hashValue, 23) * Primes64_1 + Primes64_2;
            }

            // Process last 4 bytes in 1-byte chunks (only runs if data.Length % 4 != 0)
            {
                var startOffset = remainderLength - remainderLength % 4;
                var endOffset = remainderLength;

                for (var currentOffset = startOffset; currentOffset < endOffset; currentOffset += 1)
                {
                    hashValue ^= *(data + currentOffset) * Primes64_4;
                    hashValue = RotateLeft(hashValue, 11) * Primes64_0;
                }
            }
        }

        hashValue ^= hashValue >> 33;
        hashValue *= Primes64_1;
        hashValue ^= hashValue >> 29;
        hashValue *= Primes64_2;
        hashValue ^= hashValue >> 32;

        return hashValue;
    }

    /// <summary>
    /// Updates the internal hasher state.
    /// </summary>
    /// <param name="data">
    ///     The data to feed into the hasher.
    ///     Size of this data must be a multiple of 32, and be directly after
    ///     the data used in the last call to `TransformByteGroupsInternal`.
    /// </param>
    /// <remarks>
    ///    The last &lt;32 bytes should be sent to <see cref="FinalizeHashValueInternal(byte*,int)"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TransformByteGroupsInternal(ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
        {
            TransformByteGroupsInternal(ptr, (ulong)data.Length);
        }
    }

    /// <summary>
    /// Updates the internal hasher state for the last &lt;32 bytes.
    /// After this function is completed, the final hash is returned.
    /// </summary>
    /// <param name="data">The last &lt;32 bytes to hash.</param>
    /// <returns>The final hash for the object.</returns>
    /// <remarks>
    ///    This should only be called once.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong FinalizeHashValueInternal(ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
        {
            return FinalizeHashValueInternal(ptr, data.Length);
        }
    }
}
