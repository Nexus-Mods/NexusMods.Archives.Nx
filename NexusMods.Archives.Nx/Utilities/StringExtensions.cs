namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Extension methods for strings.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    ///     Normalizes the separator from '\' to '/' in-place.
    /// </summary>
    /// <param name="path">The path to mutate.</param>
    /// <returns>The original passed in string instance, but with separator changed in place.</returns>
    public static unsafe string NormalizeSeparatorInPlace(this string path)
    {
        // Replace
        fixed (char* pathPtr = path)
        {
            var pathSpan = new Span<char>(pathPtr, path.Length);
            pathSpan.Replace('\\', '/', pathSpan);
            return path;
        }
    }
}
