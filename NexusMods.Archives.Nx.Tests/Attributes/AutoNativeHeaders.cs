using AutoFixture;
using AutoFixture.Xunit2;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Headers.Native.Structs;

namespace NexusMods.Archives.Nx.Tests.Attributes;

/// <summary>
///     Custom <see cref="AutoDataAttribute" /> with support for
///     creating a dummy <see cref="NativeFileHeader"/> for testing.
/// </summary>
[PublicAPI]
public class AutoNativeHeadersAttribute : AutoDataAttribute
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    public AutoNativeHeadersAttribute(bool randomizeHeader = false) : base(() =>
    {
        var ret = new Fixture();
        ret.Customize<NativeFileHeader>(composer =>
        {
            var result = composer.FromFactory(() =>
            {
                var header = new NativeFileHeader();
                header.SetMagic();
                return header;
            });

            if (!randomizeHeader)
                result = result.OmitAutoProperties();

            return result;
        });
        
        ret.Customize<OffsetPathIndexTuple>(composer =>
        {
            var result = composer.FromFactory(() => new OffsetPathIndexTuple());
            if (!randomizeHeader)
                result = result.OmitAutoProperties();

            return result;
        });
        return ret;
    }) { }
}