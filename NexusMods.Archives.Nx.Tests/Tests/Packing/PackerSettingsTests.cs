using FluentAssertions;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Structs;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class PackerSettingsTests
{
    [Theory]
    [InlineData(1073741825, 1073741824)]
    [InlineData(int.MaxValue, 1073741824)]
    [InlineData(1048575, 1048576)]
    [InlineData(int.MinValue, 1048576)]
    public void ChunkSize_IsClamped(int chunkSize, int expected)
    {
        var settings = new PackerSettings { Output = Stream.Null };
        settings.ChunkSize = chunkSize;
        settings.Sanitize();
        settings.ChunkSize.Should().Be(expected);
    }

    [Theory]
    [InlineData(67108864, 67108863)]
    [InlineData(int.MaxValue, 67108863)]
    [InlineData(4094, 4095)]
    [InlineData(int.MinValue, 4095)]
    public void BlockSize_IsClamped(int value, int expected)
    {
        var settings = new PackerSettings { Output = Stream.Null };
        settings.BlockSize = value;
        settings.Sanitize();
        settings.BlockSize.Should().Be(expected);
    }

    [Theory]
    // Regular Values
    [InlineData(32767, 4194303)] // BlockSize is max and ChunkSize is just below minimum
    [InlineData(67108863, 67108864)] // BlockSize is max and ChunkSize is just above it
    [InlineData(32767, 4194304)] // BlockSize is min and ChunkSize is min
    [InlineData(67108862, 67108863)] // BlockSize and ChunkSize are max - 1

    // BlockSize > ChunkSize
    [InlineData(67108863, 4194304)] // BlockSize is max and ChunkSize is min
    [InlineData(4194305, 4194304)] // BlockSize is min + 1 and ChunkSize is min
    [InlineData(67108863, 67108862)] // BlockSize is max and ChunkSize is max - 1
    public void ChunkSize_MustBeGreaterThanBlockSize(int blockSize, int chunkSize)
    {
        var settings = new PackerSettings { Output = Stream.Null };
        settings.BlockSize = blockSize;
        settings.ChunkSize = chunkSize;
        settings.Sanitize();
        settings.ChunkSize.Should().BeGreaterThan(settings.BlockSize);
    }

    [Theory]
    [InlineData(23, 22)]
    [InlineData(int.MaxValue, 22)]
    [InlineData(0, 0)]
    [InlineData(int.MinValue, -5)]
    public void ZStandardLevel_IsClamped(int value, int expected)
    {
        var settings = new PackerSettings { Output = Stream.Null };
        settings.SolidCompressionLevel = value;
        settings.Sanitize();
        settings.SolidCompressionLevel.Should().Be(expected);
    }

    [Theory]
    [InlineData(13, 12)]
    [InlineData(int.MaxValue, 12)]
    [InlineData(0, 1)]
    [InlineData(int.MinValue, 1)]
    public void LZ4Level_IsClamped(int value, int expected)
    {
        var settings = new PackerSettings
        {
            Output = Stream.Null,
            ChunkedFileAlgorithm = CompressionPreference.Lz4
        };
        settings.ChunkedCompressionLevel = value;
        settings.Sanitize();
        settings.ChunkedCompressionLevel.Should().Be(expected);
    }

    [Theory]
    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(0, 1)]
    [InlineData(int.MinValue, 1)]
    public void MaxNumThreads_IsClamped(int value, int expected)
    {
        var settings = new PackerSettings { Output = Stream.Null };
        settings.MaxNumThreads = value;
        settings.Sanitize();
        settings.MaxNumThreads.Should().Be(expected);
    }

    [Theory]
    [InlineData(CompressionPreference.NoPreference, CompressionPreference.ZStandard)] // default value
    [InlineData(unchecked((CompressionPreference)(-2)), CompressionPreference.ZStandard)] // default value
    [InlineData(CompressionPreference.Lz4, CompressionPreference.Lz4)]
    [InlineData(CompressionPreference.ZStandard, CompressionPreference.ZStandard)]
    public void SolidBlockAlgorithm_IsClamped(CompressionPreference value, CompressionPreference expected)
    {
        var settings = new PackerSettings { Output = Stream.Null };
        settings.SolidBlockAlgorithm = value;
        settings.Sanitize();
        settings.SolidBlockAlgorithm.Should().Be(expected);
    }

    [Theory]
    [InlineData(CompressionPreference.NoPreference, CompressionPreference.ZStandard)] // default value
    [InlineData(unchecked((CompressionPreference)(-2)), CompressionPreference.ZStandard)] // default value
    [InlineData(CompressionPreference.Lz4, CompressionPreference.Lz4)]
    [InlineData(CompressionPreference.ZStandard, CompressionPreference.ZStandard)]
    public void ChunkedFileAlgorithm_IsClamped(CompressionPreference value, CompressionPreference expected)
    {
        var settings = new PackerSettings { Output = Stream.Null };
        settings.ChunkedFileAlgorithm = value;
        settings.Sanitize();
        settings.ChunkedFileAlgorithm.Should().Be(expected);
    }
}
