using AutoFixture;
using FluentAssertions;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Enums;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Packing.Pack.Steps;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Tests.Attributes;
using NexusMods.Archives.Nx.Tests.Utilities;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests;

/// <summary>
///     Tests involving the serialization/deserialization of the table of contents.
///     This is fun bit packing stuff!
/// </summary>
public unsafe class TableOfContentsSerializationTests
{
    [Theory]
    [InlineAutoManagedHeaders(TableOfContentsVersion.V0)]
    [InlineAutoManagedHeaders(TableOfContentsVersion.V1)]
    public void CanSerializeAndDeserialize(TableOfContentsVersion version, IFixture fixture)
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
        var groups = GroupFiles.Do(files);
        var blocks = MakeBlocks.Do(groups, solidBlockSize, chunkSize);

        // Generate TOC.
        using var tableOfContents = TableOfContentsBuilder<PackerFileForTesting>.Create<PackerFileForTesting>(blocks, files);
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
            var bytesWritten = tableOfContents.Build(dataPtr, data.Length);
            bytesWritten.Should().Be(data.Length); // We calculated correct size.

            // Deserialize
            var newTable = TableOfContents.Deserialize<TableOfContents>(dataPtr);
            newTable.Should().Be(tableOfContents.Toc);
        }
    }

    [Theory]
    [InlineData(TableOfContentsVersion.V0)]
    [InlineData(TableOfContentsVersion.V1)]
    public void CanSerializeMaximumFileCount(TableOfContentsVersion version)
    {
        var files = new PackerFileForTesting[TableOfContents.MaxFileCountV0V1];
        for (var x = 0; x < files.Length; x++)
            files[x] = new PackerFileForTesting($"file_{x}.txt", 1024);

        var blocks = new List<IBlock<PackerFileForTesting>>
        {
            new SolidBlock<PackerFileForTesting>([files[0]], CompressionPreference.Copy)
        };

        using var tableOfContents = TableOfContentsBuilder<PackerFileForTesting>.Create<PackerFileForTesting>(blocks, files);
        tableOfContents.Version = version;

        var data = new byte[tableOfContents.CalculateTableSize()];

        var act = () =>
        {
            fixed (byte* dataPtr = data)
            {
                // ReSharper disable once AccessToDisposedClosure
                tableOfContents.Build(dataPtr, data.Length);
            }
        };

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(TableOfContentsVersion.V0)]
    [InlineData(TableOfContentsVersion.V1)]
    public void ThrowsExceptionWhenFileCountExceedsMaximum(TableOfContentsVersion version)
    {
        var files = new PackerFileForTesting[TableOfContents.MaxFileCountV0V1 + 1];
        for (var x = 0; x < files.Length; x++)
            files[x] = new PackerFileForTesting($"file_{x}.txt", 1024);

        var blocks = new List<IBlock<PackerFileForTesting>>
        {
            new SolidBlock<PackerFileForTesting>([files[0]], CompressionPreference.Copy)
        };

        using var tableOfContents = TableOfContentsBuilder<PackerFileForTesting>.Create<PackerFileForTesting>(blocks, files);
        tableOfContents.Version = version;

        var data = new byte[tableOfContents.CalculateTableSize()];

        var act = () =>
        {
            fixed (byte* dataPtr = data)
            {
                // ReSharper disable once AccessToDisposedClosure
                tableOfContents.Build(dataPtr, data.Length);
            }
        };

        act.Should().Throw<TooManyFilesException>();
    }

    [Theory]
    [InlineData(TableOfContentsVersion.V0)]
    [InlineData(TableOfContentsVersion.V1)]
    public void CanSerializeMaximumBlockCount(TableOfContentsVersion version)
    {
        var files = new PackerFileForTesting[] { new("file.txt", 1024) };
        var blocks = new List<IBlock<PackerFileForTesting>>(TableOfContents.MaxBlockCountV0V1);
        for (var x = 0; x < TableOfContents.MaxBlockCountV0V1; x++)
            blocks.Add(new SolidBlock<PackerFileForTesting>([files[0]], CompressionPreference.Copy));

        using var tableOfContents = TableOfContentsBuilder<PackerFileForTesting>.Create<PackerFileForTesting>(blocks.ToList(), files);
        tableOfContents.Version = version;

        var data = new byte[tableOfContents.CalculateTableSize()];

        var act = () =>
        {
            fixed (byte* dataPtr = data)
            {
                // ReSharper disable once AccessToDisposedClosure
                tableOfContents.Build(dataPtr, data.Length);
            }
        };

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(TableOfContentsVersion.V0)]
    [InlineData(TableOfContentsVersion.V1)]
    public void ThrowsExceptionWhenBlockCountExceedsMaximum(TableOfContentsVersion version)
    {
        var files = new PackerFileForTesting[] { new("file.txt", 1024) };
        var blocks = new List<IBlock<PackerFileForTesting>>(TableOfContents.MaxBlockCountV0V1 + 1);
        for (var x = 0; x < TableOfContents.MaxBlockCountV0V1 + 1; x++)
            blocks.Add(new SolidBlock<PackerFileForTesting>([files[0]], CompressionPreference.Copy));

        using var tableOfContents = TableOfContentsBuilder<PackerFileForTesting>.Create<PackerFileForTesting>(blocks, files);
        tableOfContents.Version = version;

        var data = new byte[tableOfContents.CalculateTableSize()];

        var act = () =>
        {
            fixed (byte* dataPtr = data)
            {
                // ReSharper disable once AccessToDisposedClosure
                tableOfContents.Build(dataPtr, data.Length);
            }
        };

        act.Should().Throw<TooManyBlocksException>();
    }
}
