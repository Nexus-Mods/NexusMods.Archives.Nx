using NexusMods.Archives.Nx.Tests.Utilities;
using NexusMods.Archives.Nx.TOC;

namespace NexusMods.Archives.Nx.Tests.Tests;

public class StringPoolTests
{
    [Fact]
    public void CreatePool()
    {
        var items = new StringWrapper[]
        {
            "Data/Movie/EN/Opening.sfd",
            "Data/Movie/Credits.sfd",
            "Data/Sonk.bin",
            "Sonk.exe"
        };

        using var createPool = StringPool.Pack(items.AsSpan());
    }
}