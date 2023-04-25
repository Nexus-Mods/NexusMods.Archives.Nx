using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Traits;

namespace NexusMods.Archives.Nx.Structs;

/// <summary>
///     An individual file input into the packer.
/// </summary>
public class PackerFile : IHasRelativePath, IHasFileSize, IHasSolidType, IHasCompressionPreference, ICanProvideFileData
{
    /// <inheritdoc />
    public required IFileDataProvider FileDataProvider { get; init; }

    /// <inheritdoc />
    public string RelativePath { get; init; } = string.Empty;

    /// <inheritdoc />
    public long FileSize { get; init; }

    // Do not change default value for CompressionPreference without updating PackerFileForBlockTesting's value.

    /// <summary>
    ///     Preferred algorithm to compress the item with.<br />
    ///     Note: This setting is only honoured if <see cref="SolidPreference.NoSolid" /> is set in <see cref="SolidType" />.
    /// </summary>
    public CompressionPreference CompressionPreference { get; set; } = CompressionPreference.NoPreference;

    /// <inheritdoc />
    public SolidPreference SolidType { get; set; } = SolidPreference.Default;
}
