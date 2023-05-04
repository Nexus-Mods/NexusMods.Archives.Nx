using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Extensions tied to the <see cref="Vector" /> class.
/// </summary>
[ExcludeFromCodeCoverage] // Taken from another project where this is tested.
internal static class VectorExtensions
{
    /// <summary>
    ///     Loads an element at an offset into a vector.
    /// </summary>
    /// <param name="source">Where to load the element from.</param>
    /// <param name="elementOffset">Offset of the element.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<T> LoadUnsafe<T>(ref T source, nuint elementOffset) where T : struct
    {
#if NET7_0_OR_GREATER
        source = ref Unsafe.Add(ref source, elementOffset);
#else
        source = ref Unsafe.Add(ref source, (int)elementOffset);
#endif
        return Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref source));
    }

    /// <summary>
    ///     Stores an element from a vector into destination + offset.
    /// </summary>
    /// <param name="source">The vector to store.</param>
    /// <param name="destination">Where to store the vector.</param>
    /// <param name="elementOffset">Offset of the element.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StoreUnsafe<T>(this Vector<T> source, ref T destination, nuint elementOffset) where T : struct
    {
#if NET7_0_OR_GREATER
        destination = ref Unsafe.Add(ref destination, elementOffset);
#else
        destination = ref Unsafe.Add(ref destination, (int)elementOffset);
#endif
        Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
    }
}
