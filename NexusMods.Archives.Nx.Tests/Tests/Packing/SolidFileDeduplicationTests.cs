using FluentAssertions;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Structs.Blocks;

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

    [Fact]
    public void DeduplicateSolidBlocks_ShouldShareSameBlock_WhenRepacking()
    {
        /*
            This test verifies the deduplication of entire SOLID blocks during repacking
            when deduplication is enabled.

            Summary:
            - Create an archive with 2 identical SOLID blocks, 2 files each. (No deduplication)
            - Repacks this archive with NxRepackerBuilder and SOLID block deduplication enabled.
            - Verifies that after repacking (with deduplication):
                a) All 4 files point to the same block index.
                b) There is only 1 unique block in the repacked archive.
                c) The content of all files is preserved correctly.
        */

        const int fileSize1 = 2045;
        const int fileSize2 = 2046;

        var packerBuilder = new NxRepackerBuilder();
        packerBuilder.WithBlockSize(4095); // Set block size to 4095 bytes (2^12 - 1)
        packerBuilder.WithChunkSize(1048576); // Standard chunk size (1 MB)
        packerBuilder.WithSolidDeduplication(false); // Disable deduplication for initial packing
        packerBuilder.WithMaxNumThreads(1); // preserve order when debugging

        // Add files to create two identical blocks
        var fileContent1 = PackingTests.MakeDummyFile(fileSize1);
        var fileContent2 = PackingTests.MakeDummyFile(fileSize2);
        packerBuilder.AddBlock(new SolidBlock<PackerFile>([
            MakeDummyPackerFile(fileContent1, "block1/file1.bin"),
            MakeDummyPackerFile(fileContent2, "block1/file2.bin")
        ], CompressionPreference.ZStandard));

        packerBuilder.AddBlock(new SolidBlock<PackerFile>([
            MakeDummyPackerFile(fileContent1, "block2/file1.bin"),
            MakeDummyPackerFile(fileContent2, "block2/file2.bin")
        ], CompressionPreference.ZStandard));

        using var packedStream = packerBuilder.Build(false);
        packedStream.Position = 0;

        var streamProvider = new FromStreamProvider(packedStream);
        var header = HeaderParser.ParseHeader(streamProvider);

        // Act
        // Repack the archive with deduplication enabled
        var repackerBuilder = new NxRepackerBuilder();
        repackerBuilder.WithSolidDeduplication(true);
        repackerBuilder.AddFilesFromNxArchive(streamProvider, header, header.Entries.AsSpan());
        repackerBuilder.WithMaxNumThreads(1); // debug only

        using var repackedStream = repackerBuilder.Build(false);
        repackedStream.Position = 0;

        var repackedProvider = new FromStreamProvider(repackedStream);
        var unpackerBuilder = new NxUnpackerBuilder(repackedProvider);
        var fileEntries = unpackerBuilder.GetPathedFileEntries();

        // Assert
        fileEntries.Length.Should().Be(4);

        // All files should point to the same block after deduplication
        var firstBlockIndex = fileEntries[0].Entry.FirstBlockIndex;
        foreach (var entry in fileEntries)
            entry.Entry.FirstBlockIndex.Should().Be(firstBlockIndex, $"File {entry.FilePath} should point to the same block as the first file");

        // Verify that there's only one unique block
        var repackedHeader = unpackerBuilder.Unpacker.GetParsedHeaderUnsafe();
        repackedHeader.Blocks[1].CompressedSize.Should().Be(0, "Second block should be empty after deduplication.");

        // Extract and verify content
        unpackerBuilder.AddFilesWithArrayOutput(fileEntries, out var extractedFiles);
        unpackerBuilder.Extract();

        // Group files by hash
        var filesByHash = extractedFiles
            .GroupBy(f => f.Entry.Hash)
            .ToList();

        filesByHash.Count.Should().Be(2, "There should be exactly two distinct file hashes");

        foreach (var group in filesByHash)
        {
            group.Count().Should().Be(2, $"There should be exactly two files with hash {group.Key}");

            var files = group.ToList();
            files[0].Data.Should().Equal(files[1].Data, "Files with the same hash should have identical content");
            switch (files[0].Data.Length)
            {
                case fileSize1:
                    files[0].Data.Should().Equal(fileContent1);
                    break;
                case fileSize2:
                    files[0].Data.Should().Equal(fileContent2);
                    break;
                default:
                    throw new Exception($"Unexpected file size: {files[0].Data.Length}");
            }
        }
    }

    private static PackerFile MakeDummyPackerFile(byte[] data, string relativePath)
    {
        return new PackerFile
        {
            FileDataProvider = new FromArrayProvider()
            {
                Data = data
            },
            RelativePath = relativePath,
            FileSize = data.Length,
            CompressionPreference = CompressionPreference.ZStandard,
            SolidType = SolidPreference.Default
        };
    }
}
