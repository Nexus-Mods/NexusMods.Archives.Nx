using AutoFixture;
using AutoFixture.Dsl;
using AutoFixture.Kernel;
using AutoFixture.Xunit2;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Headers.Native.Structs;

namespace NexusMods.Archives.Nx.Tests.Attributes;

/// <summary>
///     Custom <see cref="AutoDataAttribute" /> with support for
///     creating a dummies for native/serialized ToC sections.
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

        ret.Customize<NativeTocHeader>(composer =>
        {
            var result = composer.FromFactory(() => new NativeTocHeader());

            if (!randomizeHeader)
                result = result.OmitAutoProperties();

            return result;
        });

        ret.Customize<OffsetPathIndexTuple>(composer => WithRandomizeHeader(randomizeHeader, composer));
        ret.Customize<NativeFileEntryV0>(composer => WithRandomizeHeader(randomizeHeader, composer));
        ret.Customize<NativeFileEntryV1>(composer => WithRandomizeHeader(randomizeHeader, composer));
        return ret;
    })
    { }

    private static ISpecimenBuilder WithRandomizeHeader<T>(bool randomizeHeader, ICustomizationComposer<T> composer)
        where T : new()
    {
        var result = composer.FromFactory(() => new T());
        if (!randomizeHeader)
            result = result.OmitAutoProperties();

        return result;
    }
}
