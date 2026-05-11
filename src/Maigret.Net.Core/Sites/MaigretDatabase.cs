using System.Net.Http;
using System.Text.Json;

namespace Maigret.Net.Core.Sites;

/// <summary>
/// In-memory site database loaded from <c>data.json</c>. Mirrors
/// <c>maigret.sites.MaigretDatabase</c> with synchronous and async loaders.
/// </summary>
public sealed class MaigretDatabase
{
    private readonly List<MaigretSite> _sites = new();
    private readonly List<MaigretEngine> _engines = new();
    private readonly List<string> _tags = new();

    public IReadOnlyList<MaigretSite> Sites => _sites;

    public IReadOnlyList<MaigretEngine> Engines => _engines;

    public IReadOnlyList<string> Tags => _tags;

    public IReadOnlyDictionary<string, MaigretSite> SitesDict =>
        _sites.GroupBy(s => s.Name)
              .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

    public IReadOnlyDictionary<string, MaigretEngine> EnginesDict =>
        _engines.GroupBy(e => e.Name)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

    /// <summary>
    /// Filters and ranks the database by Alexa rank, mirroring
    /// <c>MaigretDatabase.ranked_sites_dict</c>. <paramref name="top"/> caps the
    /// number of returned sites; <c>long.MaxValue</c> means "all".
    /// </summary>
    public IReadOnlyDictionary<string, MaigretSite> RankedSitesDict(
        bool reverse = false,
        long top = long.MaxValue,
        IEnumerable<string>? tags = null,
        IEnumerable<string>? excludedTags = null,
        IEnumerable<string>? names = null,
        bool includeDisabled = true,
        string idType = "username")
    {
        var normalizedNames = ToCaseInsensitiveSet(names);
        var normalizedTags = ToCaseInsensitiveSet(tags);
        var normalizedExcluded = ToCaseInsensitiveSet(excludedTags);

        bool Filter(MaigretSite s) =>
            MatchesTags(s, normalizedTags) &&
            !MatchesExcluded(s, normalizedExcluded) &&
            MatchesNames(s, normalizedNames) &&
            (!s.Disabled || includeDisabled || normalizedTags.Contains("disabled")) &&
            string.Equals(s.Type, idType, StringComparison.Ordinal);

        var filtered = _sites.Where(Filter).ToList();
        var ordered = reverse
            ? filtered.OrderByDescending(s => s.AlexaRank).ToList()
            : filtered.OrderBy(s => s.AlexaRank).ToList();

        var slice = top >= int.MaxValue
            ? ordered
            : ordered.Take((int)Math.Min(top, int.MaxValue)).ToList();

        return slice.GroupBy(s => s.Name)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
    }

    public MaigretDatabase LoadFromJson(JsonElement document)
    {
        if (document.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("data.json root must be an object.");
        }

        LoadTags(document);
        LoadEngines(document);
        LoadSites(document);
        return this;
    }

    public MaigretDatabase LoadFromString(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return LoadFromJson(doc.RootElement);
    }

    public MaigretDatabase LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        using var doc = JsonDocument.Parse(stream);
        return LoadFromJson(doc.RootElement);
    }

    public MaigretDatabase LoadFromStream(Stream stream)
    {
        using var doc = JsonDocument.Parse(stream);
        return LoadFromJson(doc.RootElement);
    }

    public async Task<MaigretDatabase> LoadFromHttpAsync(string url, HttpClient client, CancellationToken cancellationToken = default)
    {
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new FileNotFoundException($"Invalid data file URL '{url}'.");
        }

        using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new FileNotFoundException($"Bad response while accessing data file URL '{url}'.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, default, cancellationToken).ConfigureAwait(false);
        return LoadFromJson(doc.RootElement);
    }

    // ---------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------

    private static HashSet<string> ToCaseInsensitiveSet(IEnumerable<string>? source) =>
        new(source ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

    private static bool MatchesTags(MaigretSite site, HashSet<string> tagFilter)
    {
        if (tagFilter.Count == 0)
        {
            return true;
        }

        if (site.Engine is not null && tagFilter.Contains(site.Engine))
        {
            return true;
        }

        foreach (var t in site.Tags)
        {
            if (tagFilter.Contains(t))
            {
                return true;
            }
        }

        return !string.IsNullOrEmpty(site.Protocol) && tagFilter.Contains(site.Protocol);
    }

    private static bool MatchesExcluded(MaigretSite site, HashSet<string> excludedFilter)
    {
        if (excludedFilter.Count == 0)
        {
            return false;
        }

        foreach (var t in site.Tags)
        {
            if (excludedFilter.Contains(t))
            {
                return true;
            }
        }

        if (site.Engine is not null && excludedFilter.Contains(site.Engine))
        {
            return true;
        }

        return !string.IsNullOrEmpty(site.Protocol) && excludedFilter.Contains(site.Protocol);
    }

    private static bool MatchesNames(MaigretSite site, HashSet<string> nameFilter)
    {
        if (nameFilter.Count == 0)
        {
            return true;
        }

        if (nameFilter.Contains(site.Name))
        {
            return true;
        }

        return site.Source is not null && nameFilter.Contains(site.Source);
    }

    private void LoadTags(JsonElement document)
    {
        if (!document.TryGetProperty("tags", out var tagsEl) || tagsEl.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var t in tagsEl.EnumerateArray())
        {
            if (t.ValueKind == JsonValueKind.String)
            {
                _tags.Add(t.GetString() ?? string.Empty);
            }
        }
    }

    private void LoadEngines(JsonElement document)
    {
        if (!document.TryGetProperty("engines", out var enginesEl) || enginesEl.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var prop in enginesEl.EnumerateObject())
        {
            _engines.Add(new MaigretEngine(prop.Name, prop.Value));
        }
    }

    private void LoadSites(JsonElement document)
    {
        if (!document.TryGetProperty("sites", out var sitesEl) || sitesEl.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var enginesByName = EnginesDict;
        foreach (var prop in sitesEl.EnumerateObject())
        {
            var site = ParseSite(prop.Name, prop.Value);
            if (!string.IsNullOrEmpty(site.Engine) && enginesByName.TryGetValue(site.Engine!, out var engine))
            {
                site.UpdateFromEngine(engine);
            }
            _sites.Add(site);
        }
    }

    private static MaigretSite ParseSite(string name, JsonElement value)
    {
        try
        {
            return new MaigretSite(name, value);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Problem parsing json content for site '{name}'.", ex);
        }
    }
}
