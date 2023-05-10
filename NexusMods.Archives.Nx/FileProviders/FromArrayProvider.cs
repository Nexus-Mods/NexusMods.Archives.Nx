using NexusMods.Archives.Nx.FileProviders.FileData;
using NexusMods.Archives.Nx.Interfaces;

namespace NexusMods.Archives.Nx.FileProviders;

/// <summary>
///     File data provider that provides info from an array.
/// </summary>
public sealed class FromArrayProvider : IFileDataProvider
{
    /// <summary>
    ///     The array held by this provider.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <inheritdoc />
    public IFileData GetFileData(long start, uint length) => new ArrayFileData(Data, start, length);
}
