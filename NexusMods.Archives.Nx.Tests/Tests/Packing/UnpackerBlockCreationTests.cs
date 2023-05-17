using FluentAssertions;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Tests.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class UnpackerBlockCreationTests
{
    [Fact]
    public void MakeExtractableBlocks_SplitsCorrectly()
    {
        // Setup
        const int chunkSize = 10;
        var items = new IOutputDataProvider[]
        {
            new TestOutputDataProvider()
            {
                RelativePath = "Block0File0",
                Entry = new FileEntry()
                {
                    DecompressedSize = 5,
                    DecompressedBlockOffset = 0,
                    FirstBlockIndex = 0
                }
            },
            new TestOutputDataProvider()
            {
                RelativePath = "Block0File1",
                Entry = new FileEntry()
                {
                    DecompressedSize = 5,
                    DecompressedBlockOffset = 5,
                    FirstBlockIndex = 0
                }
            },
            // Chunked File, Spans 2 Blocks
            new TestOutputDataProvider()
            {
                RelativePath = "ChunkedFile0",
                Entry = new FileEntry()
                {
                    DecompressedSize = 20,
                    DecompressedBlockOffset = 0,
                    FirstBlockIndex = 1
                }
            },
            // Non-SOLID explicitly requested by user 
            new TestOutputDataProvider()
            {
                RelativePath = "NonSolid0",
                Entry = new FileEntry()
                {
                    DecompressedSize = 5,
                    DecompressedBlockOffset = 0,
                    FirstBlockIndex = 3
                }
            }
        };

        // Act
        var blocks = NxUnpacker.MakeExtractableBlocks(items, chunkSize);

        // Assert
        blocks.Count.Should().Be(4);

        // Check First Block
        blocks[0].BlockIndex.Should().Be(0);
        blocks[0].Outputs.Count.Should().Be(2);
        blocks[0].Outputs[0].RelativePath.Should().Be("Block0File0");
        blocks[0].Outputs[1].RelativePath.Should().Be("Block0File1");
        blocks[0].DecompressSize.Should().Be(10);

        // Check Second Block (Chunk Block 0)
        blocks[1].BlockIndex.Should().Be(1);
        blocks[1].Outputs.Count.Should().Be(1);
        blocks[1].Outputs[0].RelativePath.Should().Be("ChunkedFile0");
        blocks[1].DecompressSize.Should().Be(10);

        // Check Third Block (Chunk Block 1)
        blocks[2].BlockIndex.Should().Be(2);
        blocks[2].Outputs.Count.Should().Be(1);
        blocks[2].Outputs[0].RelativePath.Should().Be("ChunkedFile0");
        blocks[2].DecompressSize.Should().Be(10);

        // Check Last Block (Explicit SOLID Requested)
        blocks[3].BlockIndex.Should().Be(3);
        blocks[3].Outputs.Count.Should().Be(1);
        blocks[3].Outputs[0].RelativePath.Should().Be("NonSolid0");
        blocks[3].DecompressSize.Should().Be(5);
    }

    // Test for when size of chunked file size is not divisible by chunk size
    [Fact]
    public void MakeExtractableBlocks_Chunked_WithNonDivisibleFileSize()
    {
        // Setup
        const int chunkSize = 10;
        var items = new IOutputDataProvider[]
        {
            // Chunked File, Spans 2 Blocks, 10 in one, 5 in another
            new TestOutputDataProvider()
            {
                RelativePath = "ChunkedFile0",
                Entry = new FileEntry()
                {
                    DecompressedSize = 15,
                    DecompressedBlockOffset = 0,
                    FirstBlockIndex = 0
                }
            }
        };

        // Act
        var blocks = NxUnpacker.MakeExtractableBlocks(items, chunkSize);

        // Assert
        blocks.Count.Should().Be(2);

        // Check First Block (Chunk Block 0)
        blocks[0].BlockIndex.Should().Be(0);
        blocks[0].Outputs.Count.Should().Be(1);
        blocks[0].Outputs[0].RelativePath.Should().Be("ChunkedFile0");
        blocks[0].DecompressSize.Should().Be(10);

        // Check Second Block (Chunk Block 1)
        blocks[1].BlockIndex.Should().Be(1);
        blocks[1].Outputs.Count.Should().Be(1);
        blocks[1].Outputs[0].RelativePath.Should().Be("ChunkedFile0");
        blocks[1].DecompressSize.Should().Be(5);
    }
}
