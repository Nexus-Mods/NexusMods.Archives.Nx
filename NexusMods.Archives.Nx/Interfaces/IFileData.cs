namespace NexusMods.Archives.Nx.Interfaces;

/// <summary>
///     An interface for providing access to underlying file data.
/// </summary>
public unsafe interface IFileData : IDisposable
{
    /// <summary>
    ///     Data of the underlying item.
    /// </summary>
    public byte* Data { get; }

    /// <summary>
    ///     Length of the underlying data.
    /// </summary>
    public nuint DataLength { get; }
}
