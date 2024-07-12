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

    private Stream CreateInitialArchive(Span<PackerFile> files, PackerSettings settings)
    {
        var builder = new NxPackerBuilder();
        builder.WithOutput(settings.Output);
        builder.WithBlockSize(settings.BlockSize);
        builder.WithChunkSize(settings.ChunkSize);
        builder.WithMaxNumThreads(settings.MaxNumThreads);

        foreach (var file in files)
            builder.AddPackerFile(file);

        return builder.Build(false);
    }
}
