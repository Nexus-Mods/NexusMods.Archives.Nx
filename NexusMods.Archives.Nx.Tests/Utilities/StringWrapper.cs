using NexusMods.Archives.Nx.Traits;

namespace NexusMods.Archives.Nx.Tests.Utilities;

public readonly struct StringWrapper : IHasFilePath, IEquatable<StringWrapper>
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

    public static implicit operator StringWrapper(string relativePath)
    {
        return new StringWrapper(relativePath);
    }

    public bool Equals(StringWrapper other)
    {
        return RelativePath == other.RelativePath;
    }

    public override bool Equals(object? obj)
    {
        return obj is StringWrapper other && Equals(other);
    }

    public override int GetHashCode()
    {
        return RelativePath.GetHashCode();
    }
}
