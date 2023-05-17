using JetBrains.Annotations;
using NexusMods.Archives.Nx.FileProviders.FileData;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.FileProviders;

/// <summary>
///     File data provider that provides info from a stream.
///     Source Stream must support seeking.
/// </summary>
[PublicAPI]
public sealed class FromStreamProvider : IFileDataProvider
{
    /// <summary>
    ///     Stream associated with this provider.
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    ///     Start of stream pointer.
    /// </summary>
    public long StreamStart { get; }

    /// <summary>
    ///     Creates a provider from a stream.
    /// </summary>
    /// <param name="stream">The stream to create provider from.</param>
    public FromStreamProvider(Stream stream)
    {
        Stream = stream;
        StreamStart = stream.Position;
    }

    /// <inheritdoc />
    public IFileData GetFileData(long start, uint length)
    {
        lock (Stream)
        {
            var newPos = StreamStart + start;
            Stream.Seek(newPos, SeekOrigin.Begin);
            var pooledData = new ArrayRental((int)length);

            // In case of old framework, or Stream which doesn't implement span overload, don't use span here.
            var numRead = Polyfills.ReadAtLeast(Stream, pooledData.Array, (int)length);
            return new RentedArrayFileData(new ArrayRentalSlice(pooledData, numRead));
        }
    }
}
