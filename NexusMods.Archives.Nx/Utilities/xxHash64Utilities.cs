using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NexusMods.Archives.Nx.Utilities;

// ReSharper disable once InconsistentNaming
internal static unsafe class xxHash64Utilities
{
    /// <summary>
    /// Updates the current hash.
    /// </summary>
    /// <param name="hash">The XxHash64Algorithm instance.</param>
    /// <param name="data">The data to be hashed.</param>
    /// <param name="length">Length of the data being appended.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void AppendHash(this ref XxHash64Algorithm hash, byte* data, ulong length)
    {
        Debug.Assert(length % 32 == 0);
        hash.TransformByteGroupsInternal(data, length);
    }

    /// <summary>
    /// Receive the final hash.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong GetFinalHash(this ref XxHash64Algorithm hash, byte* data, ulong length)
    {
        var initialSize = (length >> 5) << 5;
        if (initialSize > 0)
            hash.TransformByteGroupsInternal(data, initialSize);

        var remainingBytes = (int)(length - initialSize);
        return hash.FinalizeHashValueInternal(data + initialSize, remainingBytes);
    }

    /// <summary>
    /// Updates the current hash.
    /// </summary>
    /// <param name="hash">The XxHash64Algorithm instance.</param>
    /// <param name="data">The data to be hashed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void AppendHash(this ref XxHash64Algorithm hash, Span<byte> data)
    {
        fixed (byte* dataPtr = data)
            AppendHash(ref hash, dataPtr, (ulong)data.Length);
    }

    /// <summary>
    /// Receive the final hash.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong GetFinalHash(this ref XxHash64Algorithm hash, Span<byte> data)
    {
        fixed (byte* dataPtr = data)
            return GetFinalHash(ref hash, dataPtr, (ulong)data.Length);
    }
}
