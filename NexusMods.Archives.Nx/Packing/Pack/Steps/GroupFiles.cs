using NexusMods.Archives.Nx.Traits;

namespace NexusMods.Archives.Nx.Packing.Pack.Steps;

/// <summary>
///     This is a step of the .NX packing process that involves grouping the
///     files by extension.
///
///     Grouping the files by extension, (equivalent to 7zip `qs` parameter)
///     improves compression ratio, as data between different files of same
///     type is likely to be similar.
///
///     For example, you have two text files, two images, and two audio files.
///     Provided their extension matches, they should be grouped together.
///
///     The Nx packing pipeline typically starts with the following steps:
///     - Sort Files Ascending by Size
///     - Group File by Extension (👈 This Class ‼️)
/// </summary>
internal static class GroupFiles
{
    // Note: Items inside each dictionary must preserve ascending order.
    // ReSharper disable once ParameterTypeCanBeEnumerable.Global
    /// <summary>
    ///     Groups the given files by extension.
    /// </summary>
    /// <param name="files">
    ///     The files to be grouped.
    ///     These files should be sorted by size in ascending order to optimize
    ///     compression efficiency.
    /// </param>
    internal static Dictionary<string, List<T>> Do<T>(T[] files) where T : IHasRelativePath
    {
        // Note: This could probably do with some fewer allocations, but it's okay for now.
        // Throwing in SpanOfCharDict from Sewer's VFS is probably overkill here.
        var results = new Dictionary<string, List<T>>((int)Math.Sqrt(files.Length));
        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.RelativePath);
            if (!results.TryGetValue(extension, out var items))
            {
                items = new List<T>();
                results[extension] = items;
            }

            items.Add(file);
        }

        return results;
    }
}
