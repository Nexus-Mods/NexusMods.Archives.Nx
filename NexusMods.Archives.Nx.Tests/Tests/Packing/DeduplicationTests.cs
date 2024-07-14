using FluentAssertions;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Packing;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class DeduplicationTests
{
    [Fact]
    public void DeduplicateChunkedBlocks_IdenticalFiles_HaveSameStartingBlockIndex()
    {
        // Arrange
        var fileContent = PackingTests.MakeDummyFile(2 * 1024 * 1024); // 2 MB file
        File.WriteAllBytes("/home/sewer/Temp/test.bin", fileContent);

        var packerBuilder = new NxPackerBuilder();
        packerBuilder.WithChunkSize(1024 * 1024); // 1 MB chunks
        packerBuilder.WithDeduplication();

        // Act
        // Add the same file twice with different names
        packerBuilder.AddFile(fileContent, new AddFileParams { RelativePath = "file1.bin" });
        packerBuilder.AddFile(fileContent, new AddFileParams { RelativePath = "file2.bin" });

        using var packedStream = packerBuilder.Build(false);
        packedStream.Position = 0;

        var unpackerBuilder = new NxUnpackerBuilder(new FromStreamProvider(packedStream));
        var fileEntries = unpackerBuilder.GetPathedFileEntries();

        // Assert
        fileEntries.Length.Should().Be(2);
        fileEntries[1].Entry.FirstBlockIndex.Should().Be(fileEntries[0].Entry.FirstBlockIndex);
    }
}