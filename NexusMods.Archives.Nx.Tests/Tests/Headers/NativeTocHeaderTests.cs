using FluentAssertions;
using NexusMods.Archives.Nx.Headers.Enums;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Tests.Attributes;
using NexusMods.Archives.Nx.Tests.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Headers;

/// <summary>
///     Asserts native Table of Contents header properties.
/// </summary>
public class NativeTocHeaderTests
{
    [Fact]
    public unsafe void IsCorrectSizeBytes() => sizeof(NativeTocHeader).Should().Be(8); // 64 bits = 8 bytes

    [Theory]
    [AutoNativeHeaders]
    public void BlockCountAndFileCountCanBePacked(NativeTocHeader header)
    {
        foreach (var fileCount in Permutations.GetBitPackingOverlapTestValues(20))
            foreach (var blockCount in Permutations.GetBitPackingOverlapTestValues(18))
                PackingTestHelpers.TestPackedProperties(
                    ref header,
                    (ref NativeTocHeader instance, long value) => instance.FileCount = (int)value,
                    (ref NativeTocHeader instance) => instance.FileCount,
                    (ref NativeTocHeader instance, long value) => instance.BlockCount = (int)value,
                    (ref NativeTocHeader instance) => instance.BlockCount,
                    fileCount,
                    blockCount
                );
    }

    [Theory]
    [AutoNativeHeaders]
    public void StringPoolSizeAndBlockCountCanBePacked(NativeTocHeader header)
    {
        foreach (var blockCount in Permutations.GetBitPackingOverlapTestValues(18))
            foreach (var stringPoolSize in Permutations.GetBitPackingOverlapTestValues(24))
                PackingTestHelpers.TestPackedProperties(
                    ref header,
                    (ref NativeTocHeader instance, long value) => instance.BlockCount = (int)value,
                    (ref NativeTocHeader instance) => instance.BlockCount,
                    (ref NativeTocHeader instance, long value) => instance.StringPoolSize = (int)value,
                    (ref NativeTocHeader instance) => instance.StringPoolSize,
                    blockCount,
                    stringPoolSize
                );
    }

    [Theory]
    [AutoNativeHeaders]
    public void StringPoolSizeAndVersionCanBePacked(NativeTocHeader header)
    {
        foreach (var stringPoolSize in Permutations.GetBitPackingOverlapTestValues(24))
            foreach (var version in Permutations.GetBitPackingOverlapTestValues(2))
                PackingTestHelpers.TestPackedProperties(
                    ref header,
                    (ref NativeTocHeader instance, long value) => instance.StringPoolSize = (int)value,
                    (ref NativeTocHeader instance) => instance.StringPoolSize,
                    (ref NativeTocHeader instance, long value) => instance.Version = (TableOfContentsVersion)value,
                    (ref NativeTocHeader instance) => (long)instance.Version,
                    stringPoolSize,
                    version
                );
    }

    [Theory]
    [AutoNativeHeaders]
    public void FileCountShouldBe20Bits(NativeTocHeader header) => PackingTestHelpers.AssertSizeBits(
        ref header,
        (ref NativeTocHeader instance, long value) => instance.FileCount = (int)value,
        (ref NativeTocHeader instance) => instance.FileCount,
        20);

    [Theory]
    [AutoNativeHeaders]
    public void BlockCountShouldBe18Bits(NativeTocHeader header) => PackingTestHelpers.AssertSizeBits(
        ref header,
        (ref NativeTocHeader instance, long value) => instance.BlockCount = (int)value,
        (ref NativeTocHeader instance) => instance.BlockCount,
        18);

    [Theory]
    [AutoNativeHeaders]
    public void StringPoolSizeShouldBe24Bits(NativeTocHeader header) => PackingTestHelpers.AssertSizeBits(
        ref header,
        (ref NativeTocHeader instance, long value) => instance.StringPoolSize = (int)value,
        (ref NativeTocHeader instance) => instance.StringPoolSize,
        24);

    [Theory]
    [AutoNativeHeaders]
    public void VersionShouldBe2Bits(NativeTocHeader header) => PackingTestHelpers.AssertSizeBits(
        ref header,
        (ref NativeTocHeader instance, long value) => instance.Version = (TableOfContentsVersion)value,
        (ref NativeTocHeader instance) => (long)instance.Version,
        2);
}
