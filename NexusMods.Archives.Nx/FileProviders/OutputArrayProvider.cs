using NexusMods.Archives.Nx.FileProviders.FileData;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.FileProviders;

/// <summary>
///     Output data provider that writes data to an array.
/// </summary>
public sealed class OutputArrayProvider : IOutputDataProvider
{
    /// <summary>
    ///     The array held by this provider.
    /// </summary>
    public byte[] Data { get; }

    /// <inheritdoc />
    public string RelativePath { get; init; }

    /// <inheritdoc />
    public FileEntry Entry { get; init; }

    /// <summary>
    ///     Initializes outputting a file to an array.
    /// </summary>
    /// <param name="relativePath">Relative path of the file.</param>
    /// <param name="entry">The entry from the archive.</param>
    public OutputArrayProvider(string relativePath, FileEntry entry)
    {
        if (entry.DecompressedSize > int.MaxValue)
            ThrowHelpers.EntryCannotFitInArray(entry);

        RelativePath = relativePath;
        Entry = entry;
        Data = GC.AllocateUninitializedArray<byte>((int)Entry.DecompressedSize, false);
    }

    /// <inheritdoc />
    public IFileData GetFileData(ulong start, ulong length) => new ArrayFileData(Data, start, length);

    /// <inheritdoc />
    public void Dispose() { }
}
