using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Packing;

/// <summary>
/// High level API for packing archives.
/// </summary>
[PublicAPI]
public class NxPackerBuilder
{
    /// <summary>
    /// Settings for the packer.
    /// </summary>
    public PackerSettings Settings { get; private set; } = new()
    {
        Output = new MemoryStream()
    };

    /// <summary>
    /// List of files to pack.
    /// </summary>
    public List<PackerFile> Files { get; private set; } = new();

    /// <summary>
    /// Adds all files under given folder to the output.
    /// </summary>
    /// <param name="folder">The folder to add items from.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder AddFolder(string folder)
    {
        Files.AddRange(FileFinder.GetFiles(folder));
        return this;
    }

    /// <summary>
    /// Adds a file to be packed.
    /// </summary>
    /// <param name="options">The options for this file.</param>
    /// <param name="data">The raw data to compress.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder AddFile(byte[] data, AddFileParams options)
    {
        var file = new PackerFile()
        {
            FileSize = data.Length,
            FileDataProvider = new FromArrayProvider { Data = data }
        };

        SetFileOptions(file, options);
        Files.Add(file);
        return this;
    }
    
    /// <summary>
    /// Adds a file to be packed.
    /// </summary>
    /// <param name="options">The options for this file.</param>
    /// <param name="stream">
    ///     The data to compress.
    ///     Starts at current stream position, ends at end of stream.
    ///     Must support seeking.
    /// </param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder AddFile(Stream stream, AddFileParams options)
    {
        var file = new PackerFile()
        {
            FileSize = stream.Length - stream.Position,
            FileDataProvider = new FromStreamProvider(stream)
        };

        SetFileOptions(file, options);
        Files.Add(file);
        return this;
    }

    /// <summary>
    /// Adds a file to be packed.
    /// </summary>
    /// <param name="length">Length of data at current stream position.</param>
    /// <param name="options">The options for this file.</param>
    /// <param name="stream">
    ///     The data to compress.
    ///     Starts at current stream position.
    ///     Must support seeking.
    /// </param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder AddFile(Stream stream, long length, AddFileParams options)
    {
        var file = new PackerFile()
        {
            FileSize = length,
            FileDataProvider = new FromStreamProvider(stream)
        };

        SetFileOptions(file, options);
        Files.Add(file);
        return this;
    }
    
    /// <summary>
    /// Adds a file to be packed.
    /// </summary>
    /// <param name="filePath">Path of the file in question.</param>
    /// <param name="options">The options for this file.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder AddFile(string filePath, AddFileParams options)
    {
        // Sanitize.
        filePath = Path.GetFullPath(filePath);
        var file = new PackerFile()
        {
            FileSize = new FileInfo(filePath).Length,
            FileDataProvider = new FromDirectoryDataProvider()
            {
                Directory = Path.GetDirectoryName(filePath)!,
                RelativePath = Path.GetFileName(filePath)
            }
        };

        SetFileOptions(file, options);
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
    /// Sets the output (archive) to a stream.
    /// </summary>
    /// <param name="progress">The callback to send the current state to.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithProgress(IProgress<double>? progress)
    {
        Settings.Progress = progress;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of threads allowed for the operation.
    /// </summary>
    /// <param name="maxNumThreads">Maximum number of threads to use.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithMaxNumThreads(int maxNumThreads)
    {
        Settings.MaxNumThreads = maxNumThreads;
        return this;
    }

    /// <summary>
    /// Sets the size of SOLID blocks; range 32767 to 67108863 (64 MiB).
    /// </summary>
    /// <param name="blockSize">Size of SOLID Block to use.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithBlockSize(int blockSize)
    {
        Settings.BlockSize = blockSize;
        return this;
    }
    
    /// <summary>
    /// Sets the size of large file chunks; range is 4194304 (4 MiB) to 536870912 (512 MiB).
    /// </summary>
    /// <param name="chunkSize">Size of large file chunks.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithChunkSize(int chunkSize)
    {
        Settings.ChunkSize = chunkSize;
        return this;
    }
    
    /// <summary>
    /// Compression level to use for ZStandard if ZStandard is used. Range: 1 - 22.
    /// </summary>
    /// <param name="zStandardLevel">Level of ZStandard compression.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithZStandardLevel(int zStandardLevel)
    {
        Settings.ZStandardLevel = zStandardLevel;
        return this;
    }
    
    /// <summary>
    /// Compression level to use for ZStandard if ZStandard is used. Range: 1 - 22.
    /// </summary>
    /// <param name="lz4Level">Level of ZStandard compression.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithLZ4Level(int lz4Level)
    {
        Settings.Lz4Level = lz4Level;
        return this;
    }
    
    /// <summary>
    /// Sets the output (archive) to a stream.
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
    /// Sets the compression algorithm used for compressing SOLID blocks.
    /// </summary>
    /// <param name="solidBlockAlgorithm">The algorithm to use for SOLID blocks.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithSolidBlockAlgorithm(CompressionPreference solidBlockAlgorithm)
    {
        Settings.SolidBlockAlgorithm = solidBlockAlgorithm;
        return this;
    }
    
    /// <summary>
    /// Sets the compression algorithm used for chunked files.
    /// </summary>
    /// <param name="chunkedFileAlgorithm">The algorithm to use for chunked files.</param>
    /// <returns>The builder.</returns>
    public NxPackerBuilder WithChunkedFileAlgorithm(CompressionPreference chunkedFileAlgorithm)
    {
        Settings.ChunkedFileAlgorithm = chunkedFileAlgorithm;
        return this;
    }

    /// <summary>
    /// Builds the archive in the configured destination.
    /// </summary>
    /// <returns>The output stream.</returns>
    public Stream Build(bool disposeOutput = true)
    {
        NxPacker.Pack(Files.ToArray(), Settings);
        if (disposeOutput)
            Settings.Output.Dispose();

        return Settings.Output;
    }

    private void DisposeExistingStream()
    {
        // This is here just in case user sets output multiple times. 
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        Settings.Output?.Dispose();
        Settings.Output = null!;
    }
}

/// <summary>
/// Parameters used for adding a file.
/// </summary>
public struct AddFileParams
{
    /// <summary>
    /// Relative path of the file inside the archive.
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
        
    /// <summary/>
    [PublicAPI]
    public AddFileParams() { }
}