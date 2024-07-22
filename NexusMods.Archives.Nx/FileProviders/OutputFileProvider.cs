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

    private readonly MemoryMappedFile? _mappedFile;
    private bool _isDisposed;
    private readonly bool _isEmpty;

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

    TryCreate:
        try
        {
            if (entry.DecompressedSize <= 0)
            {
                using var _ = File.Create(FullPath);
                _isEmpty = true;
                return;
            }

            // TODO: Use OS APIs directly.
            // We specifically delete to skip a 'file already exists' check,
            // which we can't avoid with the existing API.

            // By passing FileMode.CreateNew, in conjugation with the delete above,
            // we ensure that the file is created empty, as we otherwise would
            // with `FileMode.Create`.

            // Ideally we would create the MMFs using OS APIs directly, but for
            // now that's not an option, time-wise.

            // Forcing an unlink/delete, instead of checking for existence, is faster.
            File.Delete(FullPath);
            _mappedFile = MemoryMappedFile.CreateFromFile(FullPath, FileMode.CreateNew, null, (long)entry.DecompressedSize, MemoryMappedFileAccess.ReadWrite);
        }
        catch (DirectoryNotFoundException)
        {
            // This is written this way because explicit check is slow.
            Directory.CreateDirectory(Path.GetDirectoryName(FullPath)!);
            goto TryCreate;
        }
    }

    /// <inheritdoc />
    public IFileData GetFileData(ulong start, ulong length)
    {
        if (_isEmpty)
            return new ArrayFileData([], 0, 0);

        return new MemoryMappedOutputFileData(_mappedFile!, start, length);
    }

    /// <inheritdoc />
    ~OutputFileProvider() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _mappedFile?.Dispose();
        GC.SuppressFinalize(this);
    }
}
