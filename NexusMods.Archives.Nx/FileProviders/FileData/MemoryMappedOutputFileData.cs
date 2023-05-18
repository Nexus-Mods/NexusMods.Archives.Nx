using System.IO.MemoryMappedFiles;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Interfaces;

// ReSharper disable IntroduceOptionalParameters.Global

namespace NexusMods.Archives.Nx.FileProviders.FileData;

/// <summary>
///     Implementation of <see cref="IFileData" /> backed up by memory mapped files.
/// </summary>
[PublicAPI]
public sealed class MemoryMappedOutputFileData : IFileData
{
    /// <inheritdoc />
    public unsafe byte* Data { get; private set; }

    /// <inheritdoc />
    public nuint DataLength { get; private set; }

    private MemoryMappedViewAccessor? _mappedFileView;
    private bool _disposed;

    /// <summary>
    ///     Creates file data backed by a memory mapped file.
    /// </summary>
    /// <param name="file">The memory mapped file.</param>
    /// <param name="start">Offset to start of the file.</param>
    /// <param name="length">Length of the data to map.</param>
    public unsafe MemoryMappedOutputFileData(MemoryMappedFile file, long start, uint length)
    {
        // Create a memory-mapped file
        if (length != 0)
        {
            _mappedFileView = file!.CreateViewAccessor(start, length, MemoryMappedFileAccess.ReadWrite);
            Data = (byte*)_mappedFileView.SafeMemoryMappedViewHandle.DangerousGetHandle();
            DataLength = length;
            return;
        }

        InitEmpty();
    }

    private unsafe void InitEmpty()
    {
        Data = (byte*)0x0;
        DataLength = 0;
        _mappedFileView = null;
    }

    /// <inheritdoc />
    ~MemoryMappedOutputFileData() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _mappedFileView?.Dispose();
        GC.SuppressFinalize(this);
    }
}
