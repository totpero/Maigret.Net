// Port of checking.SimpleAiohttpChecker — async HTTP probe over HttpClient.
// State-less: a single instance can be shared across many concurrent workers
// because HttpClient itself is thread-safe.
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maigret.Net.Core.Checkers;

/// <summary>
/// Default HTTP checker. Mirrors <c>checking.SimpleAiohttpChecker</c>:
/// disables certificate validation, supports custom headers, and maps known
/// transport exceptions to <see cref="CheckError"/> instances.
/// </summary>
public class SimpleHttpChecker : ICheckerBase
{
    private readonly ILogger _logger;
    private readonly bool _ownsClient;
    private readonly HttpClient _client;

    public SimpleHttpChecker(HttpClient client, ILogger? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? NullLogger.Instance;
        _ownsClient = false;
    }

    /// <summary>
    /// Convenience overload that creates an internal <see cref="HttpClient"/>
    /// with cert validation disabled — analogous to the aiohttp default in Python.
    /// </summary>
    public SimpleHttpChecker(ILogger? logger = null, IWebProxy? proxy = null, CookieContainer? cookies = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _ownsClient = true;

        var handler = new SocketsHttpHandler
        {
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
            },
            AllowAutoRedirect = true,
            UseCookies = cookies is not null,
            CookieContainer = cookies ?? new CookieContainer(),
            Proxy = proxy,
            UseProxy = proxy is not null,
        };
        _client = new HttpClient(handler);
    }

    public virtual async Task<CheckResponse> CheckAsync(
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
            return new CheckResponse(string.Empty, 0, new CheckError("Unexpected", "Url required"));
        }

        try
        {
            var verb = (method ?? "get").ToLowerInvariant();
            var httpMethod = verb switch
            {
                "post" => HttpMethod.Post,
                "head" => HttpMethod.Head,
                "put" => HttpMethod.Put,
                "delete" => HttpMethod.Delete,
                _ => HttpMethod.Get,
            };

            using var request = new HttpRequestMessage(httpMethod, url);
            ApplyHeaders(request, headers);

            if (payload is not null && verb == "post")
            {
                request.Content = BuildPayloadContent(payload, headers);
            }

            var completion = allowRedirects ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (timeout is { } t && t > TimeSpan.Zero)
            {
                timeoutCts.CancelAfter(t);
            }

            using var response = await _client.SendAsync(request, completion, timeoutCts.Token).ConfigureAwait(false);

            var statusCode = (int)response.StatusCode;
            var body = response.Content is null
                ? string.Empty
                : await ReadBodyAsync(response, timeoutCts.Token).ConfigureAwait(false);

            CheckError? error = statusCode == 0 ? new CheckError("Connection lost") : null;
            return new CheckResponse(body, statusCode, error);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new CheckResponse(string.Empty, 0, new CheckError("Request timeout", ex.Message));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new CheckResponse(string.Empty, 0, new CheckError("Interrupted"));
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException sock)
        {
            return new CheckResponse(string.Empty, 0, new CheckError("Connecting failure", sock.Message));
        }
        catch (HttpRequestException ex)
        {
            return new CheckResponse(string.Empty, 0, new CheckError("HTTP", ex.Message));
        }
        catch (IOException ex)
        {
            return new CheckResponse(string.Empty, 0, new CheckError("Connection lost", ex.Message));
        }
        catch (System.Security.Authentication.AuthenticationException ex)
        {
            return new CheckResponse(string.Empty, 0, new CheckError("SSL", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unexpected error checking {Url}", url);
            return new CheckResponse(string.Empty, 0, new CheckError("Unexpected", ex.Message));
        }
    }

    private static void ApplyHeaders(HttpRequestMessage request, IReadOnlyDictionary<string, string>? headers)
    {
        if (headers is null)
        {
            return;
        }

        foreach (var (k, v) in headers)
        {
            // Content-Type belongs on the content, not the request — set once content is built.
            if (string.Equals(k, "Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            request.Headers.TryAddWithoutValidation(k, v);
        }
    }

    private static HttpContent BuildPayloadContent(object payload, IReadOnlyDictionary<string, string>? headers)
    {
        var contentType = headers is not null && headers.TryGetValue("Content-Type", out var ct) ? ct : null;

        if (string.Equals(contentType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) &&
            payload is IDictionary<string, string> formDict)
        {
            return new FormUrlEncodedContent(formDict);
        }

        var json = payload is string s ? s : JsonSerializer.Serialize(payload);
        return new StringContent(json, Encoding.UTF8, contentType ?? "application/json");
    }

    private static async Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        var charset = response.Content.Headers.ContentType?.CharSet;
        var encoding = TryGetEncoding(charset) ?? Encoding.UTF8;
        return encoding.GetString(bytes);
    }

    private static Encoding? TryGetEncoding(string? charset)
    {
        if (string.IsNullOrEmpty(charset))
        {
            return null;
        }

        try { return Encoding.GetEncoding(charset); }
        catch (ArgumentException) { return null; }
    }

    public ValueTask DisposeAsync()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }

        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
