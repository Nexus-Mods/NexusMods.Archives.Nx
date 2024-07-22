using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Structs;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class NxRepackerBuilderTests
{
    [Theory]
    [AutoData]
    public void RepackWithExistingSolidBlock_ShouldRepackAndUnpackCorrectly(IFixture fixture)
    {
        /*
            This test works by creating an Nx file with a single SOLID block, then
            creating a new archive with the contents of a single SOLID block
            from the first archive.
        */

        // Create an initial Nx archive
        var initialFiles = PackingTests.GetRandomDummyFiles(fixture, 64, 1024, 2048, out var settings);
        settings.BlockSize = 1048575;

        using var initialArchive = CreateInitialArchive(initialFiles, settings);
        initialArchive.Position = 0;

        var provider = new FromStreamProvider(initialArchive);
        var header = HeaderParser.ParseHeader(provider);

        // Create a new archive using a solid block from the initial archive
        var repackerBuilder = new NxRepackerBuilder();
        repackerBuilder.WithOutput(new MemoryStream());
        repackerBuilder.WithSolidDeduplication(false);

        // Add all files from the initial archive
        repackerBuilder.AddFilesFromNxArchive(provider, header, header.Entries.AsSpan());

        using var repackedArchive = repackerBuilder.Build(false);
        repackedArchive.Position = 0;

        // Unpack the new archive in memory
        var unpacker = new NxUnpackerBuilder(new FromStreamProvider(repackedArchive));
        var allFileEntries = unpacker.GetPathedFileEntries();
        allFileEntries.Length.Should().BeGreaterThan(0);
        unpacker.AddFilesWithArrayOutput(allFileEntries, out var extractedFiles);
        unpacker.Extract();

        // Verify the contents of the extracted files
        PackingTests.AssertExtracted(extractedFiles);
    }

    [Theory]
    [AutoData]
    public void RepackWithExistingChunkedBlock_ShouldRepackAndUnpackCorrectly(IFixture fixture)
    {
        const int chunkSize = 1024 * 128;
        const int fileSize = (int)(chunkSize * 7.5);

        // Create an initial Nx archive
        var initialFiles = PackingTests.GetRandomDummyFiles(fixture, 1, fileSize, fileSize, out var settings);
        settings.ChunkSize = chunkSize;
        using var initialArchive = CreateInitialArchive(initialFiles, settings);
        initialArchive.Position = 0;

        var provider = new FromStreamProvider(initialArchive);
        var header = HeaderParser.ParseHeader(provider);

        // Create a new archive using the chunked block from the initial archive
        var newBuilder = new NxRepackerBuilder();
        settings.Output = new MemoryStream();
        newBuilder.WithSettings(settings);

        // Add the chunked file from the initial archive
        newBuilder.AddFileFromNxArchive(provider, header, header.Entries[0]);

        using var newArchive = newBuilder.Build(false);
        newArchive.Position = 0;

        // Unpack the new archive in memory
        var unpacker = new NxUnpackerBuilder(new FromStreamProvider(newArchive));
        var allFileEntries = unpacker.GetPathedFileEntries();
        unpacker.AddFilesWithArrayOutput(allFileEntries, out var extractedFiles);
        unpacker.Extract();

        // Assert a file was extracted
        allFileEntries.Length.Should().BeGreaterThan(0);
        extractedFiles[0].Data.Length.Should().BeGreaterThan(0);

        // Verify the contents of the extracted file
        PackingTests.AssertExtracted(extractedFiles);
    }

    [Theory]
    [AutoData]
    public void RepackWithPartialSolidBlock_ShouldRepackAndUnpackCorrectly(IFixture fixture)
    {
        // Create an initial Nx archive with a large SOLID block
        var initialFiles = PackingTests.GetRandomDummyFiles(fixture, 64, 1024, 2048, out var settings);
        settings.BlockSize = 1048575;

        using var initialArchive = CreateInitialArchive(initialFiles, settings);
        initialArchive.Position = 0;

        var provider = new FromStreamProvider(initialArchive);
        var header = HeaderParser.ParseHeader(provider);

        // Create a new archive using *some* files from the SOLID block of the initial archive
        var repackerBuilder = new NxRepackerBuilder();
        repackerBuilder.WithOutput(new MemoryStream());

        // Add only half of the files from the initial archive
        var filesToRepack = header.Entries.Take(header.Entries.Length / 2).ToArray();
        repackerBuilder.AddFilesFromNxArchive(provider, header, filesToRepack.AsSpan());

        using var repackedArchive = repackerBuilder.Build(false);
        repackedArchive.Position = 0;

        // Unpack the new archive in memory
        var unpacker = new NxUnpackerBuilder(new FromStreamProvider(repackedArchive));
        var allFileEntries = unpacker.GetPathedFileEntries();
        allFileEntries.Length.Should().Be(filesToRepack.Length);
        unpacker.AddFilesWithArrayOutput(allFileEntries, out var extractedFiles);
        unpacker.Extract();

        // Verify the contents of the extracted files
        PackingTests.AssertExtracted(extractedFiles);
    }

    [Theory]
    [AutoData]
    public void RepackWithMixedChunkSizes_ShouldThrowException(IFixture fixture)
    {
        // Create two initial Nx archives with different chunk sizes
        var files1 = PackingTests.GetRandomDummyFiles(fixture, 2, 1024 * 1024, 2 * 1024 * 1024, out var settings1);
        settings1.ChunkSize = 1024 * 128; // 128 KB chunks

        var files2 = PackingTests.GetRandomDummyFiles(fixture, 2, 1024 * 1024, 2 * 1024 * 1024, out var settings2);
        settings2.ChunkSize = 1024 * 256; // 256 KB chunks

        using var archive1 = CreateInitialArchive(files1, settings1);
        using var archive2 = CreateInitialArchive(files2, settings2);

        archive1.Position = 0;
        archive2.Position = 0;

        var provider1 = new FromStreamProvider(archive1);
        var provider2 = new FromStreamProvider(archive2);

        var header1 = HeaderParser.ParseHeader(provider1);
        var header2 = HeaderParser.ParseHeader(provider2);

        // Create a new NxRepackerBuilder
        var repackerBuilder = new NxRepackerBuilder();
        repackerBuilder.WithOutput(new MemoryStream());

        // Add files from the first archive
        repackerBuilder.AddFilesFromNxArchive(provider1, header1, header1.Entries.AsSpan());

        // Attempt to add files from the second archive with a different chunk size
        // This should throw an ArgumentException
        Action action = () => repackerBuilder.AddFilesFromNxArchive(provider2, header2, header2.Entries.AsSpan());
        action.Should().Throw<ArgumentException>()
            .WithMessage("All chunked files must have the same chunk size.");
    }

    [Theory]
    [InlineAutoData(true)]
    [InlineAutoData(false)]
    public void RepackWithIdenticalChunkedFiles_CanRepackDeduplicatedData(bool isDeduplicatedArchive, IFixture fixture)
    {
        const int chunkSize = 1024 * 1024; // 1M chunks
        const int blockSize = chunkSize - 1; // 1M blocks
        const int fileSize = chunkSize * 3; // 3M file (3 chunks)

        // Create an initial Nx archive with two identical chunked files
        var initialFiles = PackingTests.GetRandomDummyFiles(fixture, 2, fileSize, fileSize, out var settings);
        settings.BlockSize = blockSize;
        settings.ChunkSize = chunkSize;

        using var initialArchive = CreateInitialArchive(initialFiles, settings, true, true);
        initialArchive.Position = 0;

        var provider = new FromStreamProvider(initialArchive);
        var header = HeaderParser.ParseHeader(provider);

        // Create a new archive using the chunked files from the initial archive
        var repackerBuilder = new NxRepackerBuilder();
        settings.Output = new MemoryStream();
        repackerBuilder.WithOutput(new MemoryStream());
        repackerBuilder.WithSettings(settings);
        repackerBuilder.WithChunkedDeduplication(isDeduplicatedArchive);

        // Add both files from the initial archive
        repackerBuilder.AddFilesFromNxArchive(provider, header, header.Entries.AsSpan());

        using var repackedArchive = repackerBuilder.Build(false);
        repackedArchive.Position = 0;

        // Parse the repacked archive
        var repackedProvider = new FromStreamProvider(repackedArchive);
        var repackedHeader = HeaderParser.ParseHeader(repackedProvider);

        // Assert
        repackedHeader.Entries.Length.Should().Be(2, "Both files should be present in the repacked archive");
        repackedHeader.Blocks.Length.Should().Be(6, "There should be only 3 or 6 unique chunks (depending on implementation detail)");

        var file1 = repackedHeader.Entries[0];
        var file2 = repackedHeader.Entries[1];

        // NOTE: Uncomment once deduplication's added to the repacker
        file1.Hash.Should().Be(file2.Hash, "Both files should have the same hash");
        if (isDeduplicatedArchive) // If deduplication was enabled, the files should point at the same index.
            file1.FirstBlockIndex.Should().Be(file2.FirstBlockIndex, "Both files should start at the same block");

        file1.DecompressedBlockOffset.Should().Be(file2.DecompressedBlockOffset, "Both files should have the same block offset");
        file1.DecompressedSize.Should().Be(file2.DecompressedSize, "Both files should have the same size");

        // Unpack and verify content
        var unpacker = new NxUnpackerBuilder(repackedProvider);
        var allFileEntries = unpacker.GetPathedFileEntries();
        unpacker.AddFilesWithArrayOutput(allFileEntries, out var extractedFiles);
        unpacker.Extract();

        extractedFiles.Length.Should().Be(2, "Both files should be extracted");
        extractedFiles[0].Data.Should().Equal(extractedFiles[1].Data, "The content of both files should be identical");
        extractedFiles[0].Data.Length.Should().Be(fileSize, "The extracted file size should match the original");
    }

    private Stream CreateInitialArchive(Span<PackerFile> files, PackerSettings settings, bool solidDeDuplication = false, bool chunkedDeDuplication = false)
    {
        var builder = new NxPackerBuilder();
        builder.WithOutput(settings.Output);
        builder.WithBlockSize(settings.BlockSize);
        builder.WithChunkSize(settings.ChunkSize);
        builder.WithMaxNumThreads(settings.MaxNumThreads);
        builder.WithSolidDeduplication(solidDeDuplication);
        builder.WithChunkedDeduplication(chunkedDeDuplication);

        foreach (var file in files)
            builder.AddPackerFile(file);

        return builder.Build(false);
    }
}
