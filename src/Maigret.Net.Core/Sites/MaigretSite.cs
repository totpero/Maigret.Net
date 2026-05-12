using System.Text.Json;
using System.Text.RegularExpressions;

namespace Maigret.Net.Core.Sites;

/// <summary>
/// A single site definition loaded from <c>data.json</c>. Holds detection metadata,
/// HTTP request configuration, presence/absence rules, tags, and engine binding.
/// Mirrors <c>maigret.sites.MaigretSite</c>.
/// </summary>
public sealed class MaigretSite
{
    private readonly List<string> _tags = [];
    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _errors = new(StringComparer.Ordinal);
    private readonly List<string> _presenseStrs = [];
    private readonly List<string> _absenceStrs = [];
    private readonly List<string> _protection = [];
    private readonly List<string> _mirrors = [];

    public MaigretSite(string name, JsonElement information)
    {
        Name = name;
        ApplyJson(information);

        if (AlexaRank <= 0)
        {
            AlexaRank = long.MaxValue;
        }

        UpdateDetectors();
    }

    /// <summary>Site display name.</summary>
    public string Name { get; }

    /// <summary>Username known to exist on the site.</summary>
    public string UsernameClaimed { get; set; } = string.Empty;

    /// <summary>Username known to not exist on the site.</summary>
    public string UsernameUnclaimed { get; set; } = string.Empty;

    /// <summary>Optional URL path component, e.g. <c>/forum</c>.</summary>
    public string UrlSubpath { get; set; } = string.Empty;

    /// <summary>Main site URL (the landing page).</summary>
    public string UrlMain { get; set; } = string.Empty;

    /// <summary>Profile URL pattern with <c>{username}</c> placeholder.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Whether the site is disabled by default (requires <c>--use-disabled</c>).</summary>
    public bool Disabled { get; set; }

    /// <summary>Positive results indicate similar usernames rather than exact matches.</summary>
    public bool SimilarSearch { get; set; }

    /// <summary>Whether to ignore HTTP 403 responses.</summary>
    public bool Ignore403 { get; set; }

    /// <summary>Site category tags (e.g. <c>social</c>, <c>photo</c>, country codes).</summary>
    public IReadOnlyList<string> Tags => _tags;

    /// <summary>Identifier type (default <c>username</c>; also <c>gaia_id</c>, <c>vk_id</c>, etc.).</summary>
    public string Type { get; set; } = IdTypes.Username;

    /// <summary>
    /// Custom HTTP headers sent with the request. Mutable by design — activators
    /// (Twitter/Vimeo/OnlyFans) write tokens into this dictionary before each probe.
    /// </summary>
    public IDictionary<string, string> Headers => _headers;

    /// <summary>Site-specific error messages (substring → human description).</summary>
    public IReadOnlyDictionary<string, string> Errors => _errors;

    /// <summary>Site activation rules (token fetching, signature generation, etc.).</summary>
    public JsonElement Activation { get; set; }

    /// <summary>Regex constraint for valid usernames on this site.</summary>
    public string? RegexCheck { get; set; }

    /// <summary>Optional URL used to probe the site (instead of the main profile URL).</summary>
    public string? UrlProbe { get; set; }

    /// <summary>Type of check to perform: <c>status_code</c>, <c>message</c>, <c>response_url</c>.</summary>
    public string CheckType { get; set; } = string.Empty;

    /// <summary>HTTP method (<c>GET</c>, <c>POST</c>, <c>HEAD</c>).</summary>
    public string RequestMethod { get; set; } = string.Empty;

    /// <summary>Request payload for non-GET methods.</summary>
    public JsonElement RequestPayload { get; set; }

    /// <summary>Whether to send only a HEAD request (Python keeps this as a string flag).</summary>
    public string? RequestHeadOnly { get; set; }

    /// <summary>Substrings indicating the profile exists.</summary>
    public IReadOnlyList<string> PresenseStrs => _presenseStrs;

    /// <summary>Substrings indicating the profile does not exist.</summary>
    public IReadOnlyList<string> AbsenceStrs => _absenceStrs;

    /// <summary>Engine name binding the site to a reusable rule bundle.</summary>
    public string? Engine { get; set; }

    /// <summary>Engine-specific configuration overrides.</summary>
    public JsonElement EngineData { get; set; }

    /// <summary>Resolved engine instance (set after <see cref="UpdateFromEngine"/>).</summary>
    public MaigretEngine? EngineObj { get; set; }

    /// <summary>Alexa traffic rank (lower = more popular). <c>long.MaxValue</c> means unknown.</summary>
    public long AlexaRank { get; set; } = long.MaxValue;

    /// <summary>For mirror sites, the parent platform's name.</summary>
    public string? Source { get; set; }

    /// <summary>URL protocol category (e.g. <c>http</c>, <c>https</c>, custom).</summary>
    public string Protocol { get; set; } = string.Empty;

    /// <summary>Detected protections on this site (e.g. <c>tls_fingerprint</c>, <c>ddos_guard</c>).</summary>
    public IReadOnlyList<string> Protection => _protection;

    /// <summary>Whether the username comparison is case sensitive (kept with the source typo).</summary>
    public bool CaseSentitive { get; set; }

    /// <summary>Optional URL substring that, when matched, indicates an error response.</summary>
    public string? ErrorUrl { get; set; }

    /// <summary>Mirror site URLs.</summary>
    public IReadOnlyList<string> Mirrors => _mirrors;

    /// <summary>Compiled regex used to detect usernames in profile URLs.</summary>
    public Regex? UrlRegexp { get; private set; }

    /// <summary>Pretty display name including the parent platform for mirrors.</summary>
    public string PrettyName => string.IsNullOrEmpty(Source) ? Name : $"{Name} [{Source}]";

    /// <summary>
    /// Recompiles <see cref="UrlRegexp"/> based on the current <see cref="Url"/>.
    /// </summary>
    public void UpdateDetectors()
    {
        if (string.IsNullOrEmpty(Url))
        {
            UrlRegexp = null;
            return;
        }

        var url = Url
            .Replace("{urlMain}", UrlMain, StringComparison.Ordinal)
            .Replace("{urlSubpath}", UrlSubpath, StringComparison.Ordinal);
        UrlRegexp = UrlMatcher.MakeProfileUrlRegexp(url, RegexCheck ?? string.Empty);
    }

    /// <summary>Tries to extract a username from a URL using <see cref="UrlRegexp"/>.</summary>
    public string? DetectUsername(string url)
    {
        if (UrlRegexp is null || string.IsNullOrEmpty(url))
        {
            return null;
        }

        var match = UrlRegexp.Match(url);
        if (!match.Success)
        {
            return null;
        }

        for (var i = match.Groups.Count - 1; i >= 1; i--)
        {
            var g = match.Groups[i].Value;
            if (!string.IsNullOrEmpty(g))
            {
                return g.TrimEnd('/');
            }
        }
        return null;
    }

    /// <summary>Extracts both the username and its <see cref="Type"/> from a URL.</summary>
    public (string Id, string Type)? ExtractIdFromUrl(string url)
    {
        var id = DetectUsername(url);
        return id is null ? null : (id, Type);
    }

    /// <summary>
    /// Returns a normalized URL template suitable for grouping sites in stats.
    /// Mirrors <c>MaigretSite.get_url_template</c>.
    /// </summary>
    public string GetUrlTemplate()
    {
        var url = UrlMatcher.ExtractMainPart(Url);
        if (url.StartsWith("{username}", StringComparison.Ordinal))
        {
            return "SUBDOMAIN";
        }

        if (string.IsNullOrEmpty(url))
        {
            return $"{Url} ({Engine ?? "no engine"})";
        }

        var firstSlash = url.IndexOf('/');
        return firstSlash < 0 ? "/" : url[firstSlash..];
    }

    /// <summary>
    /// Merges the rules from <paramref name="engine"/> into this site, mirroring
    /// <c>MaigretSite.update_from_engine</c>. Lists are appended, dictionaries are
    /// merged, scalars are overwritten.
    /// </summary>
    public MaigretSite UpdateFromEngine(MaigretEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        EngineObj = engine;
        if (engine.Site.ValueKind == JsonValueKind.Object)
        {
            ApplyJson(engine.Site, mergeMode: true);
        }
        UpdateDetectors();
        return this;
    }

    public override string ToString() => $"{Name} ({UrlMain})";

    private void ApplyJson(JsonElement information, bool mergeMode = false)
    {
        if (information.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var prop in information.EnumerateObject())
        {
            ApplyProperty(prop.Name, prop.Value, mergeMode);
        }
    }

    private void ApplyProperty(string camelName, JsonElement value, bool mergeMode)
    {
        switch (camelName)
        {
            case "usernameClaimed":
                UsernameClaimed = value.GetString() ?? string.Empty;
                break;
            case "usernameUnclaimed":
                UsernameUnclaimed = value.GetString() ?? string.Empty;
                break;
            case "urlSubpath":
                UrlSubpath = value.GetString() ?? string.Empty;
                break;
            case "urlMain":
                UrlMain = value.GetString() ?? string.Empty;
                break;
            case "url":
                Url = value.GetString() ?? string.Empty;
                break;
            case "disabled":
                Disabled = value.ValueKind == JsonValueKind.True;
                break;
            case "similarSearch":
                SimilarSearch = value.ValueKind == JsonValueKind.True;
                break;
            case "ignore403":
                Ignore403 = value.ValueKind == JsonValueKind.True;
                break;
            case "tags":
                MergeStringList(_tags, value, mergeMode);
                break;
            case "type":
                Type = value.GetString() ?? Type;
                break;
            case "headers":
                MergeStringDict(_headers, value);
                break;
            case "errors":
                MergeStringDict(_errors, value);
                break;
            case "activation":
                Activation = value.Clone();
                break;
            case "regexCheck":
                RegexCheck = value.GetString();
                break;
            case "urlProbe":
                UrlProbe = value.GetString();
                break;
            case "checkType":
                CheckType = value.GetString() ?? string.Empty;
                break;
            case "requestMethod":
                RequestMethod = value.GetString() ?? string.Empty;
                break;
            case "requestPayload":
                RequestPayload = value.Clone();
                break;
            case "requestHeadOnly":
                RequestHeadOnly = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
                break;
            case "presenseStrs":
            case "presenceStrs":
                MergeStringList(_presenseStrs, value, mergeMode);
                break;
            case "absenceStrs":
                MergeStringList(_absenceStrs, value, mergeMode);
                break;
            case "engine":
                Engine = value.GetString();
                break;
            case "engineData":
                EngineData = value.Clone();
                break;
            case "alexaRank":
                if (value.TryGetInt64(out var rank))
                {
                    AlexaRank = rank;
                }
                break;
            case "source":
                Source = value.GetString();
                break;
            case "protocol":
                Protocol = value.GetString() ?? string.Empty;
                break;
            case "protection":
                MergeStringList(_protection, value, mergeMode);
                break;
            case "caseSentitive":
                CaseSentitive = value.ValueKind == JsonValueKind.True;
                break;
            case "errorUrl":
                ErrorUrl = value.GetString();
                break;
            case "mirrors":
                MergeStringList(_mirrors, value, mergeMode);
                break;
            // Unknown / not-yet-supported keys are ignored — data.json may grow new fields.
        }
    }

    private static void MergeStringList(List<string> target, JsonElement source, bool mergeMode)
    {
        if (source.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        if (!mergeMode)
        {
            target.Clear();
        }

        foreach (var item in source.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var s = item.GetString();
                if (s is not null)
                {
                    target.Add(s);
                }
            }
        }
    }

    private static void MergeStringDict(Dictionary<string, string> target, JsonElement source)
    {
        if (source.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var prop in source.EnumerateObject())
        {
            target[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => prop.Value.ToString(),
                JsonValueKind.Undefined => throw new NotImplementedException(),
                JsonValueKind.Object => throw new NotImplementedException(),
                JsonValueKind.Array => throw new NotImplementedException(),
                JsonValueKind.Null => throw new NotImplementedException(),
                _ => prop.Value.GetRawText(),
            };
        }
    }
}
