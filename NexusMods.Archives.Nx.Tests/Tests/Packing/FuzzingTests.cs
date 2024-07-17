using FluentAssertions;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Packing.Unpack;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Utilities;
using Xunit.Abstractions;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

/// <summary>
///     These are brute force tests that continously keep running until they potentially find a bug.
/// </summary>
// ReSharper disable once UnusedType.Global
public class FuzzingTests(ITestOutputHelper testOutputHelper)
{
    private const int BlockSize = 1048575;
    private const int ChunkSize = 1048576;

    // [Fact]
    // ReSharper disable once UnusedMember.Global
#pragma warning disable xUnit1013
    public void Packing_RealData_RoundTrips()
#pragma warning restore xUnit1013
    {
        const int iterations = 100;
        // SMIM SE 2-08-659-2-08.7z
        // https://www.nexusmods.com/Core/Libs/Common/Widgets/DownloadPopUp?id=59069&game_id=1704
        const string folderPath = "/home/sewer/Temp/Repack-Samples/SMIM SE 2-08-659-2-08";
        const string hashMapPath = "Assets/SMIM SE 2-08-659-2-08-Hashes.txt";
        const bool deduplicateSolid = true;
        const bool deduplicateChunked = true;
        var compressionLevels = new[] { 1 };

        for (var x = 0; x < iterations; x++)
        {
            // Preload all original files into memory
            var originalFiles = PreloadOriginalFiles(folderPath);

            // Testing some weird hash weirdness
            var compLevel = compressionLevels [x % compressionLevels.Length];
            var packed = PackWithFilesFromDirectory(folderPath, deduplicateSolid, deduplicateChunked, compLevel, compLevel, BlockSize, ChunkSize);
            var map = CreateHashToStringMap(hashMapPath);

            // Test succeeds if it doesn't throw.
            packed.Position = 0;
            var unpacker = new NxUnpacker(new FromStreamProvider(packed), true);
            var entries = unpacker.GetPathedFileEntries();
            var entryHashes = new HashSet<ulong>();
            var unpackSettings = new UnpackerSettings();

            for (var y = 0; y < entries.Length; y++)
            {
                testOutputHelper.WriteLine("Iteration: {0}, File Idx: {1}, File: {2}", x, y, entries[y].FilePath);
                var entry = entries[y];
                var entryData = unpacker.ExtractFilesInMemory(new[] { entry.Entry }, unpackSettings);
                var entryHash = entryData[0].Data.XxHash64();

                // Get the original file.
                if (!originalFiles.TryGetValue(entry.FilePath, out var originalFileContent))
                    Assert.Fail($"Original file not found: {entry.FilePath}");

                // Verify Length
                entryData[0].Data.Length.Should().Be(originalFileContent.Length,
                    $"Content length mismatch for file: {entry.FilePath}." +
                    $"Extracted: {entryData[0].Data.Length}," +
                    $"Original: {originalFileContent.Length}");

                // Verify content.
                var location = GetMismatchLocation(entryData, originalFileContent);
                if (location >= 0)
                {
                    Assert.Fail($"Content mismatch for file: {entry.FilePath} at offset {location}.\n" +
                                $"Extracted byte: 0x{entryData[0].Data[location]:X2}, Original byte: 0x{originalFileContent[location]:X2}.\n" +
                                $"Entry: Block Idx/{entry.Entry.FirstBlockIndex}, File Idx/{y} Offset/{entry.Entry.DecompressedBlockOffset}, Size/{entry.Entry.DecompressedSize}");
                }

                // Verify extracted file hash matches the stored header data
                entryHash.Should().Be(entry.Entry.Hash, $"Hash mismatch for file: {entry.FilePath}");
                entryHashes.Add(entry.Entry.Hash);
            }

            // Verify that each expected hash from text file is present.
            foreach (var kv in map)
                entryHashes.Contains(kv.Key).Should().BeTrue($"Failed to find hash for file: {kv.Value}. With hash {kv.Key:X}");
        }
    }

    //[Fact]
    public void Repacking_RealData_RoundTrips()
    {
        const RepackingMode mode = RepackingMode.FullSolidBlock;
        const int iterations = 100;
        const string folderPath = "/home/sewer/Temp/Repack-Samples/SMIM SE 2-08-659-2-08";
        const string hashMapPath = "Assets/SMIM SE 2-08-659-2-08-Hashes.txt";
        var compressionLevels = new[] { 1 };
        var random = new Random();

        for (var x = 0; x < iterations; x++)
        {
            // Preload all original files into memory
            var originalFiles = PreloadOriginalFiles(folderPath);

            var compLevel = compressionLevels[x % compressionLevels.Length];
            var (deduplicateSolid, deduplicateChunked) = GetSettingsForMode(mode);
            var packedInitial = PackWithFilesFromDirectory(folderPath, deduplicateSolid, deduplicateChunked, compLevel, compLevel, BlockSize, ChunkSize);
            var map = CreateHashToStringMap(hashMapPath);

            packedInitial.Position = 0;
            var initialProvider = new FromStreamProvider(packedInitial);
            var initialHeader = HeaderParser.ParseHeader(initialProvider);

            // Create a new archive reusing files from the initial archive based on the repacking mode
            var repackerBuilder = new NxRepackerBuilder();
            repackerBuilder.WithOutput(new MemoryStream());
            repackerBuilder.WithSolidDeduplication(deduplicateSolid);
            repackerBuilder.WithChunkedDeduplication(deduplicateChunked);
            repackerBuilder.WithSolidCompressionLevel(compLevel);
            repackerBuilder.WithChunkedLevel(compLevel);
            repackerBuilder.WithBlockSize(BlockSize);
            repackerBuilder.WithChunkSize(ChunkSize);

            var entriesToRepack = SelectEntriesToRepack(initialHeader.Entries, mode, random, initialHeader);
            repackerBuilder.AddFilesFromNxArchive(initialProvider, initialHeader, entriesToRepack);

            using var repackedArchive = repackerBuilder.Build(false);
            repackedArchive.Position = 0;

            // Test succeeds if it doesn't throw.
            var unpacker = new NxUnpacker(new FromStreamProvider(repackedArchive), true);
            var entries = unpacker.GetPathedFileEntries();
            var unpackSettings = new UnpackerSettings();
            unpackSettings.MaxNumThreads = 1; // speedup: avoid spawning extra threads for small files.

            for (var y = 0; y < entries.Length; y++)
            {
                testOutputHelper.WriteLine("Mode: {0}, Iteration: {1}, File Idx: {2}, File: {3}", mode, x, y, entries[y].FilePath);
                var entry = entries[y];
                var entryData = unpacker.ExtractFilesInMemory(new[] { entry.Entry }, unpackSettings);
                var entryHash = entryData[0].Data.XxHash64();

                // Get the original file.
                if (!originalFiles.TryGetValue(entry.FilePath, out var originalFileContent))
                    Assert.Fail($"Original file not found: {entry.FilePath}");

                // Verify Length
                entryData[0].Data.Length.Should().Be(originalFileContent.Length,
                    $"Content length mismatch for file: {entry.FilePath}." +
                    $"Extracted: {entryData[0].Data.Length}," +
                    $"Original: {originalFileContent.Length}");

                // Verify content.
                var location = GetMismatchLocation(entryData, originalFileContent);
                if (location >= 0)
                {
                    Assert.Fail($"Content mismatch for file: {entry.FilePath} at offset {location}.\n" +
                                $"Extracted byte: 0x{entryData[0].Data[location]:X2}, Original byte: 0x{originalFileContent[location]:X2}.\n" +
                                $"Entry: Block Idx/{entry.Entry.FirstBlockIndex}, File Idx/{y} Offset/{entry.Entry.DecompressedBlockOffset}, Size/{entry.Entry.DecompressedSize}");
                }

                // Verify extracted file hash matches the stored header data
                entryHash.Should().Be(entry.Entry.Hash, $"Hash mismatch for file: {entry.FilePath}");
            }

            // Verify that each repacked file's hash is present in the original hash map
            foreach (var entry in entries)
            {
                map.Should().ContainKey(entry.Entry.Hash, $"Failed to find hash for repacked file: {entry.FilePath}. With hash {entry.Entry.Hash:X}");
            }
        }
    }

    private static (bool deduplicateSolid, bool deduplicateChunked) GetSettingsForMode(RepackingMode mode)
    {
        return mode switch
        {
            RepackingMode.ChunkedFile => (false, true),
            RepackingMode.PartialSolidBlock => (true, false),
            RepackingMode.FullSolidBlock => (true, false),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private static Span<FileEntry> SelectEntriesToRepack(Span<FileEntry> entries, RepackingMode mode, Random random, ParsedHeader header)
    {
        return mode switch
        {
            RepackingMode.ChunkedFile => SelectChunkedFiles(entries, random),
            RepackingMode.PartialSolidBlock => SelectPartialSolidBlockFiles(entries, random, header),
            RepackingMode.FullSolidBlock => SelectFullSolidBlockFiles(entries, header),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private static Span<FileEntry> SelectChunkedFiles(Span<FileEntry> entries, Random random)
    {
        var chunkedFiles = new List<FileEntry>();
        foreach (var entry in entries)
        {
            if (entry.DecompressedSize > ChunkSize)
                chunkedFiles.Add(entry);
        }

        // Randomly select up to half of the chunked files
        return SelectRandomSubset(chunkedFiles.ToArray(), random.Next(1, (chunkedFiles.Count / 2) + 1));
    }

    private static unsafe Span<FileEntry> SelectPartialSolidBlockFiles(Span<FileEntry> entries, Random random, ParsedHeader header)
    {
        var blockCount = header.Blocks.Length;
        using var blockList = new BlockList<FileEntry>(blockCount, entries.Length);
        var origArchiveBlockFileCounts = GetOriginalArchiveBlockCounts(header, blockCount);

        // Group files by their block index
        foreach (var entry in entries)
        {
            var blockIndex = entry.FirstBlockIndex;
            var list = blockList.GetOrCreateList(blockIndex, origArchiveBlockFileCounts[blockIndex]);
            list->Push(entry);
        }

        var selectedEntries = new List<FileEntry>();

        // Randomly select some files from each block
        foreach (var block in blockList.GetAllBlocks())
        {
            if (!block.IsValid || block.Count <= 0)
                continue;

            var blockEntries = block.AsSpan();
            var selectionCount = random.Next(1, blockEntries.Length + 1);
            var selectedBlockEntries = SelectRandomSubset(blockEntries, selectionCount);

            selectedEntries.AddRange(selectedBlockEntries.ToArray());
        }

        return selectedEntries.ToArray();
    }

    private static unsafe Span<FileEntry> SelectFullSolidBlockFiles(Span<FileEntry> entries, ParsedHeader header)
    {
        var blockCount = header.Blocks.Length;
        using var blockList = new BlockList<FileEntry>(blockCount, entries.Length);
        var origArchiveBlockFileCounts = GetOriginalArchiveBlockCounts(header, blockCount);

        // Group files by their block index
        foreach (var entry in entries)
        {
            var blockIndex = entry.FirstBlockIndex;
            var list = blockList.GetOrCreateList(blockIndex, origArchiveBlockFileCounts[blockIndex]);
            list->Push(entry);
        }

        var selectedEntries = new List<FileEntry>();

        // Select all files from each valid block
        var numValidBlocks = 0;
        foreach (var block in blockList.GetAllBlocks())
        {
            // <= 1 to avoid single chunk blocks
            if (!block.IsValid || block.Count <= 1)
                continue;

            numValidBlocks++;
        }

        var numSelectedBlocks = 0;
        var numBlocksToSelect = new Random().Next(0, numValidBlocks + 1);
        foreach (var block in blockList.GetAllBlocks())
        {
            // <= 1 to avoid single chunk blocks
            if (!block.IsValid || block.Count <= 1)
                continue;

            if (numSelectedBlocks < numBlocksToSelect)
            {
                selectedEntries.AddRange(block.AsSpan().ToArray());
                numSelectedBlocks++;
            }
            else
                break;
        }

        return selectedEntries.ToArray();
    }

    private static int[] GetOriginalArchiveBlockCounts(ParsedHeader header, int blockCount)
    {
        var origArchiveBlockFileCounts = new int[blockCount];
        foreach (var entry in header.Entries)
            origArchiveBlockFileCounts.DangerousGetReferenceAt(entry.FirstBlockIndex)++;

        return origArchiveBlockFileCounts;
    }

    private static Span<FileEntry> SelectRandomSubset(Span<FileEntry> entries, int count)
    {
        var random = new Random();
        var result = new FileEntry[count];
        var indices = new HashSet<int>();

        for (var i = 0; i < count; i++)
        {
            int index;
            do
            {
                index = random.Next(entries.Length);
            } while (!indices.Add(index));

            result[i] = entries[index];
        }

        return result;
    }

    private static Stream PackWithFilesFromDirectory(string directoryPath, bool deduplicateSolid, bool deduplicateChunked, int solidCompressionLevel, int chunkedCompressionLevel, int blockSize, int chunkSize)
    {
        var output = new MemoryStream();
        var builder = new NxPackerBuilder();
        builder.AddFolder(directoryPath);
        builder.WithOutput(output);
        builder.WithBlockSize(blockSize);
        builder.WithChunkSize(chunkSize);
        builder.WithSolidDeduplication(deduplicateSolid);
        builder.WithChunkedDeduplication(deduplicateChunked);
        builder.WithSolidCompressionLevel(solidCompressionLevel);
        builder.WithChunkedLevel(chunkedCompressionLevel);
        return builder.Build(false);
    }

    private static Dictionary<ulong, string> CreateHashToStringMap(string hashesFilePath)
    {
        var hashToPathMap = new Dictionary<ulong, string>();

        foreach (var entry in File.ReadAllLines(hashesFilePath))
        {
            var parts = entry.Split(',');
            hashToPathMap[Convert.ToUInt64(parts[1], 16)] = parts[0];
        }

        return hashToPathMap;
    }

    private Dictionary<string, byte[]> PreloadOriginalFiles(string basePath)
    {
        var originalFiles = new Dictionary<string, byte[]>();
        foreach (var filePath in Directory.GetFiles(basePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(basePath, filePath);
            originalFiles[relativePath] = File.ReadAllBytes(filePath);
        }
        return originalFiles;
    }

    private static int GetMismatchLocation(OutputArrayProvider[] entryData, byte[] originalFileContent)
    {
        for (var i = 0; i < entryData[0].Data.Length; i++)
        {
            if (entryData[0].Data[i] != originalFileContent[i])
                return i;
        }

        return -1;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public enum RepackingMode
    {
        ChunkedFile,
        PartialSolidBlock,
        FullSolidBlock
    }
}
