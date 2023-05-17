namespace NexusMods.Archives.Nx.Interfaces;

/// <summary>
///     An interface for providing access to underlying file data.
/// </summary>
/// <remarks>
///     For read operations where entire file is not yet available e.g. over a network; you should stall until you can
///     provide enough data to provide.
/// </remarks>
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
