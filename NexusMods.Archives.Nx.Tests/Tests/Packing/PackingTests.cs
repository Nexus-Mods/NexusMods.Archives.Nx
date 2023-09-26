using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Tests.Utilities;
using NexusMods.Hashing.xxHash64;
using Xunit.Sdk;
using Polyfills = NexusMods.Archives.Nx.Utilities.Polyfills;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

/// <summary>
///     Simple tests for packing actual .nx files.
/// </summary>
public class PackingTests
{
    [Theory]
    [AutoData]
    public NxUnpacker Can_Pack_And_Parse_Baseline(IFixture fixture)
    {
        // Act
        NxPacker.Pack(GetRandomDummyFiles(fixture, 4096, 16384, 32766, out var settings), settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);

        // Test succeeds if it doesn't throw.
        return new NxUnpacker(streamProvider);
    }

    [Theory]
    [AutoData]
    public void Can_Pack_And_Unpack_Baseline(IFixture fixture)
    {
        // Act
        var files = GetRandomDummyFiles(fixture, 4096, 65535, 65535 * 2, out var settings);
        NxPacker.Pack(files, settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);

        // Test succeeds if it doesn't throw.
        var unpacker = new NxUnpacker(streamProvider);
        var extracted =
            unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(),
                new UnpackerSettings() { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

        // Verify data.
        AssertExtracted(extracted);
    }

    [Theory]
    [AutoData]
    public void Can_Pack_And_Unpack_WithSolidOnlyBlocks(IFixture fixture)
    {
        var files = GetRandomDummyFiles(fixture, 4096, 4096, 16384, out var settings);
        NxPacker.Pack(files, settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);

        // Test succeeds if it doesn't throw.
        var unpacker = new NxUnpacker(streamProvider);
        var extracted =
            unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(),
                new UnpackerSettings() { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

        // Assert hashes are correct
        foreach (var ext in extracted)
        {
            var extractedHash = ext.Data.XxHash64();
            var expectedHash = MakeDummyFile((int)ext.Entry.DecompressedSize).XxHash64();
            extractedHash.Should().Be(expectedHash);
        }

        // Verify data.
        AssertExtracted(extracted);
    }

    /// <summary>
    /// This test catches an edge case where a SOLID block may be partially decompressed.
    /// This can happen in the scenario where only the first file of a SOLID block (offset 0) is required to be unpacked to 1 output source.
    /// </summary>
    /// <remarks>
    ///     See associated PR https://github.com/Nexus-Mods/NexusMods.Archives.Nx/pull/13 for more details.
    /// </remarks>
    [Theory]
    // [InlineAutoData(CompressionPreference.Lz4)] // Broken until PR in K4os.Compression.LZ4 is merged.
    [InlineAutoData(CompressionPreference.Copy)]
    [InlineAutoData(CompressionPreference.ZStandard)]
    public void Can_Pack_And_Unpack_FirstSolidItem(CompressionPreference solidAlgorithm, IFixture fixture)
    {
        var files = GetRandomDummyFiles(fixture, 4, 16, 128, out var settings);
        settings.BlockSize = 1048575;
        settings.SolidBlockAlgorithm = solidAlgorithm;
        NxPacker.Pack(files, settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);

        // Test succeeds if it doesn't throw.
        var unpacker = new NxUnpacker(streamProvider);
        var firstFileInSolidBlock = unpacker.GetFileEntriesRaw().ToArray().Where(x => x.DecompressedBlockOffset == 0).ToArray();
        var extracted =
            unpacker.ExtractFilesInMemory(firstFileInSolidBlock, new UnpackerSettings());

        // Assert hashes are correct
        foreach (var ext in extracted)
        {
            var extractedHash = ext.Data.XxHash64();
            var expectedHash = MakeDummyFile((int)ext.Entry.DecompressedSize).XxHash64();
            extractedHash.Should().Be(expectedHash);
        }

        // Verify data.
        AssertExtracted(extracted);
    }

    [Theory]
    [AutoData]
    public void Can_Pack_And_Unpack_WithChunkedOnlyBlocks(IFixture fixture)
    {
        // Act
        var files = GetRandomDummyFiles(fixture, 32, 1048577, 4194308, out var settings);
        NxPacker.Pack(files, settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);

        // Test succeeds if it doesn't throw.
        var unpacker = new NxUnpacker(streamProvider);
        var extracted =
            unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(),
                new UnpackerSettings() { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

        // Assert hashes are correct
        foreach (var ext in extracted)
        {
            var extractedHash = ext.Data.XxHash64();
            var expectedHash = MakeDummyFile((int)ext.Entry.DecompressedSize).XxHash64();
            extractedHash.Should().Be(expectedHash);
        }

        // Verify data.
        AssertExtracted(extracted);
    }

    [Theory]
    [AutoData]
    public void Can_Pack_And_Unpack_WithEmptyFiles(IFixture fixture)
    {
        // Act
        var files = GetRandomDummyFiles(fixture, 4096, 0, 0, out var settings);
        NxPacker.Pack(files, settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);

        // Test succeeds if it doesn't throw.
        var unpacker = new NxUnpacker(streamProvider);
        var extracted =
            unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(),
                new UnpackerSettings() { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

        // Verify data.
        AssertExtracted(extracted);
    }

    [Theory]
    [AutoData]
    public void Can_Pack_And_Unpack_EmptyArchive(IFixture fixture)
    {
        // Act
        var files = GetRandomDummyFiles(fixture, 0, 0, 0, out var settings);
        NxPacker.Pack(files, settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);

        // Test succeeds if it doesn't throw.
        var unpacker = new NxUnpacker(streamProvider);
        var extracted =
            unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(),
                new UnpackerSettings() { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

        // Verify data.
        AssertExtracted(extracted);
    }

    [Theory]
    [AutoData]
    public void Can_Pack_And_Unpack_ToDisk(IFixture fixture)
    {
        // Act
        var files = GetRandomDummyFiles(fixture, 16, 1048577, 4194308, out var settings);
        NxPacker.Pack(files, settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);

        // Test succeeds if it doesn't throw.
        using var temporaryFilePath = new TemporaryDirectory();
        var unpacker = new NxUnpacker(streamProvider);
        var extracted = unpacker.ExtractFilesToDisk(unpacker.GetFileEntriesRaw(), temporaryFilePath.FolderPath,
            new UnpackerSettings() { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

        // Verify data.
        foreach (var item in extracted)
        {
            var path = Path.Combine(temporaryFilePath.FolderPath, item.RelativePath);
            File.Exists(path);
            var data = File.ReadAllBytes(path);
            for (var x = 0; x < data.Length; x++)
            {
                // Not asserting every byte as that would be slow, only failures.
                if (data[x] != (byte)(x % 255))
                    Assert.Fail($"Data[x] is {data[x]}, Should be: {(byte)(x % 255)}");
            }
        }
    }

    private PackerFile[] GetRandomDummyFiles(IFixture fixture, int numFiles, int minFileSize, int maxFileSize, out PackerSettings settings)
    {
        var output = new MemoryStream();
        settings = new PackerSettings
        {
            Output = output,
            BlockSize = 32767,
            ChunkSize = 1048576,
            MaxNumThreads = Environment.ProcessorCount // set to 1 for debugging.
        };

        var random = new Random();
        var index = 0;
        fixture.Customize<PackerFile>(c =>
        {
            return c.FromFactory(() =>
            {
                var fileSize = random.Next(minFileSize, maxFileSize);
                return new PackerFile()
                {
                    FileSize = fileSize,
                    RelativePath = $"File_{index++}",
                    FileDataProvider = new FromArrayProvider
                    {
                        Data = MakeDummyFile(fileSize)
                    }
                };
            }).OmitAutoProperties();
        });

        return fixture.CreateMany<PackerFile>(numFiles).ToArray();
    }

    private byte[] MakeDummyFile(int length)
    {
        var result = Polyfills.AllocateUninitializedArray<byte>(length);
        for (var x = 0; x < length; x++)
            result[x] = (byte)(x % 255);

        return result;
    }

    private static void AssertExtracted(OutputArrayProvider[] extracted)
    {
        for (var index = 0; index < extracted.Length; index++)
        {
            var item = extracted[index];
            var data = item.Data;
            for (var x = 0; x < data.Length; x++)
            {
                // Not asserting every byte as that would be slow, only failures.
                if (data[x] != (byte)(x % 255))
                    Assert.Fail($"Data[x] is {data[x]}, Should be: {(byte)(x % 255)}");
            }
        }
    }
}
