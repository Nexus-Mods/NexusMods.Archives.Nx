namespace NexusMods.Archives.Nx.Enums;

/// <summary>
///     Preferred option for compression.
/// </summary>
public enum CompressionPreference : byte
{
    /// <summary>
    ///     No preference is specified.
    /// </summary>
    NoPreference = 255,

    // Note: Values below match their encoding in ToC, so we use 255 as 'none'.

    /// <summary>
    ///     Do not compress at all, copy data verbatim.
    /// </summary>
    Copy = 0,

    /// <summary>
    ///     Compress with ZStandard.
    /// </summary>
    ZStandard = 1,

    /// <summary>
    ///     Compress with LZ4.
    /// </summary>
    Lz4 = 2
}
