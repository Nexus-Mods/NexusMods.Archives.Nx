using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Abstracts a native memory allocation.
/// </summary>
internal unsafe struct NativeMemoryAllocation : IDisposable
{
    /// <summary>
    ///     Raw pointer to the data.
    /// </summary>
    public byte* Pointer { get; private set; }

    /// <summary>
    ///     Length of the current allocation.
    /// </summary>
    public int Length { get; private set; }

    public NativeMemoryAllocation(int totalPathSize)
    {
        Pointer = Alloc(totalPathSize);
        Length = totalPathSize;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Pointer == (void*)0)
            return;

        Free();
        Pointer = (byte*)0;
    }

    /// <summary>
    ///     Grows the allocated memory; doubling the size.
    /// </summary>
    public void Grow()
    {
        // Alloc new
        var newSize = Length * 2;
        var newMemory = Alloc(newSize);

        // Copy
        Unsafe.CopyBlockUnaligned(newMemory, Pointer, (uint)Length);

        // Free
        Free();

        // Reassign
        Pointer = newMemory;
        Length = newSize;
    }

    /// <summary>
    ///     Frees the native memory associated with this instance.
    /// </summary>
    private void Free()
    {
#if NET7_0_OR_GREATER
        NativeMemory.Free(Pointer);
#else
        Marshal.FreeHGlobal((IntPtr)Pointer);
#endif
    }

    /// <summary>
    ///     Allocates native memory with specified size.
    /// </summary>
    private byte* Alloc(int size)
    {
#if NET7_0_OR_GREATER
        return (byte*)NativeMemory.Alloc((UIntPtr)size);
#else
        return (byte*)Marshal.AllocHGlobal(size);
#endif
    }
}
