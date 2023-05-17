using NexusMods.Archives.Nx.Interfaces;

namespace NexusMods.Archives.Nx.Traits;

/// <summary>
///     Trait for items which can provide file data to the user.
/// </summary>
public interface ICanProvideFileData
{
    /// <summary>
    ///     Item which provides file data to the user.
    /// </summary>
    IFileDataProvider FileDataProvider { get; }
}
