using AutoFixture;
using AutoFixture.Xunit2;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Headers.Native;

namespace NexusMods.Archives.Nx.Tests.Attributes;

/// <summary>
///     Custom <see cref="AutoDataAttribute" /> with support for
///     creating a dummy <see cref="NativeFileHeader" /> for testing.
/// </summary>
[PublicAPI]
public class AutoManagedHeadersAttribute : AutoDataAttribute
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    public AutoManagedHeadersAttribute(bool randomizeHeader = false) : base(() =>
    {
        var ret = new Fixture();
        ret.Customize<FileEntry>(composer =>
        {
            var result = composer.FromFactory(() => new FileEntry());
            if (!randomizeHeader)
                result = result.OmitAutoProperties();

            return result;
        });

        return ret;
    }) { }
}
