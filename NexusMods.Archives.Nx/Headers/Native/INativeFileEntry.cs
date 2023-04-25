using NexusMods.Archives.Nx.Headers.Managed;

namespace NexusMods.Archives.Nx.Headers.Native;

/// <summary>
/// Common interface for native file entries, used for copying data.
/// </summary>
public interface INativeFileEntry
{
    /// <summary>
    /// Copy contents of the managed file entry to the native one.
    /// </summary>
    /// <param name="entry">Source entry.</param>
    public void CopyFrom(in FileEntry entry);

    /// <summary>
    /// Copy contents of the native file entry to the managed one.
    /// </summary>
    /// <param name="entry">Receiving entry.</param>
    public void CopyTo(ref FileEntry entry);
}
