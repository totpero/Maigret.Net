// Port of checking.CheckerMock — no-op probe used in tests.
namespace Maigret.Net.Core.Checkers;

/// <summary>
/// Test stub: <see cref="CheckAsync"/> always returns <see cref="CheckResponse.Empty"/>.
/// </summary>
public sealed class MockChecker : ICheckerBase
{
    public Task<CheckResponse> CheckAsync(
        string url,
        IReadOnlyDictionary<string, string>? headers = null,
        bool allowRedirects = true,
        TimeSpan? timeout = null,
        string method = "get",
        object? payload = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(CheckResponse.Empty);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
