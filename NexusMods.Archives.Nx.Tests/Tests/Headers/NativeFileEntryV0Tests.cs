using FluentAssertions;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Tests.Attributes;

namespace NexusMods.Archives.Nx.Tests.Tests.Headers;

public class NativeFileEntryV0Tests
{
    [Fact]
    public unsafe void IsCorrectSizeBytes() => sizeof(NativeFileEntryV0).Should().Be(NativeFileEntryV0.SizeBytes);

    // Note: No tests for packing because all packed fields are tested under OffsetPathIndexTuplePackingTests

    [Theory]
    [AutoNativeHeaders(true)]
    public void CanCopyToFromManagedEntry(NativeFileEntryV0 entry) =>
        // Note: No need to worry about difference in size because entry would already have overflown
        // when AutoFixture created the entry.
        TestCopyToAndFromManagedEntry(ref entry);

    internal static void TestCopyToAndFromManagedEntry<T>(ref T entry) where T : INativeFileEntry, new()
    {
        // Note: No need to worry about difference in size because entry would already have overflown
        // when AutoFixture created the entry.

        var newEntry = new T();
        var managed = new FileEntry();

        // Do a round trip copy, and compare newEntry with oldEntry.
        // If both are equal, the copy operation is successful.
        entry.CopyTo(ref managed);
        newEntry.CopyFrom(managed);

        newEntry.Should().BeEquivalentTo(entry);
    }
}
