using FluentAssertions;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Structs;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class PackerSettingsTests
{
    [Theory]
    [InlineData(536870913, 536870912)]
    [InlineData(int.MaxValue, 536870912)]
    [InlineData(4194303, 4194304)]
    [InlineData(int.MinValue, 4194304)]
    public void ChunkSize_IsClamped(int value, int expected)
    {
        var settings = new PackerSettings() { Output = Stream.Null };
        settings.ChunkSize = value;
        settings.Sanitize();
        settings.ChunkSize.Should().Be(expected);
    }
    
    [Theory]
    [InlineData(67108864, 67108863)]
    [InlineData(int.MaxValue, 67108863)]
    [InlineData(32766, 32767)]
    [InlineData(int.MinValue, 32767)]
    public void BlockSize_IsClamped(int value, int expected)
    {
        var settings = new PackerSettings() { Output = Stream.Null };
        settings.BlockSize = value;
        settings.Sanitize();
        settings.BlockSize.Should().Be(expected);
    }
    
    [Theory]
    [InlineData(23, 22)]
    [InlineData(int.MaxValue, 22)]
    [InlineData(0, 1)]
    [InlineData(int.MinValue, 1)]
    public void ZStandardLevel_IsClamped(int value, int expected)
    {
        var settings = new PackerSettings() { Output = Stream.Null };
        settings.ZStandardLevel = value;
        settings.Sanitize();
        settings.ZStandardLevel.Should().Be(expected);
    }
    
    [Theory]
    [InlineData(13, 12)]
    [InlineData(int.MaxValue, 12)]
    [InlineData(0, 1)]
    [InlineData(int.MinValue, 1)]
    public void LZ4Level_IsClamped(int value, int expected)
    {
        var settings = new PackerSettings() { Output = Stream.Null };
        settings.Lz4Level = value;
        settings.Sanitize();
        settings.Lz4Level.Should().Be(expected);
    }
    
    [Theory]
    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(0, 1)]
    [InlineData(int.MinValue, 1)]
    public void MaxNumThreads_IsClamped(int value, int expected)
    {
        var settings = new PackerSettings() { Output = Stream.Null };
        settings.MaxNumThreads = value;
        settings.Sanitize();
        settings.MaxNumThreads.Should().Be(expected);
    }
    
    [Theory]
    [InlineData(CompressionPreference.NoPreference, CompressionPreference.Lz4)] // default value
    [InlineData(unchecked((CompressionPreference)(-2)), CompressionPreference.Lz4)] // default value
    [InlineData(CompressionPreference.Lz4, CompressionPreference.Lz4)]
    [InlineData(CompressionPreference.ZStandard, CompressionPreference.ZStandard)]
    public void SolidBlockAlgorithm_IsClamped(CompressionPreference value, CompressionPreference expected)
    {
        var settings = new PackerSettings() { Output = Stream.Null };
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
        var settings = new PackerSettings() { Output = Stream.Null };
        settings.ChunkedFileAlgorithm = value;
        settings.Sanitize();
        settings.ChunkedFileAlgorithm.Should().Be(expected);
    }
}
