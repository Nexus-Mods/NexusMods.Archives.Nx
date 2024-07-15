using FluentAssertions;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Packing;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class SolidBlockDeduplicationTests
{
    [Fact]
    public void DeduplicateSolidBlocks_IdenticalFiles_HaveSameStartingBlockIndex()
    {
        // Arrange
        var fileContent = PackingTests.MakeDummyFile(500 * 1024); // 500 KB file

        var packerBuilder = new NxPackerBuilder();
        packerBuilder.WithBlockSize(1024 * 1024); // 1 MB SOLID blocks
        packerBuilder.WithSolidDeduplication();

        // Act
        // Add the same file twice with different names
        packerBuilder.AddFile(fileContent, new AddFileParams { RelativePath = "file1.bin" });
        packerBuilder.AddFile(fileContent, new AddFileParams { RelativePath = "file2.bin" });

        using var packedStream = packerBuilder.Build(false);
        packedStream.Position = 0;

        var streamProvider = new FromStreamProvider(packedStream);
        var header = HeaderParser.ParseHeader(streamProvider);

        var unpackerBuilder = new NxUnpackerBuilder(streamProvider);
        var fileEntries = unpackerBuilder.GetPathedFileEntries();

        // Assert
        fileEntries.Length.Should().Be(2);
        fileEntries[0].Entry.FirstBlockIndex.Should().Be(fileEntries[1].Entry.FirstBlockIndex);
        fileEntries[0].Entry.DecompressedBlockOffset.Should().Be(0);
        fileEntries[1].Entry.DecompressedBlockOffset.Should().Be(0);

        // Verify content
        unpackerBuilder.AddFilesWithArrayOutput(fileEntries, out var extractedFiles);
        unpackerBuilder.Extract();

        extractedFiles.Length.Should().Be(2);
        extractedFiles[0].Data.Should().Equal(fileContent);
        extractedFiles[1].Data.Should().Equal(fileContent);

        // Verify that only one block was created
        header.Blocks.Length.Should().Be(1);
    }

    [Fact]
    public void DeduplicateSolidBlocks_DifferentFiles_HaveDifferentOffsets()
    {
        // Arrange
        var fileContent1 = PackingTests.MakeDummyFile(300 * 1024); // 300 KB file
        var fileContent2 = PackingTests.MakeDummyFile(400 * 1024); // 400 KB file

        var packerBuilder = new NxPackerBuilder();
        packerBuilder.WithBlockSize(1024 * 1024); // 1 MB SOLID blocks
        packerBuilder.WithSolidDeduplication();

        // Act
        packerBuilder.AddFile(fileContent1, new AddFileParams { RelativePath = "file1.bin" });
        packerBuilder.AddFile(fileContent2, new AddFileParams { RelativePath = "file2.bin" });

        using var packedStream = packerBuilder.Build(false);
        packedStream.Position = 0;

        var streamProvider = new FromStreamProvider(packedStream);
        var header = HeaderParser.ParseHeader(streamProvider);

        var unpackerBuilder = new NxUnpackerBuilder(streamProvider);
        var fileEntries = unpackerBuilder.GetPathedFileEntries();

        // Assert
        fileEntries.Length.Should().Be(2);
        fileEntries[0].Entry.FirstBlockIndex.Should().Be(fileEntries[1].Entry.FirstBlockIndex);
        fileEntries[0].Entry.DecompressedBlockOffset.Should().Be(0);
        fileEntries[1].Entry.DecompressedBlockOffset.Should().Be(300 * 1024);

        // Verify content
        unpackerBuilder.AddFilesWithArrayOutput(fileEntries, out var extractedFiles);
        unpackerBuilder.Extract();

        extractedFiles.Length.Should().Be(2);
        extractedFiles[0].Data.Should().BeEquivalentTo(fileContent1);
        extractedFiles[1].Data.Should().BeEquivalentTo(fileContent2);

        // Verify that only one block was created
        header.Blocks.Length.Should().Be(1);
    }
}
