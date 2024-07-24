using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers.Managed;

namespace NexusMods.Archives.Nx.Tests.Tests.Providers;

public class OutputFileProviderTests : IDisposable
{
    private readonly string _tempDirectory;

    public OutputFileProviderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
    }

    [Fact]
    public void CanWriteAboveU32SizedMemoryMappedFile()
    {
        // Arrange
        var relativePath = "large_file.bin";
        var fileSize = 4L * 1024 * 1024 * 1024 + 1; // 4GiB + 1 byte
        var entry = new FileEntry
        {
            DecompressedSize = (ulong)fileSize
        };

        // Act
        using (var provider = new OutputFileProvider(_tempDirectory, relativePath, entry))
        {
            using var fileData = provider.GetFileData(0, (ulong)fileSize);

            // Write some data at the beginning
            unsafe
            {
                fileData.Data[0] = 0xAA;
            }

            // Write some data at the end
            unsafe
            {
                fileData.Data[fileSize - 1] = 0xBB;
            }
        }

        // Assert
        var filePath = Path.Combine(_tempDirectory, relativePath);
        Assert.True(File.Exists(filePath), "Large file should exist");

        var fileInfo = new FileInfo(filePath);
        Assert.Equal(fileSize, fileInfo.Length);

        using (var fileStream = File.OpenRead(filePath))
        {
            // Check beginning of the file
            Assert.Equal(0xAA, fileStream.ReadByte());

            // Check end of the file
            fileStream.Seek(-1, SeekOrigin.End);
            Assert.Equal(0xBB, fileStream.ReadByte());
        }
    }
}
