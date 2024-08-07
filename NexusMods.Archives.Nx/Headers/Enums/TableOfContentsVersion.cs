using NexusMods.Archives.Nx.Headers.Managed;

namespace NexusMods.Archives.Nx.Headers.Enums;

/// <summary>
///     Dictates the version/variant of the archive.
///     Range: 0-3
/// </summary>
public enum TableOfContentsVersion : byte
{
    /// <summary>
    ///     20 byte <see cref="FileEntry" /> with <see cref="uint" /> sizes.
    ///     1 million file limit. Covers 99.9% of the cases.
    /// </summary>
    V0 = 0,

    /// <summary>
    ///     24 byte <see cref="FileEntry" /> with <see cref="ulong" /> sizes.
    ///     1 million file limit. Covers 99.9% of the cases.
    /// </summary>
    V1 = 1,
}
