// Common contract for checker classes — one per (site, request) probe.
namespace Maigret.Net.Core.Checkers;

/// <summary>
/// Stateless probe contract. Multiple workers may share a single checker
/// instance and call <see cref="CheckAsync"/> concurrently — request data is
/// passed in directly rather than staged. Implementations must be thread-safe
/// w.r.t. concurrent calls (HttpClient already is).
/// </summary>
public interface ICheckerBase : IAsyncDisposable
{
    /// <summary>
    /// Probes a URL. Returns the response body, HTTP status code, and an optional
    /// <see cref="CheckError"/> describing why the probe failed.
    /// </summary>
    public Task<CheckResponse> CheckAsync(
        string url,
        IReadOnlyDictionary<string, string>? headers = null,
        bool allowRedirects = true,
        TimeSpan? timeout = null,
        string method = "get",
        object? payload = null,
        CancellationToken cancellationToken = default);
}
