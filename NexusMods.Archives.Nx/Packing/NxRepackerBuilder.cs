using NexusMods.Archives.Nx.FileProviders.FileData;
using NexusMods.Archives.Nx.Interfaces;

namespace NexusMods.Archives.Nx.Packing;

/// <summary>
///     A high level API for repacking archives.
///
///     This API takes a single archive as input, and allows you to perform
///     'transformations' on it, such as deleting files, renaming files,
///     removing files and adding files.
/// </summary>
public class NxRepackerBuilder
{
    private IFileData _originalNx;

    /// <summary>
    ///     Creates a repacker for repacking an existing Nx archive.
    /// </summary>
    /// <param name="filePath">Path to the original Nx archive.</param>
    public NxRepackerBuilder(string filePath) : this(new MemoryMappedFileData(filePath, 0, (uint)new FileInfo(filePath).Length)) { }

    /// <summary>
    ///     Creates a repacker for repacking an existing Nx archive.
    /// </summary>
    /// <param name="originalNxFileData">
    ///     An accessor to the original data of the source Nx archive.
    /// </param>
    public NxRepackerBuilder(IFileData originalNxFileData)
    {
        _originalNx = originalNxFileData;
    }

    /// <summary>
    ///     Adds a file which is guaranteed to be new.
    ///     Adding a file with an already existing path is considered an error.
    ///
    ///     If you don't know if the file already exists,
    ///     use <see cref="AddOrReplaceFile"/>.
    /// </summary>
    public void AddNewFile()
    {

    }

    public void AddOrReplaceFile()
    {

    }

    /// <summary>
    ///     This adds all files from an existing Nx archive.
    /// </summary>
    public void AddFromExistingNxArchive()
    {
        /*
            This works by adding a bunch of `NxSolidItemFileDataProvider`(s)
            and `NxChunkedItemFileDataProvider`(s) to the repacker.
        */


    }
}
