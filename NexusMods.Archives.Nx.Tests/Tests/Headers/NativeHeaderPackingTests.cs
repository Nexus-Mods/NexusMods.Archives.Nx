using FluentAssertions;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Tests.Attributes;
using NexusMods.Archives.Nx.Tests.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Headers;

/// <summary>
///     Asserts native header properties.
/// </summary>
public class NativeHeaderPackingTests
{
    [Fact]
    public unsafe void IsCorrectSizeBytes() => sizeof(NativeFileHeader).Should().Be(NativeFileHeader.SizeBytes);

    [Theory]
    [AutoNativeHeaders]
    public void CanVerifyMagic(NativeFileHeader header) => header.IsValidMagicHeader().Should().BeTrue();

    /// <summary>
    ///     Version and Block Size are packed together, this test validates that they don't affect each other.
    /// </summary>
    [Theory]
    [AutoNativeHeaders]
    public void VersionAndBlockSizeCanBePacked(NativeFileHeader header)
    {
        foreach (var currentVersion in Permutations.GetBitPackingOverlapTestValues(4))
        foreach (var currentBlockSize in Permutations.GetBitPackingOverlapTestValues(4))
            PackingTestHelpers.TestPackedProperties(
                ref header,
                (ref NativeFileHeader instance, long value) => instance.Version = (byte)value,
                (ref NativeFileHeader instance) => instance.Version,
                (ref NativeFileHeader instance, long value) => instance.BlockSize = (byte)value,
                (ref NativeFileHeader instance) => instance.BlockSize,
                currentVersion,
                currentBlockSize
            );
    }

    [Theory]
    [AutoNativeHeaders]
    public void VersionShouldBe4Bits(NativeFileHeader header) => PackingTestHelpers.AssertSizeBits(
        ref header,
        (ref NativeFileHeader instance, long value) => instance.Version = (byte)value,
        (ref NativeFileHeader instance) => instance.Version,
        4);

    [Theory]
    [AutoNativeHeaders]
    public void BlockSizeShouldBe4Bits(NativeFileHeader header) => PackingTestHelpers.AssertSizeBits(
        ref header,
        (ref NativeFileHeader instance, long value) => instance.BlockSize = (byte)value,
        (ref NativeFileHeader instance) => instance.BlockSize,
        4);

    /// <summary>
    ///     Chunk Size and Page Count are packed together, this test validates that they don't affect each other.
    /// </summary>
    [Theory]
    [AutoNativeHeaders]
    public void ChunkSizeAndPageCountCanBePacked(NativeFileHeader header)
    {
        // Note: This method tests all valid values. 65536 total loop iterations.
        foreach (var currentChunkSize in Permutations.GetBitPackingOverlapTestValues(3))
        foreach (var currentPageCount in Permutations.GetBitPackingOverlapTestValues(13))
            PackingTestHelpers.TestPackedProperties(
                ref header,
                (ref NativeFileHeader instance, long value) => instance.ChunkSize = (byte)value,
                (ref NativeFileHeader instance) => instance.ChunkSize,
                (ref NativeFileHeader instance, long value) => instance.HeaderPageCount = (ushort)value,
                (ref NativeFileHeader instance) => instance.HeaderPageCount,
                currentChunkSize,
                currentPageCount
            );
    }

    [Theory]
    [AutoNativeHeaders]
    public void ChunkSizeShouldBe3Bits(NativeFileHeader header) => PackingTestHelpers.AssertSizeBits(
        ref header,
        (ref NativeFileHeader instance, long value) => instance.ChunkSize = (byte)value,
        (ref NativeFileHeader instance) => instance.ChunkSize,
        3);

    [Theory]
    [AutoNativeHeaders]
    public void PageCountShouldBe13Bits(NativeFileHeader header) => PackingTestHelpers.AssertSizeBits(
        ref header,
        (ref NativeFileHeader instance, long value) => instance.HeaderPageCount = (ushort)value,
        (ref NativeFileHeader instance) => instance.HeaderPageCount,
        13);

    /// <summary>
    ///     Verifies that reversing endian changes expected multi-byte values.
    /// </summary>
    [Theory]
    [AutoNativeHeaders]
    public void ReverseEndian_ReversesExpectedValues(NativeFileHeader header)
    {
        // Setting values that are guaranteed not to mirror in hex
        header.Magic = 1234;
        header._largeChunkSizeAndPageCount = 5678;

        // Copy and assert they are reversed.
        var header2 = header;
        header2.ReverseEndian();

        header2.Magic.Should().NotBe(header.Magic);
        header2._largeChunkSizeAndPageCount.Should().NotBe(header._largeChunkSizeAndPageCount);

        // Now reverse again and doubly make sure
        header2.ReverseEndian();
        header2.Magic.Should().Be(header.Magic);
        header2._largeChunkSizeAndPageCount.Should().Be(header._largeChunkSizeAndPageCount);
    }

    [Theory]
    [InlineData(0, 32767)]
    [InlineData(1, 65535)]
    [InlineData(11, 67108863)]
    public void BlockSizeBytes_IsCorrectlyConverted(int rawValue, int numBytes)
    {
        var header = new NativeFileHeader();
        
        // Test Raw Setter
        header.BlockSize = (byte)rawValue;
        header.BlockSizeBytes.Should().Be(numBytes);

        // Test Get/Set
        header.BlockSizeBytes = numBytes;
        header.BlockSizeBytes.Should().Be(numBytes);
    }
    
    [Theory]
    [InlineData(0, 4194304)]
    [InlineData(1, 8388608)]
    [InlineData(7, 536870912)]
    public void ChunkSizeBytes_IsCorrectlyConverted(int rawValue, int numBytes)
    {
        var header = new NativeFileHeader();
        
        // Test Raw Setter
        header.ChunkSize = (byte)rawValue;
        header.ChunkSizeBytes.Should().Be(numBytes);

        // Test Get/Set
        header.ChunkSizeBytes = numBytes;
        header.ChunkSizeBytes.Should().Be(numBytes);
    }
    
    [Theory]
    [InlineData(1, 4096)]
    [InlineData(2, 8192)]
    [InlineData(8191, 33550336)]
    [InlineData(8192, 0)] // overflows
    [InlineData(8193, 4096)]
    public void TocPageBytes_IsCorrectlyConverted(int rawValue, int numBytes)
    {
        var header = new NativeFileHeader();
        
        // Test Raw Setter
        header.HeaderPageCount = (ushort)rawValue;
        header.HeaderPageBytes.Should().Be(numBytes);

        // Test Get/Set
        header.HeaderPageBytes = numBytes;
        header.HeaderPageBytes.Should().Be(numBytes);
    }
}
