using NexusMods.Archives.Nx.FileProviders.FileData;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;

namespace NexusMods.Archives.Nx.FileProviders;

/// <summary>
///     File data provider that provides info from an array.
/// </summary>
public sealed class OutputFileProvider : IOutputDataProvider
{
    /// <inheritdoc />
    public string RelativePath { get; init; }

    /// <inheritdoc />
    public FileEntry Entry { get; init; }

    private readonly FileStream _fileStream;
    private bool _isDisposed;

    /// <summary>
    /// Creates a provider for outputting to a file.
    /// </summary>
    /// <param name="outputFolder">Folder to output data to.</param>
    /// <param name="relativePath">The relative path of the file.</param>
    /// <param name="entry">The individual file entry.</param>
    public OutputFileProvider(string outputFolder, string relativePath, FileEntry entry)
    {
        RelativePath = relativePath;
        Entry = entry;

        // Preallocate the file
        // Note: GetFullPath normalizes the path.
        var fullPath = Path.GetFullPath(Path.Combine(outputFolder, RelativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

#if NET7_0_OR_GREATER
        _fileStream = new FileStream(fullPath, new FileStreamOptions
        {
            PreallocationSize = (long)entry.DecompressedSize,
            Access = FileAccess.ReadWrite,
            Mode = FileMode.Create,
            Share = FileShare.ReadWrite
        });
#else
        _fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        _fileStream.SetLength((long)entry.DecompressedSize);
#endif
    }
    
    /// <inheritdoc />
    public IFileData GetFileData(long start, uint length) => new MemoryMappedFileData(_fileStream, start, length);
    
    /// <inheritdoc />
    ~OutputFileProvider() => Dispose();
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _fileStream.Dispose();
        GC.SuppressFinalize(this);
    }
}
