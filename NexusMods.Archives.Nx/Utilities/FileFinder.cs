using NexusMods.Archives.Nx.Structs;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.IO.Enumeration;
#endif

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Class used for finding files within a given directory.
/// </summary>
public class FileFinder
{
#if NETSTANDARD2_0
    /// <summary>
    /// Retrieves all packable files from within a given directory.
    /// </summary>
    /// <param name="directoryPath">The relative or absolute path to the directory to search.</param>
    /// <param name="searchOption">
    ///     One of the enumeration values that specifies whether the search operation
    ///     should include all subdirectories or only the current directory.
    /// </param>
    public List<PackerFile> GetFiles(string directoryPath, SearchOption searchOption =
 SearchOption.AllDirectories)
    {
        // TODO: This fallback for NS2.0 is slow.
        var results = new List<PackerFile>();
        var substringLength = directoryPath.Length + 1;
        directoryPath = Path.GetFullPath(directoryPath);
        foreach (var result in Directory.GetFiles(directoryPath, "*", searchOption))
        {
            results.Add(new PackerFile()
            {
                RelativePath = result.Substring(substringLength).NormalizeSeparatorInPlace(),
                FileSize = new FileInfo(result).Length
            });
        }
        
        return results;
    }
#endif

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
    // TODO: C Export for this GetFiles.
    /// <summary>
    ///     Retrieves all packable files from within a given directory.
    /// </summary>
    /// <param name="directoryPath">The relative or absolute path to the directory to search.</param>
    public List<PackerFile> GetFiles(string directoryPath)
    {
        return GetFiles(directoryPath, new EnumerationOptions
        {
            RecurseSubdirectories = true
        });
    }

    /// <summary>
    ///     Retrieves all packable files from within a given directory.
    /// </summary>
    /// <param name="directoryPath">The relative or absolute path to the directory to search.</param>
    /// <param name="options">
    ///     Options to use when searching for files.
    /// </param>
    public List<PackerFile> GetFiles(string directoryPath, EnumerationOptions options)
    {
        directoryPath = Path.GetFullPath(directoryPath);
        var enumerator = new PackerFileEnumerator(directoryPath, options);

        var results = new List<PackerFile>();

        // ReSharper disable once AssignNullToNotNullAttribute
        while (enumerator.MoveNext())
            results.Add(enumerator.Current);

        return results;
    }

    private class PackerFileEnumerator : FileSystemEnumerator<PackerFile>
    {
        private readonly int _substringLength;

        public PackerFileEnumerator(string directory, EnumerationOptions? options = null) : base(directory, options)
        {
            _substringLength = directory.Length + 1;
        }

        protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
        {
            return base.ShouldIncludeEntry(ref entry) && !entry.IsDirectory;
        }

        protected override PackerFile TransformEntry(ref FileSystemEntry entry)
        {
            return new PackerFile
            {
                RelativePath = entry.ToFullPath()[_substringLength..].NormalizeSeparatorInPlace(),
                FileSize = entry.Length
            };
        }
    }
#endif
}
