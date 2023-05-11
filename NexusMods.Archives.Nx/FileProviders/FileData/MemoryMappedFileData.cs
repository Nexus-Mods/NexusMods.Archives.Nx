using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using NexusMods.Archives.Nx.Interfaces;

namespace NexusMods.Archives.Nx.FileProviders.FileData;

/// <summary>
///     Implementation of <see cref="IFileData" /> backed up by memory mapped files.
/// </summary>
public sealed class MemoryMappedFileData : IFileData
{
    /// <inheritdoc />
    public unsafe byte* Data { get; private set; }

    /// <inheritdoc />
    public nuint DataLength { get; private set; }

    private MemoryMappedFile? _mappedFile;
    private MemoryMappedViewAccessor? _mappedFileView;
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
        if (length != 0)
        {
            var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            _mappedFile = MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.Inheritable, false);
            InitFromMmf(start, length);
            return;
        }
        
        InitEmpty();
    }

    /// <summary>
    ///     Creates file data backed by a memory mapped file.
    /// </summary>
    /// <param name="stream">The base stream to map to.</param>
    /// <param name="start">Offset to start of the file.</param>
    /// <param name="length">Length of the data to map.</param>
    public unsafe MemoryMappedFileData(FileStream stream, long start, uint length)
    {
        // TODO: Investigate if it's worth using OpenExisting in cases of chunked files.
        // Checking if an existing MMF is already there is a perf penalty for opening lots of small files
        // but it would speed up large chunked files. Issue is; we don't know the tradeoff here :p.

        // Create a memory-mapped file
        if (length != 0)
        {
            _mappedFile = MemoryMappedFile.CreateFromFile(stream, null, length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
            InitFromMmf(start, length);
            return;
        }
        
        InitEmpty();
    }

    private unsafe void InitFromMmf(long start, uint length)
    {
        _mappedFileView = _mappedFile!.CreateViewAccessor(start, length);
        Data = (byte*)_mappedFileView.SafeMemoryMappedViewHandle.DangerousGetHandle();
        DataLength = length;
    }
    
    private unsafe void InitEmpty()
    {
        Data = (byte*)0x0;
        DataLength = 0;
        _mappedFile = null;
        _mappedFileView = null;
    }

    /// <inheritdoc />
    ~MemoryMappedFileData() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _mappedFile?.Dispose();
        _mappedFileView?.Dispose();
        GC.SuppressFinalize(this);
    }
}
