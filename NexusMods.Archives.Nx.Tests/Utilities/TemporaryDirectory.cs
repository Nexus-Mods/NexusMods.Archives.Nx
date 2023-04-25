namespace NexusMods.Archives.Nx.Tests.Utilities;

/// <summary>
///     Creates a temporary folder that is disposed with the class.
/// </summary>
public class TemporaryDirectory : IDisposable
{
    /// <summary />
    public TemporaryDirectory(string? path = null)
    {
        path ??= MakeUniqueFolder(Path.GetTempPath());
        FolderPath = path;
    }

    /// <summary>
    ///     Path of the temporary folder.
    /// </summary>
    public string FolderPath { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete(FolderPath, true);
        }
        catch (Exception)
        {
            /* Ignored */
        }

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    ~TemporaryDirectory() => Dispose();

    /// <summary>
    ///     Makes a unique, empty folder inside a specified folder.
    /// </summary>
    /// <param name="folder">The path of the folder to make folder inside.</param>
    private static string MakeUniqueFolder(string folder)
    {
        string fullPath;

        do
        {
            fullPath = Path.Combine(folder, Path.GetRandomFileName());
        } while (Directory.Exists(fullPath));

        Directory.CreateDirectory(fullPath);
        return fullPath;
    }
}
