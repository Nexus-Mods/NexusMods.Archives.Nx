namespace NexusMods.Archives.Nx.Enums;

/// <summary>
///     User per-file preference as to how an individual file should be handled.
/// </summary>
public enum SolidPreference : byte
{
    /// <summary>
    ///     Pack into solid block if can fit into solid block size.
    /// </summary>
    Default,

    /// <summary>
    ///     This file must be non-SOLIDly packed.
    /// </summary>
    NoSolid
}
