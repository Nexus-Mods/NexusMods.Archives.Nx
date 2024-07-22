namespace NexusMods.Archives.Nx.Interfaces;

/// <summary>
///     An interface creating <see cref="IFileData" /> instances.
///     This provides the data for an existing file, with 'start' parameter
///     of 0 corresponding to start of the file.
/// </summary>
public interface IFileDataProvider
{
    /// <summary>
    ///     Gets the file data behind this provider.
    /// </summary>
    /// <param name="start">Start offset into the file.</param>
    /// <param name="length">Length of the file.</param>
    /// <returns>Individual file data. This data must be disposed; e.g. with 'using' statement.</returns>
    public IFileData GetFileData(ulong start, ulong length);
}
