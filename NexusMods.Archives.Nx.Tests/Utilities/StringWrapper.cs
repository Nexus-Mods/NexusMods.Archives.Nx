using NexusMods.Archives.Nx.Traits;

namespace NexusMods.Archives.Nx.Tests.Utilities;

public readonly struct StringWrapper : IHasRelativePath, IEquatable<StringWrapper>
{
    public string RelativePath { get; init; }

    public StringWrapper(string relativePath) => RelativePath = relativePath;

    public static StringWrapper[] FromStringArray(string[] arr) => arr.Select(x => new StringWrapper(x)).ToArray();

    public override string ToString() => RelativePath;

    public static implicit operator StringWrapper(string relativePath) => new(relativePath);

    public bool Equals(StringWrapper other) => RelativePath == other.RelativePath;

    public override bool Equals(object? obj) => obj is StringWrapper other && Equals(other);

    public override int GetHashCode() => RelativePath.GetHashCode();
}
