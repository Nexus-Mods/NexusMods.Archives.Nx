using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Traits;

namespace NexusMods.Archives.Nx.Tests.Utilities;

// Variation of PackerFile without required members.
public class PackerFileForBlockTesting : IHasFileSize, IHasSolidType, IHasCompressionPreference, IHasRelativePath
{
    /// <inheritdoc />
    public long FileSize { get; set; }

    /// <inheritdoc />
    public SolidPreference SolidType { get; set; }

    /// <inheritdoc />
    public CompressionPreference CompressionPreference { get; set; } = CompressionPreference.NoPreference;

    /// <inheritdoc />
    public string RelativePath { get; set; } = null!;
}
