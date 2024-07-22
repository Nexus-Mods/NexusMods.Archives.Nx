using FluentAssertions;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.FileProviders.FileData;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Tests.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Providers;

public class MemoryMappedFileDataTests
{
    // Tests for some edge cases here.

    [Fact]
    public void CanMapZeroByteFile()
    {
        // Arrange
        using var tempDir = new TemporaryDirectory();
        var tempPath = Path.Combine(tempDir.FolderPath, "empty.txt");
        File.Create(tempPath).Dispose();

        // Act
        using var fileData = new MemoryMappedFileData(tempPath, 0, 0);
    }

    [Fact]
    public void CanCreateZeroSizedMappedFile()
    {
        // Arrange
        using var tempDir = new TemporaryDirectory();
        var relativePath = "empty.txt";
        var entry = new FileEntry { DecompressedSize = 0 };

        // Act
        var provider = new OutputFileProvider(tempDir.FolderPath, relativePath, entry);

        // Assert
        var fullPath = Path.Combine(tempDir.FolderPath, relativePath);
        File.Exists(fullPath).Should().BeTrue();
        new FileInfo(fullPath).Length.Should().Be(0);

        // Verify that GetFileData works for zero-sized file
        using var fileData = provider.GetFileData(0, 0);
        fileData.DataLength.Should().Be(0);
    }
}
