using AutoFixture;
using AutoFixture.Xunit2;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Utilities;

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
        var extracted = unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(), new UnpackerSettings() { MaxNumThreads = 1 }); // 1 = easier to debug.
        
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
        var extracted = unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(), new UnpackerSettings() { MaxNumThreads = 1 }); // 1 = easier to debug.
        
        // Verify data.
        AssertExtracted(extracted);
    }
    
    [Theory]
    [AutoData]
    public void Can_Pack_And_Unpack_WithChunkedOnlyBlocks(IFixture fixture)
    {
        // Act
        var files = GetRandomDummyFiles(fixture, 128, 4194305, 16777220, out var settings);
        NxPacker.Pack(files, settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);
        
        // Test succeeds if it doesn't throw.
        var unpacker = new NxUnpacker(streamProvider);
        var extracted = unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(), new UnpackerSettings() { MaxNumThreads = 1 }); // 1 = easier to debug.
        
        // Verify data.
        AssertExtracted(extracted);
    }

    private PackerFile[] GetRandomDummyFiles(IFixture fixture, int numFiles, int minFileSize, int maxFileSize, out PackerSettings settings)
    {
        var output = new MemoryStream();
        settings = new PackerSettings
        {
            Output = output,
            BlockSize = 32767,
            ChunkSize = 4194304,
            MaxNumThreads = 1 // easier to debug.
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
        foreach (var item in extracted)
        {
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
