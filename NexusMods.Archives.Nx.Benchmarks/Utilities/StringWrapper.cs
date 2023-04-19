using NexusMods.Archives.Nx.Traits;

namespace NexusMods.Archives.Nx.Benchmarks.Utilities;

public struct StringWrapper : IHasFilePath
{
    public string RelativePath { get; init; }

    public StringWrapper(string relativePath)
    {
        RelativePath = relativePath;
    }

    public static StringWrapper[] FromStringArray(string[] arr)
    {
        return arr.Select(x => new StringWrapper(x)).ToArray();
    }

    public override string ToString()
    {
        return RelativePath;
    }
}
