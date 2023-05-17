using AutoFixture.Xunit2;

namespace NexusMods.Archives.Nx.Tests.Attributes;

public class InlineAutoManagedHeadersAttribute : InlineAutoDataAttribute
{
    public InlineAutoManagedHeadersAttribute(params object[] objects) : base(new AutoManagedHeadersAttribute(true), objects) { }
}
