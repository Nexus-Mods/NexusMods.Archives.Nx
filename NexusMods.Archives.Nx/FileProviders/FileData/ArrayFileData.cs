using System.Runtime.InteropServices;
using NexusMods.Archives.Nx.Interfaces;

namespace NexusMods.Archives.Nx.FileProviders.FileData;

/// <summary>
///     Implementation of <see cref="IFileData" /> backed up by a pinned array.
/// </summary>
public unsafe class ArrayFileData : IFileData
{
    /// <inheritdoc />
    public byte* Data { get; }

    /// <inheritdoc />
    public UIntPtr DataLength { get; }

    private GCHandle _handle;

    /// <summary>
    ///     Creates file data backed by an array.
    /// </summary>
    /// <param name="data">The data backed up by the array.</param>
    /// <param name="start">Start offset.</param>
    /// <param name="length">Length into the data.</param>
    public ArrayFileData(byte[] data, long start, uint length)
    {
        _handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        Data = (byte*)_handle.AddrOfPinnedObject() + start;
        // ReSharper disable once RedundantCast
        DataLength = (UIntPtr)length;
    }

    /// <inheritdoc />
    public void Dispose() => _handle.Free();
}
