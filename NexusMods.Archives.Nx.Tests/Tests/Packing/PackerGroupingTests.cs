using FluentAssertions;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Traits;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class PackerGroupingTests
{
    [Fact]
    public void CanGroupByExtension_PreservingSizeAscending()
    {
        var expected = new Dictionary<string, List<PackerTestItem>>
        {
            {
                ".txt", new List<PackerTestItem>
                {
                    new("fluffy.txt", 100),
                    new("whiskers.txt", 200),
                    new("mittens.txt", 300), // '1 elo'
                    new("snickers.txt", 400),
                    new("tigger.txt", 500),
                    new("boots.txt", 600),
                    new("simba.txt", 700),
                    new("garfield.txt", 800),
                    new("nala.txt", 900),
                    new("cleo.txt", 1000)
                }
            },
            {
                ".bin", new List<PackerTestItem>
                {
                    new("banana.bin", 450),
                    new("orange.bin", 666),
                    new("pear.bin", 777),
                    new("peach.bin", 888)
                }
            },
            {
                ".pak", new List<PackerTestItem>
                {
                    new("data01.pak", 111),
                    new("data02.pak", 222),
                    new("data03.pak", 444),
                    new("data04.pak", 889)
                }
            }
        };

        var items = expected.SelectMany(x => x.Value).ToArray();
        items.SortBySizeAscending(); // replicate sort in packer

        var groups = NxPacker.MakeGroups(items);
        foreach (var group in groups)
        {
            expected.Should().ContainKey(group.Key);
            var expectedValues = expected[group.Key];
            expectedValues.Should().Equal(group.Value);
        }
    }

    public record PackerTestItem(string RelativePath, long FileSize) : IHasRelativePath, IHasFileSize;
}
