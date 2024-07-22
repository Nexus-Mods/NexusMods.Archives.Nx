using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Providers;

public class OutputArrayProviderTests
{
    [Fact]
    public void OutputArrayProvider_WithFileTooBig_Throws()
    {
        var entry = new FileEntry
        {
            DecompressedSize = (ulong)int.MaxValue + 1
        };

        Assert.Throws<EntryCannotFitInArrayException>(() => new OutputArrayProvider("test", entry));
    }


}
