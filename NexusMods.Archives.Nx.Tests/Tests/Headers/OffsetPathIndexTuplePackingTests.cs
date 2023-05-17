using FluentAssertions;
using NexusMods.Archives.Nx.Headers.Native.Structs;
using NexusMods.Archives.Nx.Tests.Attributes;
using NexusMods.Archives.Nx.Tests.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Headers;

/// <summary>
///     Tests for bitpacked OffsetPathIndexTuple.
/// </summary>
public class OffsetPathIndexTuplePackingTests
{
    [Fact]
    public unsafe void IsCorrectSizeBytes() => sizeof(OffsetPathIndexTuple).Should().Be(OffsetPathIndexTuple.SizeBytes);

    /// <summary>
    ///     Version and Block Size are packed together, this test validates that they don't affect each other.
    /// </summary>
    [Theory]
    [AutoNativeHeaders]
    public void CanBePacked_ValuesDontOverlap(OffsetPathIndexTuple tuple)
    {
        // Note: This method tests all valid values for Version and BlockSize. 192 total loop iterations.
        foreach (var blockOffset in Permutations.GetBitPackingOverlapTestValues(26))
            foreach (var pathIndex in Permutations.GetBitPackingOverlapTestValues(20))
                foreach (var blockIndex in Permutations.GetBitPackingOverlapTestValues(18))
                {
                    // Test all 3 possibilities for overlaps
                    // A & C
                    PackingTestHelpers.TestPackedProperties(
                        ref tuple,
                        (ref OffsetPathIndexTuple instance, long value) => instance.DecompressedBlockOffset = (int)value,
                        (ref OffsetPathIndexTuple instance) => instance.DecompressedBlockOffset,
                        (ref OffsetPathIndexTuple instance, long value) => instance.FirstBlockIndex = (int)value,
                        (ref OffsetPathIndexTuple instance) => instance.FirstBlockIndex,
                        blockOffset,
                        blockIndex
                    );

                    // A & B
                    PackingTestHelpers.TestPackedProperties(
                        ref tuple,
                        (ref OffsetPathIndexTuple instance, long value) => instance.DecompressedBlockOffset = (int)value,
                        (ref OffsetPathIndexTuple instance) => instance.DecompressedBlockOffset,
                        (ref OffsetPathIndexTuple instance, long value) => instance.FilePathIndex = (int)value,
                        (ref OffsetPathIndexTuple instance) => instance.FilePathIndex,
                        blockOffset,
                        pathIndex
                    );

                    // B & C
                    PackingTestHelpers.TestPackedProperties(
                        ref tuple,
                        (ref OffsetPathIndexTuple instance, long value) => instance.FilePathIndex = (int)value,
                        (ref OffsetPathIndexTuple instance) => instance.FilePathIndex,
                        (ref OffsetPathIndexTuple instance, long value) => instance.FirstBlockIndex = (int)value,
                        (ref OffsetPathIndexTuple instance) => instance.FirstBlockIndex,
                        pathIndex,
                        blockIndex
                    );
                }
    }

    [Theory]
    [AutoNativeHeaders]
    public void DecompressedBlockOffsetShouldBe26Bits(OffsetPathIndexTuple tuple) => PackingTestHelpers.AssertSizeBits(
        ref tuple,
        (ref OffsetPathIndexTuple instance, long value) => instance.DecompressedBlockOffset = (int)value,
        (ref OffsetPathIndexTuple instance) => instance.DecompressedBlockOffset,
        26);

    [Theory]
    [AutoNativeHeaders]
    public void FilePathIndexShouldBe20Bits(OffsetPathIndexTuple tuple) => PackingTestHelpers.AssertSizeBits(
        ref tuple,
        (ref OffsetPathIndexTuple instance, long value) => instance.FilePathIndex = (int)value,
        (ref OffsetPathIndexTuple instance) => instance.FilePathIndex,
        20);

    [Theory]
    [AutoNativeHeaders]
    public void FirstBlockIndexShouldBe18Bits(OffsetPathIndexTuple tuple) => PackingTestHelpers.AssertSizeBits(
        ref tuple,
        (ref OffsetPathIndexTuple instance, long value) => instance.FirstBlockIndex = (int)value,
        (ref OffsetPathIndexTuple instance) => instance.FirstBlockIndex,
        18);
}
