using NexusMods.Archives.Nx.Traits;

namespace NexusMods.Archives.Nx.Tests.Utilities;

public struct HasFileSizeWrapper : IHasFileSize, IEquatable<HasFileSizeWrapper>
{
    public long FileSize { get; init; }

    public HasFileSizeWrapper(long fileSize) => FileSize = fileSize;

    public static HasFileSizeWrapper[] FromSizeArray(long[] arr) => arr.Select(x => new HasFileSizeWrapper(x)).ToArray();

    public override string ToString() => FileSize.ToString();

    public static implicit operator HasFileSizeWrapper(long fileSize) => new(fileSize);

    public bool Equals(HasFileSizeWrapper other) => FileSize == other.FileSize;

    public override bool Equals(object? obj) => obj is HasFileSizeWrapper other && Equals(other);

    public override int GetHashCode() => FileSize.GetHashCode();
}
