using FluentAssertions;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Tests.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class PackerBlockCreationTests
{
    [Fact]
    public void MakeBlocks_SplitsCorrectly()
    {
        // Setup
        const int solidBlockSize = 10;
        var items = new Dictionary<string, List<PackerFileForTesting>>
        {
            {
                "",
                new List<PackerFileForTesting>
                {
                    new()
                    {
                        FileSize = 1,
                        RelativePath = "Block0File0"
                    },
                    new()
                    {
                        FileSize = 8,
                        RelativePath = "Block0File1"
                    },
                    new()
                    {
                        FileSize = 1,
                        RelativePath = "Block0File2"
                    },
                    new()
                    {
                        FileSize = 1,
                        RelativePath = "Block1File0"
                    }
                }
            }
        };

        // Act
        var blocks = NxPacker.MakeBlocks(items, solidBlockSize, int.MaxValue, CompressionPreference.Lz4);

        // Assert
        blocks.Count.Should().Be(2);
        blocks[0].Should().BeOfType<SolidBlock<PackerFileForTesting>>();
        blocks[1].Should().BeOfType<SolidBlock<PackerFileForTesting>>();

        // Check Solid Block
        var first = blocks[0] as SolidBlock<PackerFileForTesting>;
        var firstItems = first!.Items;
        first.Compression.Should().Be(CompressionPreference.Lz4); // respects block compression.
        firstItems.Count.Should().Be(3);
        firstItems[0].RelativePath.Should().Be("Block0File0");
        firstItems[1].RelativePath.Should().Be("Block0File1");
        firstItems[2].RelativePath.Should().Be("Block0File2");

        // Check single item block.
        var second = blocks[1] as SolidBlock<PackerFileForTesting>;
        var secondItems = second!.Items;
        second.Compression.Should().Be(CompressionPreference.Lz4); // respects block compression.
        secondItems.Count.Should().Be(1);
        secondItems[0].RelativePath.Should().Be("Block1File0");
    }

    [Fact]
    public void MakeBlocks_RespectsNoSolidAndCompressionPreference()
    {
        // Setup
        const int solidBlockSize = 10;
        var items = new Dictionary<string, List<PackerFileForTesting>>
        {
            {
                "",
                new List<PackerFileForTesting>
                {
                    new()
                    {
                        FileSize = 1,
                        RelativePath = "Block1File0"
                    },
                    new()
                    {
                        FileSize = 8,
                        RelativePath = "Block0File0",
                        SolidType = SolidPreference.NoSolid,
                        CompressionPreference = CompressionPreference.Lz4
                    },
                    new()
                    {
                        FileSize = 1,
                        RelativePath = "Block1File1"
                    },
                    new()
                    {
                        FileSize = 1,
                        RelativePath = "Block1File2"
                    }
                }
            }
        };

        // We specified NoSOLID and LZ4 on Block0File0. Block chunker should respect this decision.
        // Act
        var blocks = NxPacker.MakeBlocks(items, solidBlockSize, int.MaxValue, CompressionPreference.ZStandard);

        // Assert
        blocks.Count.Should().Be(2);
        blocks[0].Should().BeOfType<SolidBlock<PackerFileForTesting>>();
        blocks[1].Should().BeOfType<SolidBlock<PackerFileForTesting>>();

        // Check Solid Block
        var first = blocks[0] as SolidBlock<PackerFileForTesting>;
        var firstItems = first!.Items;
        first.Compression.Should().Be(CompressionPreference.Lz4); // respects block compression.
        firstItems.Count.Should().Be(1);
        firstItems[0].RelativePath.Should().Be("Block0File0");

        // Check single item block.
        var second = blocks[1] as SolidBlock<PackerFileForTesting>;
        var secondItems = second!.Items;
        second.Compression.Should().Be(CompressionPreference.ZStandard); // respects block compression.
        secondItems.Count.Should().Be(3);
    }

    [Fact]
    public void MakeBlocks_ChunksCorrectly()
    {
        // Setup
        const int solidBlockSize = 9;
        const int chunkSize = 10;

        var items = new Dictionary<string, List<PackerFileForTesting>>
        {
            {
                "",
                new List<PackerFileForTesting>
                {
                    new()
                    {
                        FileSize = 25,
                        RelativePath = "ChunkedFile"
                    }
                }
            }
        };

        // Act
        var blocks = NxPacker.MakeBlocks(items, solidBlockSize, chunkSize, CompressionPreference.NoPreference,
            CompressionPreference.ZStandard);

        // Assert
        blocks.Count.Should().Be(3);
        blocks[0].Should().BeOfType<ChunkedFileBlock<PackerFileForTesting>>();
        blocks[1].Should().BeOfType<ChunkedFileBlock<PackerFileForTesting>>();
        blocks[2].Should().BeOfType<ChunkedFileBlock<PackerFileForTesting>>();

        // Check Solid Block
        var first = blocks[0] as ChunkedFileBlock<PackerFileForTesting>;
        first!.Compression.Should().Be(CompressionPreference.ZStandard); // respects chunk compression.
        first.StartOffset.Should().Be(0);
        first.ChunkIndex.Should().Be(0);
        first.ChunkSize.Should().Be(chunkSize);

        var second = blocks[1] as ChunkedFileBlock<PackerFileForTesting>;
        second!.Compression.Should().Be(CompressionPreference.ZStandard); // respects chunk compression.
        second.StartOffset.Should().Be(chunkSize); // 10
        second.ChunkIndex.Should().Be(1);
        second.ChunkSize.Should().Be(chunkSize);

        var last = blocks[2] as ChunkedFileBlock<PackerFileForTesting>;
        last!.Compression.Should().Be(CompressionPreference.ZStandard); // respects chunk compression.
        last.StartOffset.Should().Be(chunkSize * 2); // 20
        last.ChunkIndex.Should().Be(2);
        last.ChunkSize.Should().Be(5);
    }
}
