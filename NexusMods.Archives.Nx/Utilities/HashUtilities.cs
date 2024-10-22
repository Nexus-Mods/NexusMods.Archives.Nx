using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace NexusMods.Archives.Nx.Utilities;

internal static class HashUtilities
{
    /// <summary>
    /// Append a span of memory to the hash algorithm.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Append(this XxHash3 algo, byte* ptr, ulong len)
    {
        var span = new ReadOnlySpan<byte>(ptr, (int)len);
        algo.Append(span);
    }

    /// <summary>
    /// Calculate the hash of a span of memory.
    /// </summary>
    public static unsafe ulong HashToUInt64(byte* ptr, ulong len)
    {
        return XxHash3.HashToUInt64(new ReadOnlySpan<byte>(ptr, (int)len));
    }
}
