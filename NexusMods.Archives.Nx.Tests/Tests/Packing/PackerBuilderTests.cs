using FluentAssertions;
using Moq;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Packing;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class PackerBuilderTests
{
    [Fact]
    public void AddFolder_ShouldAddFiles()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        var folderPath = ".";

        // Act
        sut.AddFolder(folderPath);

        // Assert
        sut.Files.Should().NotBeEmpty();
    }

    [Fact]
    public void AddFile_ByteArray_ShouldAddFile()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        var data = new byte[] { 1, 2, 3 };
        var options = new AddFileParams { RelativePath = "test" };

        // Act
        sut.AddFile(data, options);

        // Assert
        sut.Files.Should().NotBeEmpty();
    }

    [Fact]
    public void AddFile_Stream_ShouldAddFile()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var options = new AddFileParams { RelativePath = "test" };

        // Act
        sut.AddFile(stream, options);

        // Assert
        sut.Files.Should().NotBeEmpty();
    }

    [Fact]
    public void Build_ShouldReturnStream()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        var data = new byte[] { 1, 2, 3 };
        var options = new AddFileParams { RelativePath = "test" };

        sut.AddFile(data, options);

        // Act
        var result = sut.Build();

        // Assert
        result.Should().NotBeNull();
    }
    
        [Fact]
    public void WithProgress_ShouldSetProgress()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        var progressMock = new Mock<IProgress<double>>();

        // Act
        sut.WithProgress(progressMock.Object);

        // Assert
        sut.Settings.Progress.Should().BeSameAs(progressMock.Object);
    }

    [Fact]
    public void WithMaxNumThreads_ShouldSetMaxNumThreads()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        int maxNumThreads = 4;

        // Act
        sut.WithMaxNumThreads(maxNumThreads);

        // Assert
        sut.Settings.MaxNumThreads.Should().Be(maxNumThreads);
    }

    [Fact]
    public void WithBlockSize_ShouldSetBlockSize()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        int blockSize = 32767;

        // Act
        sut.WithBlockSize(blockSize);

        // Assert
        sut.Settings.BlockSize.Should().Be(blockSize);
    }

    [Fact]
    public void WithChunkSize_ShouldSetChunkSize()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        int chunkSize = 4194304;

        // Act
        sut.WithChunkSize(chunkSize);

        // Assert
        sut.Settings.ChunkSize.Should().Be(chunkSize);
    }

    [Fact]
    public void WithZStandardLevel_ShouldSetZStandardLevel()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        int zStandardLevel = 1;

        // Act
        sut.WithZStandardLevel(zStandardLevel);

        // Assert
        sut.Settings.ZStandardLevel.Should().Be(zStandardLevel);
    }

    [Fact]
    public void WithLZ4Level_ShouldSetLZ4Level()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        int lz4Level = 1;

        // Act
        sut.WithLZ4Level(lz4Level);

        // Assert
        sut.Settings.Lz4Level.Should().Be(lz4Level);
    }

    [Fact]
    public void WithOutput_ShouldSetOutput()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        var output = new MemoryStream();

        // Act
        sut.WithOutput(output);

        // Assert
        sut.Settings.Output.Should().BeSameAs(output);
    }

    [Fact]
    public void WithSolidBlockAlgorithm_ShouldSetSolidBlockAlgorithm()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        var solidBlockAlgorithm = CompressionPreference.ZStandard;

        // Act
        sut.WithSolidBlockAlgorithm(solidBlockAlgorithm);

        // Assert
        sut.Settings.SolidBlockAlgorithm.Should().Be(solidBlockAlgorithm);
    }

    [Fact]
    public void WithChunkedFileAlgorithm_ShouldSetChunkedFileAlgorithm()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        var chunkedFileAlgorithm = CompressionPreference.ZStandard;

        // Act
        sut.WithChunkedFileAlgorithm(chunkedFileAlgorithm);

        // Assert
        sut.Settings.ChunkedFileAlgorithm.Should().Be(chunkedFileAlgorithm);
    }
    
    [Fact]
    public void AddFile_FromPath_ShouldSetCorrectFileSize()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        var filePath = @"NexusMods.Archives.Nx.Tests.dll";
        var expectedSize = new FileInfo(filePath).Length;
        var options = new AddFileParams { RelativePath = "NexusMods.Archives.Nx.Tests.dll" };

        // Act
        var result = sut.AddFile(filePath, options);

        // Assert
        result.Files.Should().ContainSingle();
        result.Files[0].FileSize.Should().Be(expectedSize);
        result.Files[0].FileDataProvider.Should().BeOfType<FromDirectoryDataProvider>();
    }

    [Fact]
    public void AddFile_FromStream_ShouldSetCorrectFileSize()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        var filePath = @"NexusMods.Archives.Nx.Tests.dll";
        var stream = File.OpenRead(filePath);
        var expectedSize = stream.Length;
        var options = new AddFileParams { RelativePath = "NexusMods.Archives.Nx.Tests.dll" };

        // Act
        var result = sut.AddFile(stream, expectedSize, options);

        // Assert
        result.Files.Should().ContainSingle();
        result.Files[0].FileSize.Should().Be(expectedSize);
        result.Files[0].FileDataProvider.Should().BeOfType<FromStreamProvider>();
    }
}
