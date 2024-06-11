using JetBrains.Annotations;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing.Unpack;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Packing;

/// <summary>
///     Constructs the building operation.
/// </summary>
[PublicAPI]
public class NxUnpackerBuilder
{
    /// <summary>
    ///     The unpacker behind the operation.
    /// </summary>
    public NxUnpacker Unpacker { get; private set; }

    /// <summary>
    ///     Settings for the unpacker.
    /// </summary>
    public UnpackerSettings Settings { get; private set; } = new();

    /// <summary>
    ///     Contains the files to be extracted with their corresponding outputs.
    /// </summary>
    public List<IOutputDataProvider> Outputs { get; private set; } = new();

    /// <summary>
    ///     The unpacker to use.
    /// </summary>
    /// <param name="provider">Provides access to the underlying .nx archive.</param>
    /// <param name="hasLotsOfFiles">
    ///     This is a hint to the header parser whether the file to be parsed contains lots of individual files (100+).
    /// </param>
    public NxUnpackerBuilder(IFileDataProvider provider, bool hasLotsOfFiles = false) => Unpacker = new NxUnpacker(provider, hasLotsOfFiles);

    /// <summary>
    ///     Retrieves all file entries from this archive.
    /// </summary>
    /// <remarks>
    ///     Do not directly modify the returned span. Make a copy.
    /// </remarks>
    public Span<FileEntry> GetFileEntriesRaw() => Unpacker.GetFileEntriesRaw();

    /// <summary>
    ///     Retrieves all file entries from this archive, with their corresponding relative paths.
    /// </summary>
    /// <returns>All file entries and their corresponding file names from inside the archive.</returns>
    public PathedFileEntry[] GetPathedFileEntries() => Unpacker.GetPathedFileEntries();

    /// <summary>
    ///     Sets the output (archive) to a stream.
    /// </summary>
    /// <param name="progress">The callback to send the current state to.</param>
    /// <returns>The builder.</returns>
    public NxUnpackerBuilder WithProgress(IProgress<double>? progress)
    {
        Settings.Progress = progress;
        return this;
    }

    /// <summary>
    ///     Sets the maximum number of threads allowed for the operation.
    /// </summary>
    /// <param name="maxNumThreads">Maximum number of threads to use.</param>
    /// <returns>The builder.</returns>
    public NxUnpackerBuilder WithMaxNumThreads(int maxNumThreads)
    {
        Settings.MaxNumThreads = maxNumThreads;
        return this;
    }

    /// <summary>
    ///     Extracts all files from this archive to memory.
    /// </summary>
    /// <param name="files">The entries to be extracted.</param>
    /// <param name="results">
    ///     The created outputs.
    ///     Upon completion of the extraction (via <see cref="Extract" /> method), the extracted
    ///     data will sit inside <see cref="OutputArrayProvider.Data" />.
    /// </param>
    /// <returns>This item.</returns>
    /// <remarks>
    ///     In order to get the extracted items, you will need to.
    /// </remarks>
    public NxUnpackerBuilder AddFilesWithArrayOutput(PathedFileEntry[] files, out OutputArrayProvider[] results) =>
        AddFilesWithArrayOutput(ToSpan(files), out results);

    /// <summary>
    ///     Extracts all files from this archive to memory.
    /// </summary>
    /// <param name="files">The entries to be extracted.</param>
    /// <param name="results">
    ///     The created outputs.
    ///     Upon completion of the extraction (via <see cref="Extract" /> method), the extracted
    ///     data will sit inside <see cref="OutputArrayProvider.Data" />.
    /// </param>
    /// <returns>This item.</returns>
    /// <remarks>
    ///     In order to get the extracted items, you will need to.
    /// </remarks>
    public NxUnpackerBuilder AddFilesWithArrayOutput(Span<FileEntry> files, out OutputArrayProvider[] results)
    {
        results = Unpacker.MakeArrayOutputProviders(files);
        Outputs.AddRange(results);
        return this;
    }

    /// <summary>
    ///     Extracts all files from this archive to disk.
    /// </summary>
    /// <param name="files">The entries to be extracted.</param>
    /// <param name="outputFolder">Folder to output items to.</param>
    public NxUnpackerBuilder AddFilesWithDiskOutput(PathedFileEntry[] files, string outputFolder)
    {
        Outputs.AddRange(Unpacker.MakeDiskOutputProviders(ToSpan(files), outputFolder));
        return this;
    }

    /// <summary>
    ///     Extracts all files from this archive to disk.
    /// </summary>
    /// <param name="files">The entries to be extracted.</param>
    /// <param name="outputFolder">Folder to output items to.</param>
    public NxUnpackerBuilder AddFilesWithDiskOutput(Span<FileEntry> files, string outputFolder)
    {
        Outputs.AddRange(Unpacker.MakeDiskOutputProviders(files, outputFolder));
        return this;
    }

    /// <summary>
    ///     Extracts the data.
    /// </summary>
    /// <returns>The outputs to which results were written to.</returns>
    public IOutputDataProvider[] Extract()
    {
        var outputs = Outputs.ToArray();
        Unpacker.ExtractFiles(outputs, Settings);
        return outputs;
    }

    private Span<FileEntry> ToSpan(PathedFileEntry[] files)
    {
        var result = Polyfills.AllocateUninitializedArray<FileEntry>(files.Length);
        for (var x = 0; x < result.Length; x++)
            result[x] = files.DangerousGetReferenceAt(x).Entry;

        return result;
    }
}
