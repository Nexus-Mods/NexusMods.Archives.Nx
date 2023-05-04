using System.IO.MemoryMappedFiles;
using NexusMods.Archives.Nx.Interfaces;

namespace NexusMods.Archives.Nx.FileProviders.FileData;

/// <summary>
///     Implementation of <see cref="IFileData" /> backed up by memory mapped files.
/// </summary>
public class MemoryMappedFileData : IFileData
{
    /// <inheritdoc />
    public unsafe byte* Data { get; }

    /// <inheritdoc />
    public nuint DataLength { get; }

    private readonly MemoryMappedFile _mappedFile;
    private readonly MemoryMappedViewAccessor _mappedFileView;
    private bool _disposed;

    /// <summary>
    ///     Creates file data backed by a memory mapped file.
    /// </summary>
    /// <param name="filePath">Path of the file to map.</param>
    /// <param name="start">Offset to start of the file.</param>
    /// <param name="length">Length of the data to map.</param>
    public unsafe MemoryMappedFileData(string filePath, long start, uint length)
    {
        // TODO: Investigate if it's worth using OpenExisting in cases of chunked files.
        // Checking if an existing MMF is already there is a perf penalty for opening lots of small files
        // but it would speed up large chunked files. Issue is; we don't know the tradeoff here :p.

        // Create a memory-mapped file
        _mappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);
        _mappedFileView = _mappedFile.CreateViewAccessor(start, length);
        Data = (byte*)_mappedFileView.SafeMemoryMappedViewHandle.DangerousGetHandle();
        DataLength = length;
    }

    /// <inheritdoc />
    ~MemoryMappedFileData() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _mappedFile.Dispose();
        _mappedFileView.Dispose();
        GC.SuppressFinalize(this);
    }
}
