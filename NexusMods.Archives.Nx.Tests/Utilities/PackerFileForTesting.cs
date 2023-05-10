using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Traits;

namespace NexusMods.Archives.Nx.Tests.Utilities;

// Variation of PackerFile without required members.
public class PackerFileForTesting : IHasFileSize, IHasSolidType, IHasCompressionPreference, IHasRelativePath, ICanProvideFileData
{
    /// <inheritdoc />
    public long FileSize { get; set; }

    /// <inheritdoc />
    public SolidPreference SolidType { get; set; }

    /// <inheritdoc />
    public CompressionPreference CompressionPreference { get; set; } = CompressionPreference.NoPreference;

    /// <inheritdoc />
    public string RelativePath { get; set; } = null!;

    public PackerFileForTesting() { }

    public PackerFileForTesting(string relativePath, long fileSize, SolidPreference solidType = SolidPreference.Default,
        CompressionPreference compressionPreference = CompressionPreference.NoPreference)
    {
        FileSize = fileSize;
        SolidType = solidType;
        CompressionPreference = compressionPreference;
        RelativePath = relativePath;
    }

    public IFileDataProvider FileDataProvider { get; } = null!;
}
