using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing;

/// <summary>
///     A specialized version of <see cref="NxRepackerBuilder"/> that deduplicates
///     files (already in Nx archives) based on their hash and relative path.
///     This can be used for merging archives together.
/// </summary>
/// <remarks>
///     This builder extends <see cref="NxRepackerBuilder"/> to provide file deduplication functionality.
///     Files are considered duplicates if they have the same hash AND the same relative path.
///     TODO: If two files have the same hash but different paths, both will be included in the archive.
///           And the data will be duplicated.
///           - This this can technically be done by storing Paths for each FileEntry in the NxSourceData.
///           - But that has some issues with figuring out the correct path because we lose the order when
///             we group the entries by block in underlying <see cref="NxRepackerBuilder"/>.
///           - Instead this would be better fixed with general purpose block deduplication by hash in packer feature.
/// </remarks>
public class NxDeduplicatingRepackerBuilder : NxRepackerBuilder
{
    private readonly Dictionary<ulong, HashSet<string>> _addedFiles = new();

    /// <summary/>
    public NxDeduplicatingRepackerBuilder() { }

    /// <summary>
    ///     Adds a file from an existing Nx archive, deduplicating based
    ///     on file hash and relative path.
    /// </summary>
    /// <param name="nxSource">Source for an underlying Nx archive.</param>
    /// <param name="header">Pre-parsed header for the given Nx archive.</param>
    /// <param name="entry">The entry for the file.</param>
    /// <returns>This builder instance.</returns>
    /// <remarks>
    ///     Deduplication is performed on a per-file basis (if hash + path match)
    /// </remarks>
    public new NxRepackerBuilder AddFileFromNxArchive(IFileDataProvider nxSource, ParsedHeader header, FileEntry entry)
    {
        var relativePath = header.Pool[entry.FilePathIndex];
        if (!_addedFiles.TryGetValue(entry.Hash, out var paths))
        {
            paths = new HashSet<string>();
            _addedFiles[entry.Hash] = paths;
        }

        // If the file hash and path already exist, we don't add it again
        return paths.Add(relativePath) ? base.AddFileFromNxArchive(nxSource, header, entry) : this;
    }

    /// <summary>
    ///     Adds multiple files from an existing Nx archive, deduplicating based
    ///     on file hash and relative path.
    /// </summary>
    /// <param name="nxSource">Source for an underlying Nx archive.</param>
    /// <param name="header">Pre-parsed header for the given Nx archive.</param>
    /// <param name="entries">A span of file entries to add from the source archive.</param>
    /// <returns>This builder instance.</returns>
    /// <remarks>
    ///     Deduplication is performed on a per-file basis (if hash + path match)
    /// </remarks>
    public new NxRepackerBuilder AddFilesFromNxArchive(IFileDataProvider nxSource, ParsedHeader header, Span<FileEntry> entries)
    {
        foreach (var entry in entries)
            AddFileFromNxArchive(nxSource, header, entry);

        return this;
    }

    /// <summary>
    ///     Adds multiple files from an existing Nx archive, deduplicating based
    ///     on file hash and relative path.
    /// </summary>
    /// <param name="nxSource">Source for an underlying Nx archive.</param>
    /// <param name="header">Pre-parsed header for the given Nx archive.</param>
    /// <param name="entries">An enumerable collection of file entries to add from the source archive.</param>
    /// <returns>This builder instance.</returns>
    /// <remarks>
    ///     Deduplication is performed on a per-file basis (if hash + path match)
    /// </remarks>
    public new NxRepackerBuilder AddFilesFromNxArchive(IFileDataProvider nxSource, ParsedHeader header, IEnumerable<FileEntry> entries)
    {
        foreach (var entry in entries)
            AddFileFromNxArchive(nxSource, header, entry);

        return this;
    }

    internal Dictionary<ulong, HashSet<string>> AddedFiles => _addedFiles;
}
