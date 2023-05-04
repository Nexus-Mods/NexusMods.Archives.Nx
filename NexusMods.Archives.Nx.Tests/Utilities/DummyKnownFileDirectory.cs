namespace NexusMods.Archives.Nx.Tests.Utilities;

/// <summary>
///     Represents a dummy directory with known files.
/// </summary>
public class DummyKnownFileDirectory : TemporaryDirectory
{
    /// <summary>
    ///     Dummy files stored in this directory.
    /// </summary>
    internal static readonly DummyFile[] DummyFiles =
    {
        new("1.bin", 16),
        new("2.bin", 32),
        new("3.bin", 48),
        new("1/1.bin", 64),
        new("2/2.bin", 80),
        new("3/3.bin", 96),
        new("1/1/1.bin", 112),
        new("2/2/2.bin", 128),
        new("3/3/3.bin", 144)
    };

    public DummyKnownFileDirectory(string? path = null) : base(path)
    {
        var buffer = new byte[DummyFiles.Max(x => x.FileSize)];
        foreach (var dummyFile in DummyFiles)
        {
            var target = Path.Combine(FolderPath, dummyFile.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using var file = File.Create(target);
            buffer[0] = (byte)dummyFile.FileSize;
            file.Write(buffer, 0, dummyFile.FileSize);
        }
    }

    internal record struct DummyFile(string FileName, int FileSize);
}
