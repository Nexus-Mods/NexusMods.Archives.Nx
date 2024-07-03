# File Providers

!!! info "In this project `File Providers` are types that satisfy the `IFileDataProvider` or `IOutputDataProvider` interfaces."

These interfaces declare the `IFileData GetFileData(long start, uint length)` method.
The `IFileData` interface is defined as:

```csharp
/// <summary>
///     An interface for providing access to underlying file data.
/// </summary>
/// <remarks>
///     For read operations where entire file is not yet available e.g. over a network; you should stall until you can
///     provide enough data to provide.
/// </remarks>
public unsafe interface IFileData : IDisposable
{
    /// <summary>
    ///     Data of the underlying item.
    /// </summary>
    public byte* Data { get; }

    /// <summary>
    ///     Length of the underlying data.
    /// </summary>
    public ulong DataLength { get; }
}
```

And represents a chunk of data from a file. This can be a chunk of a file to be written, or a chunk of a file to be read.
Sources of `IFileData` can vary, for example, it can be a `Memory Mapped File` on disk, data received from the web or
simply an array in memory.

The `.nx` Packer and Unpacker will call the method `GetFileData(long start, uint length)` to get a slice of the data
which it can directly read or write to.

## Example

!!! info "Actual Example from Source Code"

```csharp
/// <summary>
///     File data provider that provides info from an array.
/// </summary>
public sealed class FromArrayProvider : IFileDataProvider
{
    /// <summary>
    ///     The array held by this provider.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <inheritdoc />
    public IFileData GetFileData(ulong start, ulong length) => new ArrayFileData(Data, start, length);
}

/// <summary>
///     Implementation of <see cref="IFileData" /> backed up by a pinned array.
/// </summary>
public sealed unsafe class ArrayFileData : IFileData
{
    /// <inheritdoc />
    public byte* Data { get; }

    /// <inheritdoc />
    public ulong DataLength { get; }

    private GCHandle _handle;
    private bool _disposed;

    /// <summary>
    ///     Creates file data backed by an array.
    /// </summary>
    /// <param name="data">The data backed up by the array.</param>
    /// <param name="start">Start offset.</param>
    /// <param name="length">Length into the data.</param>
    public ArrayFileData(byte[] data, ulong start, ulong length)
    {
        _handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        Data = (byte*)_handle.AddrOfPinnedObject() + start;
        // ReSharper disable once RedundantCast
        DataLength = )length;
    }

    /// <inheritdoc />
    ~ArrayFileData() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _handle.Free();
        GC.SuppressFinalize(this);
    }
}
```
