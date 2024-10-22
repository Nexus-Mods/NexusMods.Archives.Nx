using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Extensions for dealing with arrays.
/// </summary>
internal static class ArrayExtensions
{
    /// <summary>
    ///     Returns a reference to an element at a specified index without performing a bounds check.
    /// </summary>
    /// <typeparam name="T">The type of elements in the input <typeparamref name="T" /> array instance.</typeparam>
    /// <param name="array">The input <typeparamref name="T" /> array instance.</param>
    /// <param name="i">The index of the element to retrieve within <paramref name="array" />.</param>
    /// <returns>A reference to the element within <paramref name="array" /> at the index specified by <paramref name="i" />.</returns>
    /// <remarks>
    ///     This method doesn't do any bounds checks, therefore it is responsibility of the caller to ensure the
    ///     <paramref name="i" /> parameter is valid.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T DangerousGetReferenceAt<T>(this T[] array, int i)
    {
        ref var r0 = ref MemoryMarshal.GetArrayDataReference(array);
        return ref Unsafe.Add(ref r0, (nint)(uint)i);
    }
}
