using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Growable pool of managed bytes.
/// </summary>
internal unsafe struct GrowableMemoryPool : IDisposable
{
    /// <summary>
    ///     Currently pinned array from pool.
    /// </summary>
    public byte[] PinnedArray { get; private set; }

    /// <summary>
    ///     Pointer to raw data.
    /// </summary>
    public byte* Pointer;

    /// <summary>
    ///     Available size.
    /// </summary>
    public int AvailableSize => PinnedArray.Length;

    private GCHandle _gcHandle;

    /// <summary>
    ///     Creates a growable memory pool.
    /// </summary>
    /// <param name="initialSize">Initial size of the rental.</param>
    /// <param name="zeroArray">Set this to true to zero the array.</param>
    public GrowableMemoryPool(int initialSize, bool zeroArray = false)
    {
        PinnedArray = ArrayPool<byte>.Shared.Rent(initialSize);
        _gcHandle = GCHandle.Alloc(PinnedArray, GCHandleType.Pinned);
        Pointer = (byte*)_gcHandle.AddrOfPinnedObject();

        if (zeroArray)
            Unsafe.InitBlockUnaligned(Pointer, 0, (uint)PinnedArray.Length);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(PinnedArray);
        _gcHandle = default;
        Pointer = (byte*)0;
    }

    /// <summary>
    ///     Grows this buffer, doubling the size.
    /// </summary>
    /// <param name="zeroArray">Set this to true to zero the array.</param>
    public void Grow(bool zeroArray = false)
    {
        // Note: Rentals above 1MB incur direct allocation.
        //       However for that to happen, we must get to around 20000 for original intended use case of packing mods.
        //       Very rarely a mod is going to have 10000+ files; so that would be a cold path.

        // Alloc new
        var newPool = ArrayPool<byte>.Shared.Rent(PinnedArray.Length * 2);
        var newPoolHandle = GCHandle.Alloc(newPool, GCHandleType.Pinned);
        var newPointer = (byte*)newPoolHandle.AddrOfPinnedObject();
        if (zeroArray)
            Unsafe.InitBlockUnaligned(newPointer, 0, (uint)newPool.Length);

        // Copy
        Unsafe.CopyBlockUnaligned(newPointer, Pointer, (uint)PinnedArray.Length);

        // Replace
        Dispose();

        // Replace Variables
        PinnedArray = newPool;
        _gcHandle = newPoolHandle;
        Pointer = newPointer;
    }
}
