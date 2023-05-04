using FluentAssertions;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Tests.Attributes;
using NexusMods.Archives.Nx.Tests.Utilities;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Utilities;

public class FileFinderTests
{
    /// <summary>
    ///     Determines if the code can properly find all files.
    /// </summary>
    [Theory]
    [AutoFileSystem]
    public void CanFindAllFiles(FileFinder finder, DummyKnownFileDirectory dummyKnownDirectory)
    {
        var files = finder.GetFiles(dummyKnownDirectory.FolderPath);
        foreach (var file in files)
        {
            var originalPath = Path.Combine(dummyKnownDirectory.FolderPath, file.RelativePath);
            File.Exists(originalPath).Should().BeTrue();
            new FileInfo(originalPath).Length.Should().Be(file.FileSize);
            file.SolidType.Should().Be(SolidPreference.Default);
            file.RelativePath.Count(x => x == '\\').Should().Be(0, "We only allow forward slashes in archives.");

            file.FileDataProvider.Should().BeOfType<FromDirectoryDataProvider>();
            var provider = (FromDirectoryDataProvider)file.FileDataProvider;
            provider.Directory.Should().Be(dummyKnownDirectory.FolderPath);
        }

        files.Count.Should().Be(DummyKnownFileDirectory.DummyFiles.Length);
    }

    /// <summary>
    ///     Determines if the code can properly find all files.
    /// </summary>
    [Theory]
    [AutoFileSystem]
    public unsafe void CanAccessFileData(FileFinder finder, DummyKnownFileDirectory dummyKnownDirectory)
    {
        var files = finder.GetFiles(dummyKnownDirectory.FolderPath);
        foreach (var file in files)
        {
            using var data = file.FileDataProvider.GetFileData(0, (uint)file.FileSize);

            // Our test files store size at offset 0.
            (*data.Data).Should().Be((byte)data.DataLength);
        }
    }
}
