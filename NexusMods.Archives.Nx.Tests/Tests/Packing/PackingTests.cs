using AutoFixture;
using AutoFixture.Xunit2;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Tests.Utilities;
using Xunit.Abstractions;
using Polyfills = NexusMods.Archives.Nx.Utilities.Polyfills;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

/// <summary>
///     Simple tests for packing actual .nx files.
/// </summary>
public class PackingTests
{
    // Specifically set to 1 for repro-ing the corruption bug with archive headers.
    private readonly Random _rnd = new(1);

    private readonly ITestOutputHelper _testOutputHelper;

    public PackingTests(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

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
                new UnpackerSettings { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

        // Verify data.
        AssertExtracted(extracted);
    }

    [Theory]
    [AutoData]
    public void Can_Pack_And_Unpack_WithSolidOnlyBlocks(IFixture fixture)
    {
        // Solid Blocks are LZ4 by default.
        // Act
        var files = GetRandomDummyFiles(fixture, 4096, 4096, 16384, out var settings);
        NxPacker.Pack(files, settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);

        // Test succeeds if it doesn't throw.
        var unpacker = new NxUnpacker(streamProvider);
        var extracted =
            unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(),
                new UnpackerSettings { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

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
                new UnpackerSettings { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

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
                new UnpackerSettings { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

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
            new UnpackerSettings { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

        // Verify data.
        foreach (var item in extracted)
        {
            var path = Path.Combine(temporaryFilePath.FolderPath, item.RelativePath);
            File.Exists(path);
            var data = File.ReadAllBytes(path);
            for (var x = 0; x < data.Length; x++)
                // Not asserting every byte as that would be slow, only failures.
                if (data[x] != (byte)(x % 255))
                    Assert.Fail($"Data[x] is {data[x]}, Should be: {(byte)(x % 255)}");
        }
    }

    [Theory]
    [InlineAutoData(32, 1024)]
    [InlineAutoData(64, 1024)]
    [InlineAutoData(128, 1024)]
    [InlineAutoData(256, 1024)]
    [InlineAutoData(512, 1024)]
    [InlineAutoData(1024, 1024)]
    [InlineAutoData(2048, 1024)]
    [InlineAutoData(4096, 1024)]
    public void StressTest_InMemory(int fileCount, int maxSize, IFixture fixture)
    {
        // Act
        var files = GetRandomDummyFiles(fixture, fileCount, 0, maxSize * 2, out var settings);
        NxPacker.Pack(files, settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);

        // Test succeeds if it doesn't throw.
        var unpacker = new NxUnpacker(streamProvider);
        var extracted =
            unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(),
                new UnpackerSettings { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

        // Verify data.
        AssertExtracted(extracted);
    }

    [Theory]
    [InlineAutoData(32, 1024)]
    [InlineAutoData(64, 1024)]
    [InlineAutoData(128, 1024)]
    [InlineAutoData(256, 1024)]
    [InlineAutoData(512, 1024)]
    [InlineAutoData(1024, 1024)]
    [InlineAutoData(2048, 1024)]
    [InlineAutoData(4096, 1024)]
    public void StressTest_WithStreamInput(int fileCount, int maxSize, IFixture fixture)
    {
        // Act
        var files = GetRandomDummyFiles(fixture, fileCount, 0, maxSize * 2, GetFromStreamFileDataProvider, out var settings);
        NxPacker.Pack(files, settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);

        // Test succeeds if it doesn't throw.
        var unpacker = new NxUnpacker(streamProvider);
        var extracted =
            unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(),
                new UnpackerSettings { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

        // Verify data.
        AssertExtracted(extracted);
    }

    [Theory]
    [AutoData]
    public void StressTest_ZStdCorruptionBug(IFixture fixture)
    {
        // Repro for ZStandard corruption bug when using levels >= 14.
        var minSize = 1024;
        var maxSize = 1024 * 1024 * 2;
        var totalFiles = 0;
        long totalBytes = 0;

        for (var numFiles = 100; numFiles < 4096; numFiles += 1)
        {
            if (numFiles % 10 == 0)
            {
                _testOutputHelper.WriteLine(
                    $"Count | Last Packed Num Files: {numFiles} | Total GB: {(totalBytes / 1000.0 / 1000.0 / 1000.0):#.00} | Total Files: {totalFiles}");
            }

            for (var x = 0; x < 20; x++)
            {
                // Act
                var files = GetRandomDummyFiles(fixture, numFiles, minSize, maxSize, out var settings);
                
                NxPacker.Pack(files, settings);
                settings.Output.Position = 0;
                var streamProvider = new FromStreamProvider(settings.Output);

                try
                {
                    // Test succeeds if it doesn't throw.
                    var unpacker = new NxUnpacker(streamProvider);
                    var extracted =
                        unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(),
                            new UnpackerSettings { MaxNumThreads = Environment.ProcessorCount }); // 1 = easier to debug.

                    // Verify data.
                    AssertExtracted(extracted);

                    foreach (var item in extracted)
                    {
                        totalBytes += item.Data.Length;
                        totalFiles += numFiles;
                    }
                }
                catch (Exception)
                {
                    _testOutputHelper.WriteLine($"{numFiles}, {x}");
                    settings.Output.Position = 0;
                    File.WriteAllBytes("latest.nx", ((MemoryStream)settings.Output).ToArray());
                    throw;
                }
            }
        }
    }

    private PackerFile[] GetRandomDummyFiles(IFixture fixture, int numFiles, int minFileSize, int maxFileSize, out PackerSettings settings) =>
        GetRandomDummyFiles(fixture, numFiles, minFileSize, maxFileSize, x => GetFromArrayFileDataProvider(x), out settings);

    private PackerFile[] GetRandomDummyFiles(IFixture fixture, int numFiles, int minFileSize, int maxFileSize, Func<int, IFileDataProvider> provider,
        out PackerSettings settings)
    {
        var output = new MemoryStream();
        settings = new PackerSettings
        {
            Output = output,
            BlockSize = 32767,
            ChunkSize = 1048576,
            MaxNumThreads = Environment.ProcessorCount, // set to 1 for debugging.
            SolidCompressionLevel = 3,
            ChunkedCompressionLevel = 3
        };

        var random = new Random();
        var index = 0;
        fixture.Customize<PackerFile>(c =>
        {
            return c.FromFactory(() =>
            {
                var fileSize = random.Next(minFileSize, maxFileSize);
                return new PackerFile
                {
                    FileSize = fileSize,
                    RelativePath = GetRandomHash(), // IncrementAndMod(ref index).ToString("X16"),
                    FileDataProvider = provider(fileSize)
                };
            }).OmitAutoProperties();
        });

        return fixture.CreateMany<PackerFile>(numFiles).ToArray();
    }

    private string GetRandomHash()
    {
        // We want determinism, so we use a fixed seed.
        var buf = new byte[8];
        _rnd.NextBytes(buf);
        return BitConverter.ToString(buf).Replace("-", "");
    }

    private FromArrayProvider GetFromArrayFileDataProvider(int fileSize) =>
        new()
        {
            Data = MakeDummyFile(fileSize)
        };

    private FromStreamProvider GetFromStreamFileDataProvider(int fileSize) =>
        new(new MemoryStream(MakeDummyFile(fileSize)));

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
                // Not asserting every byte as that would be slow, only failures.
                if (data[x] != (byte)(x % 255))
                    Assert.Fail($"Data[x] is {data[x]}, Should be: {(byte)(x % 255)}");
        }
    }
}
