using System.IO.MemoryMappedFiles;
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

    /// <summary>
    /// Full path to the file.
    /// </summary>
    public string FullPath { get; init; }
    
    private MemoryMappedFile? _mappedFile;
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
        FullPath = Path.GetFullPath(Path.Combine(outputFolder, RelativePath));

        trycreate:
        try
        {
#if NET7_0_OR_GREATER
            _fileStream = new FileStream(FullPath, new FileStreamOptions
            {
                PreallocationSize = (long)entry.DecompressedSize,
                Access = FileAccess.ReadWrite,
                Mode = FileMode.Create,
                Share = FileShare.ReadWrite
            });
#else
            _fileStream = new FileStream(FullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            _fileStream.SetLength((long)entry.DecompressedSize);
#endif
        }
        catch (DirectoryNotFoundException)
        {
            // This is written this way because explicit check is slow.
            Directory.CreateDirectory(Path.GetDirectoryName(FullPath)!);
            goto trycreate;
        }

        if (entry.DecompressedSize > 0)
            _mappedFile = MemoryMappedFile.CreateFromFile(_fileStream, null, (long)entry.DecompressedSize, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
    }
    
    /// <inheritdoc />
    public IFileData GetFileData(long start, uint length) => new MemoryMappedOutputFileData(_mappedFile!, start, length);

    /// <inheritdoc />
    ~OutputFileProvider() => Dispose();
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _mappedFile?.Dispose();
        _fileStream.Dispose();
        GC.SuppressFinalize(this);
    }
}
