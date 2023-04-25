namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Method wrappers for newer runtime features that delegate back to older approaches on unsupported runtimes.
/// </summary>
internal class Polyfills
{
    /// <summary>
    ///     Allocates an array without zero filling it.
    /// </summary>
    /// <returns></returns>
    public static T[] AllocateUninitializedArray<T>(int size)
    {
#if NET5_0_OR_GREATER
        return GC.AllocateUninitializedArray<T>(size);
#else
        return new T[size];
#endif
    }
}
