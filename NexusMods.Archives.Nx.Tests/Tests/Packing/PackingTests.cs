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
        NxPacker.Pack(GetRandomDummyFiles(fixture, 4096, out var settings), settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);
        
        // Test succeeds if it doesn't throw.
        return new NxUnpacker(streamProvider);
    }

    [Theory]
    [AutoData]
    public NxUnpacker Can_Pack_And_Unpack_Baseline(IFixture fixture)
    {
        // Act
        NxPacker.Pack(GetRandomDummyFiles(fixture, 4096, out var settings), settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);
        
        // Test succeeds if it doesn't throw.
        var unpacker = new NxUnpacker(streamProvider);
        var extracted = unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(), new UnpackerSettings() { MaxNumThreads = 1 }); // 1 = easier to debug.
        return unpacker;
    }

    private PackerFile[] GetRandomDummyFiles(IFixture fixture, int numBlockedFiles, out PackerSettings settings)
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
                var fileSize = random.Next(100, 16384);
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
        
        return fixture.CreateMany<PackerFile>(numBlockedFiles).ToArray();
    }
    
    private byte[] MakeDummyFile(int length)
    {
        var result = Polyfills.AllocateUninitializedArray<byte>(length);
        for (var x = 0; x < length; x++)
            result[x] = (byte)(x % 255);

        return result;
    }
}
