using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Tests.Utilities;

namespace NexusMods.Archives.Nx.Tests.Attributes;

/// <summary>
///     Custom <see cref="AutoDataAttribute" /> with support for
///     generating a custom temporary directory and other FileSystem helpers.
/// </summary>
[PublicAPI]
public class AutoFileSystemAttribute : AutoDataAttribute
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    public AutoFileSystemAttribute() : base(() =>
    {
        var ret = new Fixture();
        ret.Customize(new AutoMoqCustomization());
        ret.Customize<TemporaryDirectory>(composer => composer.FromFactory(() => new TemporaryDirectory()));
        ret.Customize<DummyKnownFileDirectory>(composer => composer.FromFactory(() => new DummyKnownFileDirectory()));
        return ret;
    }) { }
}
