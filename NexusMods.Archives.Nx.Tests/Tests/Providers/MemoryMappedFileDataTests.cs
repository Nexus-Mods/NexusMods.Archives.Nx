using NexusMods.Archives.Nx.FileProviders.FileData;
using NexusMods.Archives.Nx.Tests.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Providers;

public class MemoryMappedFileDataTests
{
    // Tests for some edge cases here.
    
    [Fact]
    public void CanMapZeroByteFile()
    {
        // Arrange
        using var tempDir = new TemporaryDirectory();
        var tempPath = Path.Combine(tempDir.FolderPath, "empty.txt");
        File.Create(tempPath).Dispose();
        
        // Act
        using var fileData = new MemoryMappedFileData(tempPath, 0, 0);
    }
}
