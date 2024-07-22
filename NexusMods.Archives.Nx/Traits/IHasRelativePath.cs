namespace NexusMods.Archives.Nx.Traits;

/// <summary>
///     Trait for an item which contains a file path.
/// </summary>
public interface IHasRelativePath
{
    /// <summary>
    ///     Returns the relative path to the file from archive/folder root.
    /// </summary>
    public string RelativePath { get; }
}

/// <summary>
///     This is a standard wrapper around <see cref="IHasRelativePath"/>.
///
///     Use this as a return parameter if you need to return a non-virtual
///     item that implements <see cref="IHasRelativePath"/>.
/// </summary>
internal readonly struct HasRelativePathWrapper : IHasRelativePath
{
    /// <inheritdoc />
    public string RelativePath { get; }

    /// <summary/>
    private HasRelativePathWrapper(string relativePath) => RelativePath = relativePath;

    public override string ToString() => RelativePath;

    // Implicit conversion from string to HasRelativePathWrapper
    public static implicit operator HasRelativePathWrapper(string relativePath) => new(relativePath);
}
