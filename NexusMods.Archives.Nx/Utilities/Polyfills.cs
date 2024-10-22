using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Method wrappers for newer runtime features that delegate back to older approaches on unsupported runtimes.
/// </summary>
[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
internal static class Polyfills
{
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
}
