// Port of checking.AiodnsDomainResolver — resolves a host name via DNS.
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maigret.Net.Core.Checkers;

/// <summary>
/// DNS-based domain resolver. Returns <c>(ip, 200, null)</c> when the host
/// resolves and <c>("", 404, null)</c> when it does not.
/// </summary>
public sealed class DomainResolver(ILogger? logger = null) : ICheckerBase
{
    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    public async Task<CheckResponse> CheckAsync(
        string url,
        IReadOnlyDictionary<string, string>? headers = null,
        bool allowRedirects = true,
        TimeSpan? timeout = null,
        string method = "get",
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(url))
        {
            return new CheckResponse(string.Empty, 0, new CheckError("DNS resolve error", "Url required"));
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (timeout is { } t && t > TimeSpan.Zero)
            {
                timeoutCts.CancelAfter(t);
            }

            var addresses = await Dns.GetHostAddressesAsync(url, timeoutCts.Token).ConfigureAwait(false);
            return addresses.Length == 0 ? new CheckResponse(string.Empty, 404, null) : new CheckResponse(addresses[0].ToString(), 200, null);
        }
        catch (SocketException)
        {
            return new CheckResponse(string.Empty, 404, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new CheckResponse(string.Empty, 0, new CheckError("Interrupted"));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "DNS resolve error for {Host}", url);
            return new CheckResponse(string.Empty, 0, new CheckError("DNS resolve error", ex.Message));
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
