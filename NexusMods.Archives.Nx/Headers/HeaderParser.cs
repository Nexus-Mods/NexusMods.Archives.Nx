using NexusMods.Archives.Nx.Headers.Enums;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Headers;

/// <summary>
///     Utility for parsing `.nx` headers intended for extracting files.
/// </summary>
public static class HeaderParser
{
    /// <summary>
    ///     Parses the header from a given data provider.
    /// </summary>
    /// <param name="provider">
    ///     Provides the header data.
    /// </param>
    /// <param name="hasLotsOfFiles">
    ///     This is a hint to the parser whether the file to be parsed contains lots of individual files (100+).
    /// </param>
    public static unsafe ParsedHeader ParseHeader(IFileDataProvider provider, bool hasLotsOfFiles = false)
    {
        /*
            This library is optimised in mind with smaller mods; most mods' TOC and header fits in within 4K.
        
            In any case, on a 980 Pro (currently most popular NVMe), we observe the following latencies (QD 1):
            - 4K: 52us
            - 8K: 80us
            ---- Delimited For Margin of Error ----
            - 16K: 95us
            - 32K: 85us
            - 64K: 89us
            - 128K: 100us
            - 256K: 140us
            ----
            - 512K: 218us
    
            Although benchmarks for random reads beyond 4K are hard to come by; this is a general pattern that seems to surface
            from the limited data I could gather. In any case, this is the explanation for header size choice.
        */

        var headerSize = hasLotsOfFiles ? 65536 : 4096;
        using var data = provider.GetFileData(0, (uint)headerSize);
        var header = TryParseHeader(data.Data, headerSize);
        if (header.Header != null)
            return header.Header;

        using var remainingData = provider.GetFileData(headerSize, (uint)(header.HeaderSize - headerSize));
        using var fullHeader = new ArrayRental(header.HeaderSize);
        fixed (byte* fullHeaderPtr = fullHeader.Span)
        {
            Buffer.MemoryCopy(data.Data, fullHeaderPtr, fullHeader.Array.Length, headerSize);
            Buffer.MemoryCopy(remainingData.Data, fullHeaderPtr + headerSize, fullHeader.Array.Length - headerSize, header.HeaderSize - headerSize);
            return TryParseHeader(fullHeaderPtr, header.HeaderSize).Header!;
        }
    }

    /// <summary>
    ///     Tries to read the header from the given data.
    /// </summary>
    /// <param name="data">Pointer to header data.</param>
    /// <param name="dataSize">Number of bytes available at <paramref name="data" />.</param>
    /// <returns>
    ///     A result with <see cref="HeaderParserResult.Header"/> not null if parsed, else <see cref="HeaderParserResult.Header"/> is null
    ///     and you should call this method again with a larger <paramref name="dataSize"/>.
    /// </returns>
    /// <exception cref="NotANexusArchiveException">Not a Nexus Archive.</exception>
    public static unsafe HeaderParserResult TryParseHeader(byte* data, int dataSize)
    {
        var header = (NativeFileHeader*)data;
        header->ReverseEndianIfNeeded();

        try
        {
            // Verify if Nexus Archive
            if (!header->IsValidMagicHeader())
            {
                ThrowHelpers.ThrowNotANexusArchive();
                return new HeaderParserResult { Header = null, HeaderSize = -1 };
            }

            // Verify if length is sufficient.
            if (dataSize < header->HeaderPageBytes)
                return new HeaderParserResult { Header = null, HeaderSize = header->HeaderPageBytes };

            var parsedHeader =
                TableOfContents.Deserialize<ParsedHeader>(data + sizeof(NativeFileHeader), header->TocSize, (ArchiveVersion)header->Version);
            parsedHeader.Header = *header;
            parsedHeader.Init();

            return new HeaderParserResult
            {
                Header = parsedHeader,
                HeaderSize = header->TocSize
            };
        }
        finally
        {
            // Leave original data untouched.
            header->ReverseEndianIfNeeded();
        }
    }

    /// <summary>
    ///     Stores the result of parsing header.
    /// </summary>
    public struct HeaderParserResult
    {
        /// <summary>
        ///     The parsed header to use with extraction logic.
        ///     If this header is null, insufficient bytes are available and you should get required header size from
        ///     <see cref="HeaderSize" /> and
        ///     call the method again once you have enough bytes..
        /// </summary>
        public required ParsedHeader? Header { get; init; } = null;

        /// <summary>
        ///     Required size of the header + toc in bytes.
        /// </summary>
        public required int HeaderSize { get; init; }

        /// <summary />
        public HeaderParserResult() { }
    }
}
