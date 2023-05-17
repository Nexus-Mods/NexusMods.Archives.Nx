using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Traits;

namespace NexusMods.Archives.Nx.Benchmarks.Utilities;

// Variation of PackerFile without required members.
public class PackerFileForBenchmarking : IHasFileSize, IHasSolidType, IHasCompressionPreference, IHasRelativePath, ICanProvideFileData
{
    /// <inheritdoc />
    public long FileSize { get; set; }

    /// <inheritdoc />
    public SolidPreference SolidType { get; set; }

    /// <inheritdoc />
    public CompressionPreference CompressionPreference { get; set; } = CompressionPreference.NoPreference;

    /// <inheritdoc />
    public string RelativePath { get; set; } = null!;

    public PackerFileForBenchmarking() { }

    public PackerFileForBenchmarking(string relativePath, long fileSize, SolidPreference solidType = SolidPreference.Default,
        CompressionPreference compressionPreference = CompressionPreference.NoPreference)
    {
        FileSize = fileSize;
        SolidType = solidType;
        CompressionPreference = compressionPreference;
        RelativePath = relativePath;
    }

    public IFileDataProvider FileDataProvider { get; } = null!;
}
