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
    public ulong DataLength { get; }
}

/// <summary/>
// ReSharper disable once InconsistentNaming
internal static class IFileDataExtensions
{
    /// <summary>
    /// Creates a Span from the underlying data of the IFileData instance.
    /// </summary>
    /// <param name="fileData">The IFileData instance.</param>
    /// <param name="length">The length of the Span slice.</param>
    /// <returns>A Span representing the underlying data.</returns>
    public static unsafe Span<byte> AsSpan(this IFileData fileData, int length) => new(fileData.Data, length);
}
