using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;

namespace NexusMods.Archives.Nx.Tests.Utilities;

public class TestOutputDataProvider : IOutputDataProvider
{
    public string RelativePath { get; init; } = null!;
    public FileEntry Entry { get; init; }
    public IFileData GetFileData(long start, uint length) => throw new NotImplementedException();
    public void Dispose() { }
}
