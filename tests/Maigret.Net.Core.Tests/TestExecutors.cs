using Maigret.Net.Core;
using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestExecutors
{
    [Fact]
    public async Task QueueGeneratorExecutor_StreamsAllResults()
    {
        var executor = new QueueGeneratorExecutor<int>(workersCount: 4);
        var tasks = Enumerable.Range(0, 20)
            .Select(n => (Func<CancellationToken, Task<int>>)(async ct =>
            {
                await Task.Delay(5, ct).ConfigureAwait(false);
                return n;
            }));

        var results = new List<int>();
        await foreach (var r in executor.RunAsync(tasks).ConfigureAwait(false))
        {
            results.Add(r);
        }

        results.Count.ShouldBe(20);
        results.OrderBy(x => x).ShouldBe(Enumerable.Range(0, 20));
        executor.ExecutionTime.TotalMilliseconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task QueueGeneratorExecutor_TimeoutSwapsInDefault()
    {
        var executor = new QueueGeneratorExecutor<int>(workersCount: 2, timeout: TimeSpan.FromMilliseconds(50));
        var tasks = new[]
        {
            (Func<CancellationToken, Task<int>>)(async ct => { await Task.Delay(500, ct).ConfigureAwait(false); return 1; }),
            (Func<CancellationToken, Task<int>>)(async ct => { await Task.Delay(5, ct).ConfigureAwait(false); return 2; }),
        };

        var results = new List<int>();
        await foreach (var r in executor.RunAsync(tasks, defaultResult: -1).ConfigureAwait(false))
        {
            results.Add(r);
        }

        results.Count.ShouldBe(2);
        results.ShouldContain(2);
        results.ShouldContain(-1);
    }

    [Fact]
    public async Task QueueGeneratorExecutor_EmptyInput_YieldsNothing()
    {
        var executor = new QueueGeneratorExecutor<int>(workersCount: 4);
        var results = new List<int>();
        await foreach (var r in executor.RunAsync(Array.Empty<Func<CancellationToken, Task<int>>>()).ConfigureAwait(false))
        {
            results.Add(r);
        }

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task QueueGeneratorExecutor_FailingTask_ReturnsDefault()
    {
        var executor = new QueueGeneratorExecutor<int>(workersCount: 2);
        var tasks = new[]
        {
            (Func<CancellationToken, Task<int>>)(_ => throw new InvalidOperationException("boom")),
            (Func<CancellationToken, Task<int>>)(_ => Task.FromResult(42)),
        };

        var results = new List<int>();
        await foreach (var r in executor.RunAsync(tasks, defaultResult: -1).ConfigureAwait(false))
        {
            results.Add(r);
        }

        results.Count.ShouldBe(2);
        results.ShouldContain(42);
        results.ShouldContain(-1);
    }
}
