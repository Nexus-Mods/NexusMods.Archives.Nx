namespace NexusMods.Archives.Nx.Tests.Utilities;

/// <summary>
///     Backwards compatibility wrappers for older runtimes.
/// </summary>
public static class Polyfills
{
    // Needed for NET462
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable) => Enumerable.ToHashSet(enumerable);
}
