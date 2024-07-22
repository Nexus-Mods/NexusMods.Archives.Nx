using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.FileProviders.FileData;
using NexusMods.Archives.Nx.Interfaces;

namespace NexusMods.Archives.Nx.FileProviders;

/// <summary>
///     File data provider that provides data from a specified file path.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage] // Copy of FromDirectoryDataProvider.cs
public sealed class FromFilePathProvider : IFileDataProvider
{
    /// <summary>
    ///     The full path to the file from which the data will be fetched.
    /// </summary>
    public required string FilePath { get; init; }

    /// <inheritdoc />
    public IFileData GetFileData(ulong start, ulong length) => new MemoryMappedFileData(FilePath, start, length, true);
}
