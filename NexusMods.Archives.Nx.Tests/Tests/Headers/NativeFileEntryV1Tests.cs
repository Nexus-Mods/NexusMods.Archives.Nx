using FluentAssertions;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Tests.Attributes;

namespace NexusMods.Archives.Nx.Tests.Tests.Headers;

public class NativeFileEntryV1Tests
{
    [Fact]
    public unsafe void IsCorrectSizeBytes() => sizeof(NativeFileEntryV1).Should().Be(NativeFileEntryV1.SizeBytes);

    // Note: No tests for packing because all packed fields are tested under OffsetPathIndexTuplePackingTests

    [Theory]
    [AutoNativeHeaders(true)]
    public void CanCopyToFromManagedEntry(NativeFileEntryV1 entry) =>
        // Note: No need to worry about difference in size because entry would already have overflown
        // when AutoFixture created the entry.
        NativeFileEntryV0Tests.TestCopyToAndFromManagedEntry(ref entry);
}
