using FluentAssertions;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Packing;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class ChunkedFileDeduplicationTests
{
    [Fact]
    public void DeduplicateChunkedBlocks_IdenticalFiles_HaveSameStartingBlockIndex()
    {
        // Arrange
        var fileContent = PackingTests.MakeDummyFile(2 * 1024 * 1024); // 2 MB file

        var packerBuilder = new NxPackerBuilder();
        packerBuilder.WithChunkSize(1024 * 1024); // 1 MB chunks
        packerBuilder.WithChunkedDeduplication();

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
        fileEntries[0].Entry.Hash.Should().NotBe(0);
        fileEntries[1].Entry.Hash.Should().Be(fileEntries[0].Entry.Hash);
        fileEntries[1].Entry.FirstBlockIndex.Should().Be(fileEntries[0].Entry.FirstBlockIndex);
        fileEntries[0].Entry.DecompressedSize.Should().Be(2 * 1024 * 1024);
        fileEntries[1].Entry.DecompressedSize.Should().Be(2 * 1024 * 1024);

        // Extract and verify content
        unpackerBuilder.AddFilesWithArrayOutput(fileEntries, out var extractedFiles);
        unpackerBuilder.Extract();

        extractedFiles.Length.Should().Be(2);
        extractedFiles[0].Data.Should().Equal(fileContent);
        extractedFiles[1].Data.Should().Equal(fileContent);
    }
}
