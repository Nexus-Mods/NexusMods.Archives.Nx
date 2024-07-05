using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Packing.Pack;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Packing;

/// <summary>
///     High level API for packing archives.
/// </summary>
[PublicAPI]
public class NxPackerBuilder
{
    /// <summary>
    ///     Settings for the packer.
    /// </summary>
    public PackerSettings Settings { get; private set; } = new()
    {
        Output = new MemoryStream()
    };

    /// <summary>
    ///     List of files to pack.
    /// </summary>
    public List<PackerFile> Files { get; private set; } = new();

    /// <summary>
    ///     All existing blocks that are sourced from external, existing Nx archives.
    /// </summary>
    private List<IBlock<PackerFile>> ExistingBlocks { get; } = new();

    /// <summary>
    ///     Adds all files under given folder to the output.
    /// </summary>
    /// <param name="folder">The folder to add items from.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder AddFolder(string folder)
    {
        Files.AddRange(FileFinder.GetFiles(folder));
        return this;
    }

    /// <summary>
    ///     Adds a file to be packed.
    /// </summary>
    /// <param name="options">The options for this file.</param>
    /// <param name="data">The raw data to compress.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder AddFile(byte[] data, AddFileParams options)
    {
        var file = new PackerFile
        {
            FileSize = data.Length,
            FileDataProvider = new FromArrayProvider { Data = data }
        };

        SetFileOptions(file, options);
        Files.Add(file);
        return this;
    }

    /// <summary>
    ///     Adds a file to be packed.
    /// </summary>
    /// <param name="options">The options for this file.</param>
    /// <param name="stream">
    ///     The data to compress.
    ///     Starts at current stream position, ends at end of stream.
    ///     Must support seeking.
    ///
    ///     The maximum allowed length is 2GiB.
    /// </param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder AddFile(Stream stream, AddFileParams options)
    {
        var length = stream.Length - stream.Position;
        Debug.Assert(length <= int.MaxValue, "Streams larger than 2GiB are not currently supported. Please file a ticket if this is a requirement.");

        var file = new PackerFile
        {
            FileSize = length,
            FileDataProvider = new FromStreamProvider(stream)
        };

        SetFileOptions(file, options);
        Files.Add(file);
        return this;
    }

    /// <summary>
    ///     Adds a file to be packed.
    /// </summary>
    /// <param name="length">
    ///     Length of data at current stream position.
    ///     The maximum allowed length is 2GiB.
    /// </param>
    /// <param name="options">The options for this file.</param>
    /// <param name="stream">
    ///     The data to compress.
    ///     Starts at current stream position.
    ///     Must support seeking.
    /// </param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder AddFile(Stream stream, long length, AddFileParams options)
    {
        Debug.Assert(length <= int.MaxValue, "Streams larger than 2GiB are not currently supported. Please file a ticket if this is a requirement.");

        var file = new PackerFile
        {
            FileSize = length,
            FileDataProvider = new FromStreamProvider(stream)
        };

        SetFileOptions(file, options);
        Files.Add(file);
        return this;
    }

    /// <summary>
    ///     Adds a file to be packed.
    /// </summary>
    /// <param name="filePath">Path of the file in question.</param>
    /// <param name="options">The options for this file.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder AddFile(string filePath, AddFileParams options)
    {
        // Sanitize.
        filePath = Path.GetFullPath(filePath);
        var file = new PackerFile
        {
            FileSize = new FileInfo(filePath).Length,
            FileDataProvider = new FromDirectoryDataProvider
            {
                Directory = Path.GetDirectoryName(filePath)!,
                RelativePath = Path.GetFileName(filePath)
            }
        };

        SetFileOptions(file, options);
        Files.Add(file);
        return this;
    }

    /// <summary>
    ///     Adds an already internally created <see cref="PackerFile"/> directly
    ///     to the packer input.
    /// </summary>
    /// <param name="file">The block to add to the compressor input.</param>
    public NxPackerBuilder AddPackerFile(PackerFile file)
    {
        Files.Add(file);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetFileOptions(PackerFile file, AddFileParams options)
    {
        file.CompressionPreference = options.CompressionPreference;
        file.RelativePath = options.RelativePath;
        file.SolidType = options.SolidType;
    }

    /// <summary>
    ///     Sets the output (archive) to a stream.
    /// </summary>
    /// <param name="progress">The callback to send the current state to.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithProgress(IProgress<double>? progress)
    {
        Settings.Progress = progress;
        return this;
    }

    /// <summary>
    ///     Sets the maximum number of threads allowed for the operation.
    /// </summary>
    /// <param name="maxNumThreads">Maximum number of threads to use.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithMaxNumThreads(int maxNumThreads)
    {
        Settings.MaxNumThreads = maxNumThreads;
        return this;
    }

    /// <summary>
    ///     Sets the size of SOLID blocks; range 32767 to 67108863 (64 MiB).
    /// </summary>
    /// <param name="blockSize">Size of SOLID Block to use.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithBlockSize(int blockSize)
    {
        Settings.BlockSize = blockSize;
        return this;
    }

    /// <summary>
    ///     Sets the size of large file chunks; range is 4194304 (4 MiB) to 536870912 (512 MiB).
    /// </summary>
    /// <param name="chunkSize">Size of large file chunks.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithChunkSize(int chunkSize)
    {
        Settings.ChunkSize = chunkSize;
        return this;
    }

    /// <summary>
    ///     Sets the compression level to use for SOLID data.
    ///     ZStandard has Range -5 - 22.<br />
    ///     LZ4 has Range: 1 - 12.<br />
    /// </summary>
    /// <param name="level">Level of compression.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithSolidCompressionLevel(int level)
    {
        Settings.SolidCompressionLevel = level;
        return this;
    }

    /// <summary>
    ///     Sets the compression level to use for Chunked data.
    ///     ZStandard has Range -5 - 22.<br />
    ///     LZ4 has Range: 1 - 12.<br />
    /// </summary>
    /// <param name="level">Level of compression.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithChunkedLevel(int level)
    {
        Settings.ChunkedCompressionLevel = level;
        return this;
    }

    /// <summary>
    ///     Sets the output (archive) to a stream.
    /// </summary>
    /// <param name="output">The output to place the packed archive into.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithOutput(Stream output)
    {
        DisposeExistingStream();
        Settings.Output = output;
        return this;
    }

    /// <summary>
    ///     Sets the compression preset.
    /// </summary>
    /// <param name="preset">The preset to apply.</param>
    /// <returns>The builder.</returns>
    [ExcludeFromCodeCoverage] // This can change between versions.
    public NxPackerBuilder WithPreset(PackerPreset preset)
    {
        switch (preset)
        {
            case PackerPreset.Default:
                Settings.SolidCompressionLevel = 16;
                Settings.ChunkedCompressionLevel = 9;
                break;
            case PackerPreset.RandomAccess:
                Settings.SolidCompressionLevel = -1;
                Settings.ChunkedCompressionLevel = 9;
                break;
        }

        return this;
    }

    /// <summary>
    ///     Sets the compression algorithm used for compressing SOLID blocks.
    /// </summary>
    /// <param name="solidBlockAlgorithm">The algorithm to use for SOLID blocks.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithSolidBlockAlgorithm(CompressionPreference solidBlockAlgorithm)
    {
        Settings.SolidBlockAlgorithm = solidBlockAlgorithm;
        return this;
    }

    /// <summary>
    ///     Sets the compression algorithm used for chunked files.
    /// </summary>
    /// <param name="chunkedFileAlgorithm">The algorithm to use for chunked files.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithChunkedFileAlgorithm(CompressionPreference chunkedFileAlgorithm)
    {
        Settings.ChunkedFileAlgorithm = chunkedFileAlgorithm;
        return this;
    }

    /// <summary>
    ///     Builds the archive in the configured destination.
    /// </summary>
    /// <returns>The output stream.</returns>
    public Stream Build(bool disposeOutput = true)
    {
        if (ExistingBlocks.Count > 0)
            NxPacker.PackWithExistingBlocks(Files.ToArray(), ExistingBlocks, Settings);
        else
            NxPacker.Pack(Files.ToArray(), Settings);

        if (disposeOutput)
            Settings.Output.Dispose();

        return Settings.Output;
    }

    /// <summary>
    ///     Adds a SOLID block that's backed by an existing Nx archive.
    /// </summary>
    /// <param name="block">The block to add to the compressor input.</param>
    internal NxPackerBuilder AddSolidBlockFromExistingArchive(SolidBlockFromExistingNxBlock<PackerFile> block)
    {
        ExistingBlocks.Add(block);
        return this;
    }

    /// <summary>
    ///     Adds a SOLID block that's backed by an existing Nx archive.
    /// </summary>
    /// <param name="block">The block to add to the compressor input.</param>
    internal NxPackerBuilder AddChunkedFileFromExistingArchiveBlock(ChunkedFileFromExistingNxBlock<PackerFile> block)
    {
        ExistingBlocks.Add(block);
        return this;
    }

    private void DisposeExistingStream()
    {
        // This is here just in case user sets output multiple times.
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        Settings.Output.Dispose();
        Settings.Output = null!;
    }
}

/// <summary>
///     Parameters used for adding a file.
/// </summary>
public struct AddFileParams
{
    /// <summary>
    ///     Relative path of the file inside the archive.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    ///     Preferred algorithm to compress the item with.<br />
    ///     Note: This setting is only honoured if <see cref="SolidPreference.NoSolid" /> is set in <see cref="SolidType" />.
    /// </summary>
    public CompressionPreference CompressionPreference { get; set; } = CompressionPreference.NoPreference;

    /// <summary>
    ///     Preference in terms of whether this item should be SOLID or not.
    /// </summary>
    public SolidPreference SolidType { get; set; } = SolidPreference.Default;

    /// <summary />
    [PublicAPI]
    public AddFileParams() { }
}

/// <summary>
///     The preset to apply.
/// </summary>
public enum PackerPreset
{
    /// <summary>
    ///     This preset prioritises file size for long term archival.
    /// </summary>
    Default,

    /// <summary>
    ///     This preset prioritises decompression speed for SOLID blocks.
    ///     Intended for applications such as the Nexus App.
    /// </summary>
    RandomAccess
}
