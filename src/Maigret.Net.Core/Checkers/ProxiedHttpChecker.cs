// Port of checking.ProxiedAiohttpChecker — same probe logic over a SOCKS/HTTP proxy.
using System.Net;
using Microsoft.Extensions.Logging;

namespace Maigret.Net.Core.Checkers;

/// <summary>
/// HTTP checker variant that routes traffic through a proxy. .NET's
/// <see cref="System.Net.Http.SocketsHttpHandler"/> supports <c>http://</c>,
/// <c>https://</c>, and <c>socks{4,4a,5}://</c> proxy schemes natively (since .NET 6).
/// </summary>
public sealed class ProxiedHttpChecker : SimpleHttpChecker
{
    public ProxiedHttpChecker(string proxyUrl, ILogger? logger = null, CookieContainer? cookies = null)
        : base(logger, BuildProxy(proxyUrl), cookies)
    {
    }

    private static IWebProxy BuildProxy(string proxyUrl)
    {
        if (string.IsNullOrEmpty(proxyUrl))
        {
            throw new ArgumentException("proxyUrl must be non-empty", nameof(proxyUrl));
        }

        return new WebProxy(proxyUrl);
    }
}
