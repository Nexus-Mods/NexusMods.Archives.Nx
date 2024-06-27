using System.Diagnostics.CodeAnalysis;
using NetEscapades.EnumGenerators;

namespace NexusMods.Archives.Nx.Enums;

/// <summary>
///     Preferred option for compression.
/// </summary>
[EnumExtensions]
public enum CompressionPreference : byte
{
    /// <summary>
    ///     No preference is specified.
    /// </summary>
    NoPreference = 255,

    // Note: Values below match their encoding in ToC, so we use 255 as 'none'.
    // Note: Max allowed value is 7 in current implementation due to packing.

    /// <summary>
    ///     Do not compress at all, copy data verbatim.
    /// </summary>
    Copy = 0,

    /// <summary>
    ///     Compress with ZStandard.
    /// </summary>
    ZStandard = 1,

    /// <summary>
    ///     Compress with LZ4.
    /// </summary>
    Lz4 = 2
}

// Auto generated.
[ExcludeFromCodeCoverage]
#pragma warning disable RS0016 // Add public types and members to the declared API
// ReSharper disable once PartialTypeWithSinglePart
public static partial class CompressionPreferenceExtensions { }
#pragma warning restore RS0016
