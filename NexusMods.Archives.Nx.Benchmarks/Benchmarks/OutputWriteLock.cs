using System.Collections.Concurrent;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Benchmarks.Benchmarks;

/// <summary>
/// This benchmark is to measure the performance of different approaches to locking the output file for writing
/// individual blocks. Results of this will be used to determine final implementation.
/// </summary>
/// <remarks>
///     Assumes CPU scheduler will be completely fair; results may vary based on scheduler.
/// </remarks>
[SimpleJob(1, 1, 1, 1)]
public class OutputWriteLock
{
    // ~80MB/s is for HDD (compensating for read)
    // ~250MB/s for SSD
    // ~3GB/s for NVMe
    // ~32GB/s for RAM
    [Params(80, 250, 3000, 32000)]
    public int DiskWriteSpeedMB { get; set; } 
    
    // Common thread counts
    [Params(/*4, 8,*/ 12, 24/*, 16, 32*/)]
    public int NumThreads { get; set; }
    
    // 1 MB for SOLID Blocks
    // 64 MB for CHUNKED Blocks
    [Params(0.03125F, 0.125F, 0.5F, 1F)]
    public float BlockSizeMB { get; set; }
    
    // ~40MB/s ZSTD -16 on modern Ryzen 5XXX. (and close to LZ4 -9)
    // ~22MB/s LZ4 -12 on modern Ryzen 5XXX.
    [Params(/*22, */40)]
    public int CompressionSpeedMB { get; set; }

    private readonly int _totalMB = 1000;
    private OrderedTaskScheduler _scheduler = null!;

    [IterationSetup]
    public void Setup()
    {
        _scheduler = new OrderedTaskScheduler(NumThreads);
    }

    /// <summary>
    /// This benchmark does not stall execution, but takes the lock immediately when possible.
    /// In this case we would write the block write order into the TOC.
    /// </summary>
    [Benchmark]
    public void Stall_IgnoringOrder()
    {
        var blocks = _totalMB / BlockSizeMB;
        for (var x = 0; x < blocks; x++)
            Task.Factory.StartNew(StallWriteIgnoreOrder, CancellationToken.None, TaskCreationOptions.None, _scheduler);
        
        // Joins the threads.
        _scheduler.Dispose();
    }
    
    private void StallWriteIgnoreOrder()
    {
        SimulateCompress();
        lock (_scheduler) // contention by lock.
            SimulateWrite();
    }

    /// <summary>
    /// This benchmark queues execution, and does not stall, but suffers penalty of a memory copy.
    /// </summary>
    /// <remarks>
    ///     This test is actually flawed and will produce results better than they should be.
    ///     I never fixed this benchmark, because I have pulled enough information out with existing code here.
    /// </remarks>
    [Benchmark]
    public void Queue()
    {
        var blocks = _totalMB / BlockSizeMB;
        for (var x = 0; x < blocks; x++)
            Task.Factory.StartNew(QueueWrite, CancellationToken.None, TaskCreationOptions.None, _scheduler);
        
        // Joins the threads.
        _scheduler.Dispose();
    }

    private ConcurrentQueue<byte[]> _queue = new();
    private byte[] _queueDummy = Array.Empty<byte>();
    
    private void QueueWrite()
    {
        SimulateCompress();

        // This code is flawed.
        while (_queue.Count > 2)
            Thread.Yield();
            
        // Simulate memory copy.
        if (_queue.Count == 0)
            _queue.Enqueue(_queueDummy); // we queued for free, no memory copy.
        else
            _queue.Enqueue(new byte[(int)(BlockSizeMB * 0.66)]); // memory copy was required, because something else was holding lock
            
        // But we won't stall at any point unless write thread is overwhelmed.
        SimulateWrite();
        _queue.TryDequeue(out var item);
    }
    
    /// <summary>
    /// This benchmark stalls execution until the previous in-order runner has finished.
    /// </summary>
    [Benchmark]
    public void Stall()
    {
        var blocks = _totalMB / BlockSizeMB;
        _currentBlock = 0;
        for (var x = 0; x < blocks; x++)
            Task.Factory.StartNew(StallWrite_Ordered, x, CancellationToken.None, TaskCreationOptions.None, _scheduler);
        
        // Joins the threads.
        _scheduler.Dispose();
    }
    
    private int _currentBlock = 0;
    
    private void StallWrite_Ordered(object? indexObj)
    {
        var index = (int)indexObj!;
        SimulateCompress();

        // Wait until it's our turn to write.
        var spinWait = new SpinWait();
        while (_currentBlock != index)
            spinWait.SpinOnce(-1);
        
        SimulateWrite();
        Interlocked.Increment(ref _currentBlock);
    }

    /// <summary>
    /// We simulate an actual CPU load, to see what scheduler would do under actual pressure.
    /// This makes the test more realistic as under a real situation, threads would be active compressing.
    /// </summary>
    private void SimulateCompress()
    {
        var compressTimeMs = (BlockSizeMB) * 1000f / CompressionSpeedMB;
        var watch = Stopwatch.StartNew();

        while (watch.Elapsed.TotalMilliseconds < compressTimeMs)
            Thread.SpinWait(1);
    }
    
    /// <summary>
    /// Simulate writing data to disk.
    /// </summary>
    private void SimulateWrite()
    {
        // assume 0.66 compression ratio
        var writeTimeMs = (BlockSizeMB * 0.66f) * 1000f / DiskWriteSpeedMB;
        var watch = Stopwatch.StartNew();
        
        while (watch.Elapsed.TotalMilliseconds < writeTimeMs)
            Thread.SpinWait(1);
        
        // Storage can be unpredictable, so yield is a decent compromise that's random enough, and more accurate than sleep.
        
        // Note: Windows has sleep granularity of 15.6ms, so we can't simulate this well with sleep.
        //       We have to spin a bit.
    }
}
