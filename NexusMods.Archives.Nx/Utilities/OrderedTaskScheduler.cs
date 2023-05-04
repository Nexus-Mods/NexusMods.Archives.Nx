using System.Collections.Concurrent;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     An implementation of <see cref="TaskScheduler" /> which executes tasks in the order they are submitted.
/// </summary>
/// <remarks>
///     This class has 2 purposes:<br />
///     - Limit number of threads.<br />
///     - Process items in-order.<br />
///     Usage: `using var scheduler = new OrderedTaskScheduler(threadCount)` then `scheduler.Enqueue(method)`.
///     Scheduler will wait for all threads to finish on dispose, and run in parallel in the meantime.
///     Manually call <see cref="Dispose" /> if you need results.
/// </remarks>
internal class OrderedTaskScheduler : TaskScheduler, IDisposable
{
    private readonly BlockingCollection<Task> _tasks = new();
    private readonly Thread[] _threads;

    public OrderedTaskScheduler(int concurrencyLevel)
    {
        if (concurrencyLevel < 1)
            throw new ArgumentOutOfRangeException(nameof(concurrencyLevel));

        _threads = new Thread[concurrencyLevel];
        for (var x = 0; x < _threads.Length; x++)
        {
            var thread = new Thread(ExecuteTasks);
            thread.IsBackground = true;
            thread.Start();
            _threads[x] = thread;
        }
    }

    protected override void QueueTask(Task task) => _tasks.Add(task);

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) =>
        // Always use the dedicated threads to maintain the order.
        false;

    protected override IEnumerable<Task> GetScheduledTasks() => _tasks.ToArray();

    private void ExecuteTasks()
    {
        while (!_tasks.IsCompleted)
        {
            try
            {
                var task = _tasks.Take();
                TryExecuteTask(task);
            }
            catch (InvalidOperationException)
            {
                // An InvalidOperationException means that the BlockingCollection is empty and completed.
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _tasks.CompleteAdding();
        foreach (var thread in _threads)
            thread.Join();
    }
}
