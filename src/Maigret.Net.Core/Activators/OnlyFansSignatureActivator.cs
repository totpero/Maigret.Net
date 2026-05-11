// Port of activation.ParsingActivator.onlyfans — computes the signed-request
// headers expected by the OnlyFans API. Signing rules (static_param,
// checksum_indexes, checksum_constant, format) are stored in data.json under
// `OnlyFans.activation` and rotate upstream every 1–3 weeks.

using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maigret.Net.Core.Activators;

/// <summary>
/// Computes the rotating signed headers (<c>time</c>, <c>sign</c>) expected by
/// the OnlyFans API. On first call also fetches the bootstrap cookies.
/// </summary>
public sealed class OnlyFansSignatureActivator : ISiteActivator
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public OnlyFansSignatureActivator(HttpClient client, ILogger<OnlyFansSignatureActivator>? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = (ILogger?)logger ?? NullLogger.Instance;
    }

    public string Method => "onlyfans";

    public async Task ActivateAsync(MaigretSite site, string? probedUrl, CancellationToken cancellationToken = default)
    {
        var act = site.Activation;
        var staticParam = ReadString(act, "static_param") ?? throw MissingField(site, "static_param");
        var indexes = ReadIntArray(act, "checksum_indexes") ?? throw MissingField(site, "checksum_indexes");
        var constant = ReadInt(act, "checksum_constant") ?? throw MissingField(site, "checksum_constant");
        var format = ReadString(act, "format") ?? throw MissingField(site, "format");
        var initUrl = ReadString(act, "url") ?? throw MissingField(site, "url");

        var userId = site.Headers.TryGetValue("user-id", out var u) && !string.IsNullOrEmpty(u) ? u : "0";

        if (!site.Headers.TryGetValue("x-bc", out var xbc) || xbc.Trim('0').Length == 0)
        {
            site.Headers["x-bc"] = RandomHex(20);
        }

        if (!site.Headers.ContainsKey("cookie"))
        {
            var initPath = new Uri(initUrl).AbsolutePath;
            var (t, sg) = Sign(initPath, staticParam, userId, indexes, constant, format);

            using var request = new HttpRequestMessage(HttpMethod.Get, initUrl);
            foreach (var (k, v) in site.Headers)
            {
                if (k.Equals("cookie", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                request.Headers.TryAddWithoutValidation(k, v);
            }
            request.Headers.TryAddWithoutValidation("time", t);
            request.Headers.TryAddWithoutValidation("sign", sg);

            using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                var jar = string.Join("; ", cookies.Select(c => c.Split(';', 2)[0]));
                if (!string.IsNullOrEmpty(jar))
                {
                    site.Headers["cookie"] = jar;
                }
            }
        }

        var targetPath = !string.IsNullOrEmpty(probedUrl)
            ? new Uri(probedUrl!).AbsolutePath
            : new Uri(initUrl).AbsolutePath;

        var (time, sign) = Sign(targetPath, staticParam, userId, indexes, constant, format);
        site.Headers["time"] = time;
        site.Headers["sign"] = sign;
        _logger.LogDebug("OnlyFans signed {Path} time={Time}", targetPath, time);
    }

    private static (string Time, string Sign) Sign(string path, string staticParam, string userId, int[] indexes, int constant, string format)
    {
        var t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        var msg = $"{staticParam}\n{t}\n{path}\n{userId}";

        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(msg));
        var sha = Convert.ToHexString(bytes).ToLowerInvariant();

        var cs = indexes.Sum(i => sha[i]) + constant;
        var sign = string.Format(CultureInfo.InvariantCulture, format, sha, Math.Abs(cs));
        return (t, sign);
    }

    private static string RandomHex(int byteLen)
    {
        Span<byte> buf = stackalloc byte[byteLen];
        RandomNumberGenerator.Fill(buf);
        return Convert.ToHexString(buf).ToLowerInvariant();
    }

    private static string? ReadString(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object &&
        el.TryGetProperty(name, out var p) &&
        p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static int? ReadInt(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object &&
        el.TryGetProperty(name, out var p) &&
        p.ValueKind == JsonValueKind.Number &&
        p.TryGetInt32(out var v) ? v : null;

    private static int[]? ReadIntArray(JsonElement el, string name)
    {
        if (el.ValueKind != JsonValueKind.Object ||
            !el.TryGetProperty(name, out var p) ||
            p.ValueKind != JsonValueKind.Array)
        {
            return null;
        }
        var list = new List<int>(p.GetArrayLength());
        foreach (var item in p.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var v))
            {
                list.Add(v);
            }
        }
        return list.ToArray();
    }

    private static InvalidOperationException MissingField(MaigretSite site, string field) =>
        new($"OnlyFans activation for {site.Name} is missing '{field}'.");
}
