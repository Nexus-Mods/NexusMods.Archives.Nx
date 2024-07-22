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
    ///     Version and Chunk Size are packed together, this test validates that they don't affect each other.
    /// </summary>
    [Theory]
    [AutoNativeHeaders]
    public void VersionAndChunkSizeCanBePacked(NativeFileHeader header)
    {
        foreach (var currentVersion in Permutations.GetBitPackingOverlapTestValues(7))
            foreach (var currentChunkSize in Permutations.GetBitPackingOverlapTestValues(5))
                PackingTestHelpers.TestPackedProperties(
                    ref header,
                    (ref NativeFileHeader instance, long value) => instance.Version = (byte)value,
                    (ref NativeFileHeader instance) => instance.Version,
                    (ref NativeFileHeader instance, long value) => instance.ChunkSize = (byte)value,
                    (ref NativeFileHeader instance) => instance.ChunkSize,
                    currentVersion,
                    currentChunkSize
                );
    }

    [Theory]
    [AutoNativeHeaders]
    public void VersionShouldBe7Bits(NativeFileHeader header) => PackingTestHelpers.AssertSizeBits(
        ref header,
        (ref NativeFileHeader instance, long value) => instance.Version = (byte)value,
        (ref NativeFileHeader instance) => instance.Version,
        7);

    [Theory]
    [AutoNativeHeaders]
    public void ChunkSizeShouldBe5Bits(NativeFileHeader header) => PackingTestHelpers.AssertSizeBits(
        ref header,
        (ref NativeFileHeader instance, long value) => instance.ChunkSize = (byte)value,
        (ref NativeFileHeader instance) => instance.ChunkSize,
        5);

    /// <summary>
    ///     Header Page Count and Feature Flags are packed together, this test validates that they don't affect each other.
    /// </summary>
    [Theory]
    [AutoNativeHeaders]
    public void HeaderPageCountAndFeatureFlagsCanBePacked(NativeFileHeader header)
    {
        foreach (var currentPageCount in Permutations.GetBitPackingOverlapTestValues(16))
            foreach (var currentFeatureFlags in Permutations.GetBitPackingOverlapTestValues(4))
                PackingTestHelpers.TestPackedProperties(
                    ref header,
                    (ref NativeFileHeader instance, long value) => instance.HeaderPageCount = (ushort)value,
                    (ref NativeFileHeader instance) => instance.HeaderPageCount,
                    (ref NativeFileHeader instance, long value) => instance.FeatureFlags = (byte)value,
                    (ref NativeFileHeader instance) => instance.FeatureFlags,
                    currentPageCount,
                    currentFeatureFlags
                );
    }

    [Theory]
    [AutoNativeHeaders]
    public void HeaderPageCountShouldBe16Bits(NativeFileHeader header) => PackingTestHelpers.AssertSizeBits(
        ref header,
        (ref NativeFileHeader instance, long value) => instance.HeaderPageCount = (ushort)value,
        (ref NativeFileHeader instance) => instance.HeaderPageCount,
        16);

    [Theory]
    [AutoNativeHeaders]
    public void FeatureFlagsShouldBe4Bits(NativeFileHeader header) => PackingTestHelpers.AssertSizeBits(
        ref header,
        (ref NativeFileHeader instance, long value) => instance.FeatureFlags = (byte)value,
        (ref NativeFileHeader instance) => instance.FeatureFlags,
        4);

    /// <summary>
    ///     Verifies that reversing endian changes expected multi-byte values.
    /// </summary>
    [Theory]
    [AutoNativeHeaders]
    public void ReverseEndian_ReversesExpectedValues(NativeFileHeader header)
    {
        // Setting values that are guaranteed not to mirror in hex
        header.Magic = 1234;
        header._headerData = 5678;

        // Copy and assert they are reversed.
        var header2 = header;
        header2.ReverseEndian();

        header2.Magic.Should().NotBe(header.Magic);
        header2._headerData.Should().NotBe(header._headerData);

        // Now reverse again and doubly make sure
        header2.ReverseEndian();
        header2.Magic.Should().Be(header.Magic);
        header2._headerData.Should().Be(header._headerData);
    }

    [Theory]
    [InlineData(0, NativeFileHeader.BaseChunkSize)]
    [InlineData(1, NativeFileHeader.BaseChunkSize * 2)]
    [InlineData(15, NativeFileHeader.BaseChunkSize * 32768)]
    [InlineData(21, NativeFileHeader.BaseChunkSize * 2097152)]
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
    [InlineData(1, NativeConstants.HeaderPageSize)]
    [InlineData(2, NativeConstants.HeaderPageSize * 2)]
    [InlineData(65535, NativeConstants.HeaderPageSize * 65535)]
    public void HeaderPageBytes_IsCorrectlyConverted(int rawValue, int numBytes)
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
