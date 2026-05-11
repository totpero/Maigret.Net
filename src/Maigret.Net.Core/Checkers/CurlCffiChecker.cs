// Placeholder for checking.CurlCffiChecker — TLS-fingerprinting bypass for sites
// behind Cloudflare with strict JA3 signatures. Not implemented in P0; sites that
// require it surface as Status=Waf via the "TLS impersonation unavailable" error.
namespace Maigret.Net.Core.Checkers;

/// <summary>
/// Stub for the curl_cffi-based checker. Returns a sentinel error so callers can
/// surface a clear "TLS impersonation unavailable" status without throwing.
/// </summary>
public sealed class CurlCffiChecker : ICheckerBase
{
    public Task<CheckResponse> CheckAsync(
        string url,
        IReadOnlyDictionary<string, string>? headers = null,
        bool allowRedirects = true,
        TimeSpan? timeout = null,
        string method = "get",
        object? payload = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new CheckResponse(
            string.Empty,
            0,
            new CheckError("Bot protection", "TLS impersonation unavailable in .NET P0; install curl_cffi-equivalent")));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
