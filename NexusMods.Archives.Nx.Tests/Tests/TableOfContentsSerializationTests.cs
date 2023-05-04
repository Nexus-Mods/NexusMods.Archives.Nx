using AutoFixture;
using FluentAssertions;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Enums;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Tests.Attributes;
using NexusMods.Archives.Nx.Tests.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests;

/// <summary>
///     Tests involving the serialization/deserialization of the table of contents.
///     This is fun bit packing stuff!
/// </summary>
public class TableOfContentsSerializationTests
{
    [Theory]
    [InlineAutoManagedHeaders(ArchiveVersion.V0)]
    [InlineAutoManagedHeaders(ArchiveVersion.V1)]
    public unsafe void CanSerializeAndDeserialize(ArchiveVersion version, IFixture fixture)
    {
        const int solidBlockSize = 32767; // 32 KiB
        const int chunkSize = 67108864; // 64 MiB

        var files = new PackerFileForTesting[]
        {
            new("dvdroot/textures/s01.txd", 113763968), // 2 blocks
            new("dvdroot/textures/s12.txd", 62939496), // 1 block
            new("ModConfig.json", 768, SolidPreference.NoSolid, CompressionPreference.Lz4), // 1 file, but non-solid
            new("Readme.md", 3072), // 1 solid block
            new("Changelog.md", 2048)
        };

        // Generate dummy data for archived file.
        fixture.RepeatCount = files.Length;
        var entries = fixture.Create<FileEntry[]>();

        // Generate blocks.
        var groups = NxPacker.MakeGroups(files);
        var blocks = NxPacker.MakeBlocks(groups, solidBlockSize, chunkSize);

        // Generate TOC.
        using var tableOfContents = new TableOfContentsBuilder<PackerFileForTesting>(blocks, files);
        tableOfContents.Version = version;
        foreach (var entry in entries)
        {
            ref var item = ref tableOfContents.GetAndIncrementFileAtomic();
            item = entry; // copy data.
        }

        // Serialize
        var data = new byte[tableOfContents.CalculateTableSize()];
        fixed (byte* dataPtr = data)
        {
            var bytesWritten = tableOfContents.Build(dataPtr);
            bytesWritten.Should().Be(data.Length); // We calculated correct size.

            // Deserialize
            var newTable = TableOfContents.Deserialize(dataPtr, data.Length, tableOfContents.Version);
            newTable.Should().Be(tableOfContents.Toc);
        }
    }
}
