using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Traits;

namespace NexusMods.Archives.Nx.Structs;

/// <summary>
///     An individual file input into the packer.
/// </summary>
public class PackerFile : IHasFilePath
{
    /// <summary>
    ///     Size of the file in bytes.
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    ///     Preference in terms of whether this item should be SOLID or not.
    /// </summary>
    public SolidPreference SolidType { get; set; } = SolidPreference.Default;

    /// <summary>
    ///     Relative path of the file from within the archive.
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;
}
