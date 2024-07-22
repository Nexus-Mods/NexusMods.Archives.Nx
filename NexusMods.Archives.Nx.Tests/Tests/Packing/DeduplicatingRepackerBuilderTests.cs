using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;
using Moq;
using FluentAssertions;
using NexusMods.Archives.Nx.Packing;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class DeduplicatingRepackerBuilderTests
{
    [Fact]
    public void AddFileFromNxArchive_SameHashTwice_AddsOnce()
    {
        // Arrange
        var builder = new NxDeduplicatingRepackerBuilder();
        var mockSource = new Mock<IFileDataProvider>();
        var header = new ParsedHeader { Pool = ["path1"] };
        var entry = new FileEntry { Hash = 123, FilePathIndex = 0 };

        // Act
        builder.AddFileFromNxArchive(mockSource.Object, header, entry);
        builder.AddFileFromNxArchive(mockSource.Object, header, entry);

        // Assert
        builder.AddedFiles.Should().ContainSingle(kvp => kvp.Key == 123, "only one hash should be added");
        builder.AddedFiles[123].Should().ContainSingle("only one path should be added for this hash");
    }

    [Fact]
    public void AddFileFromNxArchive_SameHashDifferentPaths_AddsBoth()
    {
        // Arrange
        var builder = new NxDeduplicatingRepackerBuilder();
        var mockSource = new Mock<IFileDataProvider>();
        var header = new ParsedHeader { Pool = ["path1", "path2"] };
        var entry1 = new FileEntry { Hash = 123, FilePathIndex = 0 };
        var entry2 = new FileEntry { Hash = 123, FilePathIndex = 1 };

        // Act
        builder.AddFileFromNxArchive(mockSource.Object, header, entry1);
        builder.AddFileFromNxArchive(mockSource.Object, header, entry2);

        // Assert
        builder.AddedFiles.Should().ContainSingle(kvp => kvp.Key == 123, "only one hash should be added");
        builder.AddedFiles[123].Should().HaveCount(2, "two different paths should be added for this hash");
        builder.AddedFiles[123].Should().Contain(new[] { "path1", "path2" }, "both paths should be present");
    }

    [Fact]
    public void AddFileFromNxArchive_DifferentHash_AddsBoth()
    {
        // Arrange
        var builder = new NxDeduplicatingRepackerBuilder();
        var mockSource = new Mock<IFileDataProvider>();
        var header = new ParsedHeader { Pool = ["path1", "path2"] };
        var entry1 = new FileEntry { Hash = 123, FilePathIndex = 0 };
        var entry2 = new FileEntry { Hash = 456, FilePathIndex = 1 };

        // Act
        builder.AddFileFromNxArchive(mockSource.Object, header, entry1);
        builder.AddFileFromNxArchive(mockSource.Object, header, entry2);

        // Assert
        builder.AddedFiles.Should().HaveCount(2, "two different hashes should be added");
        builder.AddedFiles.Should().ContainKey(123).And.ContainKey(456);
        builder.AddedFiles[123].Should().ContainSingle("path1");
        builder.AddedFiles[456].Should().ContainSingle("path2");
    }
}
