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
    ///     Full path to the file.
    /// </summary>
    public string FullPath { get; init; }

    private readonly MemoryMappedFile _mappedFile;
    private bool _isDisposed;

    /// <summary>
    ///     Creates a provider for outputting to a file.
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
            _mappedFile = MemoryMappedFile.CreateFromFile(FullPath, FileMode.Create, null, (long)entry.DecompressedSize, MemoryMappedFileAccess.ReadWrite);
        }
        catch (DirectoryNotFoundException)
        {
            // This is written this way because explicit check is slow.
            Directory.CreateDirectory(Path.GetDirectoryName(FullPath)!);
            goto trycreate;
        }
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
        _mappedFile.Dispose();
        GC.SuppressFinalize(this);
    }
}
