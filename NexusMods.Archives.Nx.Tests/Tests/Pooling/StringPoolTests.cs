using FluentAssertions;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Tests.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Pooling;

public class StringPoolTests
{
    /// <summary>
    ///     Purpose of this specific test is to ensure GrowableMemoryPool grows; and that we copy the data over correctly.
    ///     This test is specifically crafted to hit that case; careful if editing it.
    /// </summary>
    [Fact]
    public void CreateAndVerifyPool_WithMultiCharOnly()
    {
        var items = new StringWrapper[]
        {
            "🧡🧡🧡🧡🧡🧡🧡🧡",
            "🔥🔥🔥🔥🔥🔥",
            "‼️‼️‼️‼️",
            "⚡⚡"
        };

        using var createPool = StringPool.Pack(items.AsSpan());
        var strings = Polyfills.ToHashSet(StringPool.Unpack(createPool.Span));

        foreach (var item in items)
            strings.Should().Contain(item.RelativePath);
    }

    [Fact]
    public void CreateAndVerifyPool()
    {
        var items = new StringWrapper[]
        {
            "Data/Movie/EN/Opening.sfd",
            "Data/Movie/Credits.sfd",
            "Data/Sonk.bin",
            "Sonk.exe"
        };

        using var createPool = StringPool.Pack(items.AsSpan());
        var strings = Polyfills.ToHashSet(StringPool.Unpack(createPool.Span));

        foreach (var item in items)
            strings.Should().Contain(item.RelativePath);
    }
}
