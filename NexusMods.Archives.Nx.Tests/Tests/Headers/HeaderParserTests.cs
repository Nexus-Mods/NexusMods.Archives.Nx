using System.Runtime.CompilerServices;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Headers;

public class HeaderParserTests
{
    [Fact]
    public unsafe void ThrowsUnsupportedArchiveVersionException_WhenVersionIsHigherThanSupported()
    {
        // Arrange
        var header = new NativeFileHeader();
        NativeFileHeader.Init(&header, 1024 * 1024, 1);

        // Act
        // Set the archive to an incompatible version.
        header.Version = NativeFileHeader.CurrentArchiveVersion + 1;

        // Verify that we can't parse
        var array = new byte[NativeFileHeader.SizeBytes];
        fixed (byte* ptr = array)
            Unsafe.Write(ptr, header);

        var arrayProvider = new FromArrayProvider()
        {
            Data = array
        };
        Assert.Throws<UnsupportedArchiveVersionException>(() => HeaderParser.ParseHeader(arrayProvider));
    }
}
