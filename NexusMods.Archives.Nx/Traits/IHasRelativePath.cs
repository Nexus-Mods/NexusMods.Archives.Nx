namespace NexusMods.Archives.Nx.Traits;

/// <summary>
///     Trait for an item which contains a file path.
/// </summary>
public interface IHasRelativePath
{
    /// <summary>
    ///     Returns the relative path to the file from archive/folder root.
    /// </summary>
    public string RelativePath { get; }
}
