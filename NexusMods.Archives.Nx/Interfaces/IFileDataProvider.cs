namespace NexusMods.Archives.Nx.Interfaces;

/// <summary>
///     An interface creating <see cref="IFileData" /> instances.
/// </summary>
public interface IFileDataProvider
{
    /// <summary>
    ///     Gets the file data behind this provider.
    /// </summary>
    /// <param name="start">Start offset into the file.</param>
    /// <param name="length">Length of the file.</param>
    /// <returns>Individual file data. This data must be disposed; e.g. with 'using' statement.</returns>
    public IFileData GetFileData(long start, uint length);
}
