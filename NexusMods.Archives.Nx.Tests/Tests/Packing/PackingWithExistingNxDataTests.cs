using AutoFixture;
using AutoFixture.Xunit2;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Packing.Unpack;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class PackingWithExistingNxDataTests
{
    [Theory]
    [AutoData]
    public void PackWithExistingSolidBlock_ShouldPackAndUnpackCorrectly(IFixture fixture)
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
        var newBuilder = new NxPackerBuilder();
        newBuilder.WithMaxNumThreads(1);
        newBuilder.WithOutput(new MemoryStream());

        var solidBlock = CreateSolidBlockFromExistingNxBlock(provider, header, 0);
        newBuilder.AddSolidBlockFromExistingArchive(solidBlock);

        using var newArchive = newBuilder.Build(false);
        newArchive.Position = 0;

        // Unpack the new archive in memory
        var unpacker = new NxUnpackerBuilder(new FromStreamProvider(newArchive));
        var allFileEntries = unpacker.GetPathedFileEntries();
        Assert.True(allFileEntries.Length > 0);
        unpacker.AddFilesWithArrayOutput(allFileEntries, out var extractedFiles);
        unpacker.Extract();

        // Verify the contents of the extracted files
        PackingTests.AssertExtracted(extractedFiles);
    }

    [Theory]
    [AutoData]
    public void PackWithExistingChunkedBlock_ShouldPackAndUnpackCorrectly(IFixture fixture)
    {
        /*
            This test works by creating a Nx file with a single file that has
            been chunked into 7.5 pieces.

            We then try to create a second Nx file which is derivative from
            the chunk in the first file.
        */
        const int chunkSize = 1024 * 128;
        const int fileSize = (int)(chunkSize * 7.5);

        // Create an initial Nx archive
        var initialFiles = PackingTests.GetRandomDummyFiles(fixture, 1, fileSize, fileSize, out var settings);
        settings.ChunkSize = chunkSize;
        using var initialArchive = CreateInitialArchive(initialFiles, settings);
        initialArchive.Position = 0;

        var provider = new FromStreamProvider(initialArchive);
        var header = HeaderParser.ParseHeader(provider);

        // Create a new archive using a chunked block from the initial archive
        var newBuilder = new NxPackerBuilder();
        newBuilder.WithMaxNumThreads(1);
        newBuilder.WithOutput(new MemoryStream());

        var chunkedBlocks = CreateChunkedFileFromExistingNxBlock(provider, header, 0);
        foreach (var block in chunkedBlocks)
            newBuilder.AddChunkedFileFromExistingArchiveBlock(block);

        using var newArchive = newBuilder.Build(false);
        newArchive.Position = 0;

        // Unpack the new archive in memory
        var unpacker = new NxUnpackerBuilder(new FromStreamProvider(newArchive));
        var allFileEntries = unpacker.GetPathedFileEntries();
        unpacker.AddFilesWithArrayOutput(allFileEntries, out var extractedFiles);
        unpacker.Extract();
        Assert.True(allFileEntries.Length > 0);
        Assert.True(extractedFiles[0].Data.Length > 0);

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

    private SolidBlockFromExistingNxBlock<PackerFile> CreateSolidBlockFromExistingNxBlock(IFileDataProvider provider, ParsedHeader header,
        int blockIndex)
    {
        var block = header.Blocks[blockIndex];
        var blockOffset = header.BlockOffsets[blockIndex];
        var compression = header.BlockCompressions[blockIndex];

        var items = new List<PathedFileEntry>();
        foreach (var entry in header.Entries)
        {
            if (entry.FirstBlockIndex == blockIndex)
            {
                items.Add(new PathedFileEntry
                {
                    Entry = entry,
                    FilePath = header.Pool[entry.FilePathIndex]
                });
            }
        }

        return new SolidBlockFromExistingNxBlock<PackerFile>(
            items,
            provider,
            blockOffset,
            block.CompressedSize,
            compression
        );
    }

    private ChunkedFileFromExistingNxBlock<PackerFile>[] CreateChunkedFileFromExistingNxBlock(IFileDataProvider provider, ParsedHeader header, int fileIndex)
    {
        var entry = header.Entries[fileIndex];
        var chunkSize = header.Header.ChunkSizeBytes;
        var numChunks = entry.GetChunkCount(chunkSize);
        var result = Polyfills.AllocateUninitializedArray<ChunkedFileFromExistingNxBlock<PackerFile>>(numChunks);

        for (var chunkIndex = 0; chunkIndex < numChunks; chunkIndex++)
        {
            var blockIndex = entry.FirstBlockIndex + chunkIndex;
            var block = header.Blocks[blockIndex];
            var blockOffset = header.BlockOffsets[blockIndex];
            var compression = header.BlockCompressions[blockIndex];

            result[chunkIndex] = new ChunkedFileFromExistingNxBlock<PackerFile>(
                blockOffset,
                block.CompressedSize,
                chunkIndex,
                new ChunkedBlockFromExistingNxState
                {
                    NumChunks = numChunks,
                    NxSource = provider,
                    RelativePath = header.Pool[entry.FilePathIndex],
                    FileLength = entry.DecompressedSize,
                    FileHash = entry.Hash
                },
                compression
            );
        }

        return result;
    }
}
