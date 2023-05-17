using System.Runtime.InteropServices;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.FileProviders.FileData;

/// <summary>
///     Implementation of <see cref="IFileData" /> backed up by a rented array.
/// </summary>
public sealed unsafe class RentedArrayFileData : IFileData
{
    /// <inheritdoc />
    public byte* Data { get; }

    /// <inheritdoc />
    public nuint DataLength { get; }

    private readonly ArrayRentalSlice _data;
    private GCHandle _handle;
    private bool _isDisposed;

    /// <summary>
    ///     Creates file data backed by a rented array.
    /// </summary>
    /// <param name="data">The rented array.</param>
    public RentedArrayFileData(ArrayRentalSlice data)
    {
        _data = data;
        _handle = GCHandle.Alloc(data.Rental.Array, GCHandleType.Pinned);
        Data = (byte*)_handle.AddrOfPinnedObject();
        DataLength = (nuint)data.Length;
    }

    /// <inheritdoc />
    ~RentedArrayFileData() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _handle.Free();
        _data.Dispose();
        GC.SuppressFinalize(this);
    }
}
