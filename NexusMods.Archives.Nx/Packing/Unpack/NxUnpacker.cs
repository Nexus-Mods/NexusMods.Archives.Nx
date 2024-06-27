using JetBrains.Annotations;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing.Unpack.Steps;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Packing.Unpack;

/// <summary>
///     Utility for unpacking `.nx` files.
/// </summary>
[PublicAPI]
public class NxUnpacker
{
    // At Initialization
    private ParsedHeader _nxHeader;
    private IFileDataProvider _dataProvider;

    // Current Decompression State
    private IProgress<double>? _progress;
    private int _currentNumBlocks;
    private PackerArrayPool _decompressPool = null!;

    /// <summary>
    ///     Creates a utility for unpacking archives.
    /// </summary>
    /// <param name="provider">Provides access to the underlying .nx archive.</param>
    /// <param name="hasLotsOfFiles">
    ///     This is a hint to the header parser whether the file to be parsed contains lots of individual files (100+).
    /// </param>
    public NxUnpacker(IFileDataProvider provider, bool hasLotsOfFiles = false)
    {
        _nxHeader = HeaderParser.ParseHeader(provider, hasLotsOfFiles);
        _dataProvider = provider;
    }

    /// <summary>
    ///     Retrieves all file entries from this archive.
    /// </summary>
    /// <remarks>
    ///     Do not directly modify the returned span. Make a copy.
    /// </remarks>
    /// <returns>All entries from inside the archive.</returns>
    public Span<FileEntry> GetFileEntriesRaw() => _nxHeader.Entries;

    /// <summary>
    ///     Retrieves all file entries from this archive, with their corresponding relative paths.
    /// </summary>
    /// <returns>All file entries and their corresponding file names from inside the archive.</returns>
    public PathedFileEntry[] GetPathedFileEntries()
    {
        var results = Polyfills.AllocateUninitializedArray<PathedFileEntry>(_nxHeader.Entries.Length);
        for (var x = 0; x < results.Length; x++)
        {
            ref var entry = ref _nxHeader.Entries.DangerousGetReferenceAt(x);
            results[x] = new PathedFileEntry
            {
                Entry = entry,
                FileName = _nxHeader.Pool.DangerousGetReferenceAt(entry.FilePathIndex)
            };
        }

        return results;
    }

    /// <summary>
    ///     Retrieves a file path for a given entry.
    /// </summary>
    /// <param name="entry">The entry for which to get the archived file path.</param>
    /// <returns>The archived file path.</returns>
    public string GetFilePath(FileEntry entry) => _nxHeader.Pool.DangerousGetReferenceAt(entry.FilePathIndex);

    /// <summary>
    ///     Arranges for the given files to be extracted to memory.
    /// </summary>
    /// <param name="files">The entries to be extracted.</param>
    public OutputArrayProvider[] MakeArrayOutputProviders(Span<FileEntry> files)
    {
        // Wrap entries into arrays.
        var results = Polyfills.AllocateUninitializedArray<OutputArrayProvider>(files.Length);
        for (var x = 0; x < files.Length; x++)
        {
            var entry = files[x];
            var relPath = _nxHeader.Pool[entry.FilePathIndex];
            results.DangerousGetReferenceAt(x) = new OutputArrayProvider(relPath, entry);
        }

        return results;
    }

    /// <summary>
    ///     Arranges for the given files to be extracted to disk.
    /// </summary>
    /// <param name="files">The entries to be extracted.</param>
    /// <param name="outputFolder">Folder to output items to.</param>
    public OutputFileProvider[] MakeDiskOutputProviders(Span<FileEntry> files, string outputFolder)
    {
        // Wrap entries into arrays.
        var results = Polyfills.AllocateUninitializedArray<OutputFileProvider>(files.Length);
        var filesCopy = files.ToArray();

        Parallel.ForEach(filesCopy, (entry, _, x) =>
        {
            var relPath = _nxHeader.Pool[entry.FilePathIndex];
            results.DangerousGetReferenceAt((int)x) = new OutputFileProvider(outputFolder, relPath, entry);
        });

        return results;
    }

    /// <summary>
    ///     Extracts all files from this archive in memory.
    /// </summary>
    /// <param name="files">The entries to be extracted.</param>
    /// <param name="settings">The settings for the unpacker.</param>
    public OutputArrayProvider[] ExtractFilesInMemory(Span<FileEntry> files, UnpackerSettings settings)
    {
        var results = MakeArrayOutputProviders(files);
        // ReSharper disable once CoVariantArrayConversion
        ExtractFiles(results, settings);
        return results;
    }

    /// <summary>
    ///     Extracts all files from this archive to disk.
    /// </summary>
    /// <param name="files">The entries to be extracted.</param>
    /// <param name="outputFolder">Folder to output items to.</param>
    /// <param name="settings">The settings for the unpacker.</param>
    public OutputFileProvider[] ExtractFilesToDisk(Span<FileEntry> files, string outputFolder, UnpackerSettings settings)
    {
        var results = MakeDiskOutputProviders(files, outputFolder);
        // ReSharper disable once CoVariantArrayConversion
        ExtractFiles(results, settings);
        return results;
    }

    /// <summary>
    ///     Extracts all files from this archive.
    /// </summary>
    /// <param name="outputs">The entries to be extracted.</param>
    /// <param name="settings">The settings for the unpacker.</param>
    public void ExtractFiles(IOutputDataProvider[] outputs, UnpackerSettings settings)
    {
        settings.Sanitize();
        _progress = settings.Progress;

        var blocks = MakeExtractableBlocks.Do(outputs, _nxHeader.Header.ChunkSizeBytes);
        _decompressPool = new PackerArrayPool(settings.MaxNumThreads, _nxHeader.Header.ChunkSizeBytes);
        _currentNumBlocks = blocks.Count;
        if (settings.MaxNumThreads > 1)
        {
            using var sched = new OrderedTaskScheduler(settings.MaxNumThreads);
            for (var x = 0; x < _currentNumBlocks; x++)
                Task.Factory.StartNew(ExtractBlock, blocks[x], CancellationToken.None, TaskCreationOptions.None, sched);
        }
        else
        {
            foreach (var block in blocks)
                ExtractBlock(block);
        }

        _decompressPool.Dispose(); // Let GC reclaim.
        for (var x = 0; x < outputs.Length; x++)
            outputs[x].Dispose();
    }

    private unsafe void ExtractBlock(object? state)
    {
        var extractable = (MakeExtractableBlocks.ExtractableBlock)state!;
        var blockIndex = extractable.BlockIndex;
        var chunkSize = _nxHeader.Header.ChunkSizeBytes;
        var offset = _nxHeader.BlockOffsets[blockIndex];
        var blockSize = _nxHeader.Blocks[blockIndex].CompressedSize;
        var method = _nxHeader.BlockCompressions[blockIndex];

        using var compressedBlock = _dataProvider.GetFileData(offset, (uint)blockSize);
        var outputs = extractable.Outputs;
        var canFastDecompress = outputs.Count == 1;
    fallback:
        if (canFastDecompress)
        {
            // This is a hot path in case of 1 output which starts at offset 0.
            // This is common in the case of chunked files extracted to disk.
            var output = outputs[0];
            var entry = output.Entry;
            if (entry.DecompressedBlockOffset != 0)
            {
                // This mode is only supported if start of decompressed data is at offset 0 of decompressed buffer.
                // If this is unsupported (rarely in this hot path) we go back to 'slow' approach.
                canFastDecompress = false;
                goto fallback;
            }

            // Get block index.
            var blockIndexOffset = extractable.BlockIndex - entry.FirstBlockIndex;
            var start = (long)chunkSize * blockIndexOffset;
            var decompSizeInChunk = entry.DecompressedSize - (ulong)start;
            var length = Math.Min((long)decompSizeInChunk, chunkSize);

            using var outputData = output.GetFileData(start, (uint)length);
            Compression.Decompress(method, compressedBlock.Data, blockSize, outputData.Data, (int)outputData.DataLength);
            _progress?.Report(extractable.BlockIndex / (float)_currentNumBlocks);
            return;
        }

        // This is the logic in case of multiple outputs, e.g. if user specifies an Array + File output.
        // It incurs additional memory copies, which may bottleneck when extraction is done purely in RAM.
        // Decompress the needed bytes.
        using var extractedBlock = _decompressPool.Rent(extractable.DecompressSize);
        fixed (byte* extractedPtr = extractedBlock.Span)
        {
            // Decompress all.
            Compression.Decompress(method, compressedBlock.Data, blockSize, extractedPtr, extractable.DecompressSize);

            // Copy to outputs.
            for (var x = 0; x < outputs.Count; x++)
            {
                var output = outputs[x];
                var entry = output.Entry;

                // Get block index.
                var blockIndexOffset = extractable.BlockIndex - entry.FirstBlockIndex;
                var start = (long)chunkSize * blockIndexOffset;
                var decompSizeInChunk = entry.DecompressedSize - (ulong)start;
                var length = Math.Min((long)decompSizeInChunk, chunkSize);

                using var outputData = output.GetFileData(start, (uint)length);
                Buffer.MemoryCopy(extractedPtr + entry.DecompressedBlockOffset, outputData.Data, outputData.DataLength, outputData.DataLength);
            }

            _progress?.Report(extractable.BlockIndex / (float)_currentNumBlocks);
        }
    }
}

/// <summary>
///     Represents a tuple between <see cref="FileEntry" /> and the name of the corresponding entry.
/// </summary>
[PublicAPI]
public class PathedFileEntry
{
    /// <summary>
    ///     Attaches a file entry to a name.
    /// </summary>
    public FileEntry Entry { get; init; }

    /// <summary>
    ///     Name of the file in question.
    /// </summary>
    public required string FileName { get; init; }
}
