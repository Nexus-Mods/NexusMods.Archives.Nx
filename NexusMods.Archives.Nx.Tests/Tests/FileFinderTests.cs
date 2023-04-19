using FluentAssertions;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Tests.Attributes;
using NexusMods.Archives.Nx.Tests.Utilities;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests;

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
        }

        files.Count.Should().Be(DummyKnownFileDirectory.DummyFiles.Length);
    }
}
