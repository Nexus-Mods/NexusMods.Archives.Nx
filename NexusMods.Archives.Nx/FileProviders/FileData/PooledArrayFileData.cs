using System.Runtime.InteropServices;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.FileProviders.FileData;

/// <summary>
///     Implementation of <see cref="IFileData" /> backed up by a pinned array.
/// </summary>
public unsafe class RentedArrayFileData : IFileData
{
    /// <inheritdoc />
    public byte* Data { get; }

    /// <inheritdoc />
    public UIntPtr DataLength { get; }

    private ArrayRentalSlice _data;
    private GCHandle _handle;

    /// <summary>
    /// Creates file data backed by an array.
    /// </summary>
    /// <param name="data">The data backed up by the array.</param>
    public RentedArrayFileData(ArrayRentalSlice data)
    {
        _data = data;
        _handle = GCHandle.Alloc(data.Rental.Array, GCHandleType.Pinned);
        Data = (byte*)_handle.AddrOfPinnedObject();
        DataLength = (UIntPtr)data.Length;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _handle.Free();
        _data.Dispose();
    }
}
