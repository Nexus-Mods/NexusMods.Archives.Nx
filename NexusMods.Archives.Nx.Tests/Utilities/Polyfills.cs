namespace NexusMods.Archives.Nx.Tests.Utilities;

/// <summary>
/// Backwards compatibility wrappers for older runtimes.
/// </summary>
public static class Polyfills
{
    // Needed for NET462
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
    {
#if NET462
        var hashSet = new HashSet<T>();
        foreach (var item in enumerable)
            hashSet.Add(item);

        return hashSet;
#else
        // ReSharper disable once InvokeAsExtensionMethod
        return Enumerable.ToHashSet(enumerable);
#endif
        
    }
}
