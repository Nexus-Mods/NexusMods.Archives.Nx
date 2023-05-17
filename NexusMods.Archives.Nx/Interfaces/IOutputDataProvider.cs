using NexusMods.Archives.Nx.Headers.Managed;

namespace NexusMods.Archives.Nx.Interfaces;

/// <summary>
///     An interface creating <see cref="IFileData" /> instances which allow the user to output information
///     for unpacking purposes.
///
///     Note: Items are disposed upon successful write to target; not explicitly by the user.
/// </summary>
public interface IOutputDataProvider : IDisposable
{
    /// <summary>
    /// The relative path to the output location.
    /// </summary>
    // ReSharper disable once UnusedMemberInSuper.Global
    public string RelativePath { get; }

    /// <summary>
    /// The entry this provider is for.
    /// </summary>
    public FileEntry Entry { get; }

    /// <summary>
    ///     Gets the output data behind this provider.
    /// </summary>
    /// <param name="start">Start offset into the file.</param>
    /// <param name="length">Length of the file.</param>
    /// <returns>Individual <see cref="IFileData"/> buffer to write decompressed data to. Make sure to dispose, e.g. with 'using' statement.</returns>
    public IFileData GetFileData(long start, uint length);
}
