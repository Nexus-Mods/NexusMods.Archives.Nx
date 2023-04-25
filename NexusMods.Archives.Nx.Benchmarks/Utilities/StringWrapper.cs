using NexusMods.Archives.Nx.Traits;

namespace NexusMods.Archives.Nx.Benchmarks.Utilities;

public struct StringWrapper : IHasRelativePath
{
    public string RelativePath { get; init; }

    public StringWrapper(string relativePath) => RelativePath = relativePath;

    public static StringWrapper[] FromStringArray(string[] arr) => arr.Select(x => new StringWrapper(x)).ToArray();

    public override string ToString() => RelativePath;
}
