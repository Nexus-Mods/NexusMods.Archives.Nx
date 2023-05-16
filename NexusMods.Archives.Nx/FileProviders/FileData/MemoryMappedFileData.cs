using System.IO.MemoryMappedFiles;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Interfaces;
// ReSharper disable IntroduceOptionalParameters.Global

namespace NexusMods.Archives.Nx.FileProviders.FileData;

/// <summary>
///     Implementation of <see cref="IFileData" /> backed up by memory mapped files.
/// </summary>
[PublicAPI]
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
    public unsafe MemoryMappedFileData(string filePath, long start, uint length) : this(filePath, start, length, false) { }
    
    /// <summary>
    ///     Creates file data backed by a memory mapped file.
    /// </summary>
    /// <param name="filePath">Path of the file to map.</param>
    /// <param name="start">Offset to start of the file.</param>
    /// <param name="length">Length of the data to map.</param>
    /// <param name="readOnly">If true, this is read only.</param>
    public unsafe MemoryMappedFileData(string filePath, long start, uint length, bool readOnly)
    {
        // TODO: Investigate if it's worth using OpenExisting in cases of chunked files.
        // Checking if an existing MMF is already there is a perf penalty for opening lots of small files
        // but it would speed up large chunked files. Issue is; we don't know the tradeoff here :p.

        // Create a memory-mapped file
        if (length != 0)
        {
            var fileMode = readOnly ? FileAccess.Read : FileAccess.ReadWrite;
            var mmfAccess = readOnly ? MemoryMappedFileAccess.Read : MemoryMappedFileAccess.ReadWrite;
            
            var fs = new FileStream(filePath, FileMode.Open, fileMode, FileShare.ReadWrite);
            _mappedFile = MemoryMappedFile.CreateFromFile(fs, null, 0, mmfAccess, HandleInheritability.Inheritable, false);
            InitFromMmf(start, length, readOnly);
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
    public unsafe MemoryMappedFileData(FileStream stream, long start, uint length) : this(stream, start, length, false) { }

    /// <summary>
    ///     Creates file data backed by a memory mapped file.
    /// </summary>
    /// <param name="stream">The base stream to map to.</param>
    /// <param name="start">Offset to start of the file.</param>
    /// <param name="length">Length of the data to map.</param>
    /// <param name="readOnly">If true, this is read only.</param>
    public unsafe MemoryMappedFileData(FileStream stream, long start, uint length, bool readOnly)
    {
        // TODO: Investigate if it's worth using OpenExisting in cases of chunked files.
        // Checking if an existing MMF is already there is a perf penalty for opening lots of small files
        // but it would speed up large chunked files. Issue is; we don't know the tradeoff here :p.

        // Create a memory-mapped file
        if (length != 0)
        {
            var mmfAccess = readOnly ? MemoryMappedFileAccess.Read : MemoryMappedFileAccess.ReadWrite;
            _mappedFile = MemoryMappedFile.CreateFromFile(stream, null, length, mmfAccess, HandleInheritability.None, true);
            InitFromMmf(start, length, readOnly);
            return;
        }
        
        InitEmpty();
    }

    private unsafe void InitFromMmf(long start, uint length, bool isReadOnly = false)
    {
        var access = isReadOnly ? MemoryMappedFileAccess.Read : MemoryMappedFileAccess.ReadWrite;
        _mappedFileView = _mappedFile!.CreateViewAccessor(start, length, access);
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
