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
    [Fact]
    public void Pack_Baseline()
    {
        // Act
        var output = new MemoryStream();
        var settings = new PackerSettings
        {
            Output = output,
            BlockSize = 32767,
            ChunkSize = 4194304,
            MaxNumThreads = 1 // easier to debug.
        };

        NxPacker.Pack(new PackerFile[]
        {
            new()
            {
                FileSize = 5000,
                RelativePath = "Block0File0",
                FileDataProvider = new FromArrayProvider
                {
                    Data = MakeDummyFile(5000)
                }
            },
            new()
            {
                FileSize = 25000,
                RelativePath = "Block0File1",
                FileDataProvider = new FromArrayProvider
                {
                    Data = MakeDummyFile(25000)
                }
            },
            new()
            {
                FileSize = 2000,
                RelativePath = "Block0File2",
                FileDataProvider = new FromArrayProvider
                {
                    Data = MakeDummyFile(2000)
                }
            },
            new()
            {
                FileSize = 8000000L,
                RelativePath = "ChunkedFile",
                FileDataProvider = new FromArrayProvider
                {
                    Data = MakeDummyFile(8000000)
                }
            }
        }, settings);

        // Get result
        File.WriteAllBytes("output.bin", output.ToArray());
    }

    private byte[] MakeDummyFile(int length)
    {
        var result = Polyfills.AllocateUninitializedArray<byte>(length);
        for (var x = 0; x < length; x++)
            result[x] = (byte)(x % 255);

        return result;
    }
}
