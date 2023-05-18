using FluentAssertions;
using Moq;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Tests.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class UnpackerBuilderTests
{
    [Fact]
    public void WithProgress_ShouldSetCorrectProgress()
    {
        // Arrange
        var sut = CreateBuilder();
        var mockProgress = new Mock<IProgress<double>>();

        // Act
        var result = sut.WithProgress(mockProgress.Object);

        // Assert
        result.Settings.Progress.Should().Be(mockProgress.Object);
    }

    [Fact]
    public void WithMaxNumThreads_ShouldSetCorrectMaxThreads()
    {
        // Arrange
        var sut = CreateBuilder();
        var maxThreads = 5;

        // Act
        var result = sut.WithMaxNumThreads(maxThreads);

        // Assert
        result.Settings.MaxNumThreads.Should().Be(maxThreads);
    }

    [Fact]
    public void AddFilesWithArrayOutput_ShouldAddCorrectOutputs()
    {
        // Arrange
        var sut = CreateBuilder();
        var files = sut.GetPathedFileEntries();

        // Act
        sut.AddFilesWithArrayOutput(files, out var results);

        // Assert
        sut.Outputs.Should().Equal(results);
        ((OutputArrayProvider)sut.Outputs[0]).Data.Should().NotBeEmpty();
    }

    [Fact]
    public void AddFilesWithDiskOutput_ShouldAddCorrectOutputs()
    {
        // Arrange
        var sut = CreateBuilder();
        using var tempFolder = new TemporaryDirectory();

        // Act
        sut.AddFilesWithDiskOutput(sut.GetPathedFileEntries(), tempFolder.FolderPath);
        sut.Extract();

        // Assert
        var files = Directory.GetFiles(tempFolder.FolderPath);
        files.Length.Should().BeGreaterThan(0);
    }

    private NxUnpackerBuilder CreateBuilder()
    {
        // Arrange
        var sut = new NxPackerBuilder();
        var filePath = @"NexusMods.Archives.Nx.Tests.dll";
        var options = new AddFileParams { RelativePath = "NexusMods.Archives.Nx.Tests.dll" };

        // Act
        sut.WithSolidCompressionLevel(1);
        sut.WithChunkedLevel(1);
        sut.AddFile(filePath, options);
        var result = sut.Build(false);
        result.Position = 0;
        return new NxUnpackerBuilder(new FromStreamProvider(result));
    }
}
