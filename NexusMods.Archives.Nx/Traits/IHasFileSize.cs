namespace NexusMods.Archives.Nx.Traits;

/// <summary>
///     Used for items which can specify a file size.
/// </summary>
public interface IHasFileSize
{
    /// <summary>
    ///     Size of the item in bytes.
    /// </summary>
    public long FileSize { get; }
}
