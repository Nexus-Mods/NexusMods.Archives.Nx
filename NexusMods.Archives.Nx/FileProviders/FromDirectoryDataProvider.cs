using NexusMods.Archives.Nx.FileProviders.FileData;
using NexusMods.Archives.Nx.Interfaces;

namespace NexusMods.Archives.Nx.FileProviders;

/// <summary>
///     File data provider that provides files from a given directory.
/// </summary>
public class FromDirectoryDataProvider : IFileDataProvider
{
    // Note: We store directory and relative path separately because that saves memory in our use case (deduplicated strings);
    //       then we can temporarily combine when GetFileData gets called down the road.

    /// <summary>
    ///     The directory from which the data will be fetched from.
    /// </summary>
    public required string Directory { get; init; }

    /// <summary>
    ///     Relative path to directory.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <inheritdoc />
    public IFileData GetFileData(long start, uint length) => new MemoryMappedFileData(Path.Combine(Directory, RelativePath), start, length);
}
