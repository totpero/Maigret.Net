// Port of maigret/executors.py — async executor primitives.
// Python uses asyncio.Queue; .NET uses Channel<T> for the same producer/consumer
// pattern, with SemaphoreSlim controlling worker concurrency.

using System.Threading.Channels;

namespace Maigret.Net.Core;

/// <summary>
/// Streaming queue executor: enqueues a batch of async work items and yields
/// each result via <see cref="IAsyncEnumerable{T}"/> as soon as it completes.
/// .NET counterpart of <c>maigret.executors.AsyncioQueueGeneratorExecutor</c>.
/// </summary>
/// <typeparam name="TResult">Result type produced by each work item.</typeparam>
public sealed class QueueGeneratorExecutor<TResult>
{
    private readonly int _workersCount;
    private readonly TimeSpan? _timeout;

    public QueueGeneratorExecutor(int workersCount = 10, TimeSpan? timeout = null)
    {
        if (workersCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workersCount), "must be > 0");
        }

        _workersCount = workersCount;
        _timeout = timeout;
    }

    /// <summary>Time spent in the most recent <see cref="RunAsync"/> call.</summary>
    public TimeSpan ExecutionTime { get; private set; }

    /// <summary>
    /// Streams results as work items finish. Each task is awaited with the executor's
    /// timeout (if set); on timeout, the work item's <paramref name="defaultResult"/>
    /// is yielded instead.
    /// </summary>
    public async IAsyncEnumerable<TResult> RunAsync(
        IEnumerable<Func<CancellationToken, Task<TResult>>> tasks,
        TResult defaultResult = default!,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var taskList = tasks.ToList();
        if (taskList.Count == 0)
        {
            yield break;
        }

        var workers = Math.Min(_workersCount, taskList.Count);

        var input = Channel.CreateUnbounded<Func<CancellationToken, Task<TResult>>>(
            new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });
        var output = Channel.CreateUnbounded<TResult>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        foreach (var item in taskList)
        {
            input.Writer.TryWrite(item);
        }

        input.Writer.Complete();

        var start = DateTime.UtcNow;

        var workerTasks = Enumerable.Range(0, workers)
            .Select(_ => Task.Run(() => WorkerLoop(input.Reader, output.Writer, defaultResult, cancellationToken), cancellationToken))
            .ToArray();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.WhenAll(workerTasks).ConfigureAwait(false);
            }
            finally
            {
                output.Writer.TryComplete();
            }
        }, cancellationToken);

        await foreach (var result in output.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return result;
        }

        ExecutionTime = DateTime.UtcNow - start;
    }

    private async Task WorkerLoop(
        ChannelReader<Func<CancellationToken, Task<TResult>>> input,
        ChannelWriter<TResult> output,
        TResult defaultResult,
        CancellationToken cancellationToken)
    {
        while (await input.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (input.TryRead(out var work))
            {
                TResult result;
                try
                {
                    if (_timeout is { } timeout)
                    {
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCts.CancelAfter(timeout);
                        result = await work(timeoutCts.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        result = await work(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    result = defaultResult;
                }
                catch
                {
                    // Mirror Python: errors logged elsewhere; emit default to keep stream going.
                    result = defaultResult;
                }

                await output.WriteAsync(result, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
