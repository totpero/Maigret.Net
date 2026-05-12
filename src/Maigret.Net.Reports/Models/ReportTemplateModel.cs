// Default template-friendly view of a ReportContext. Engines can either accept
// `IReportTemplateModel` directly or copy the dictionary representation into
// their own context.

namespace Maigret.Net.Reports.Models;

/// <summary>
/// Concrete <see cref="IReportTemplateModel"/> derived from a <see cref="ReportContext"/>.
/// </summary>
public sealed class ReportTemplateModel : IReportTemplateModel
{
    public ReportTemplateModel(ReportContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Username = context.Username;
        IdType = context.IdType;
        GeneratedAt = context.GeneratedAtIso8601;
        TotalChecked = context.TotalChecked;
        TotalFound = context.TotalFound;
        Extras = context.Extras;

        var found = context.Claimed.ToList();
        var tagCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var countryCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var r in found)
        {
            foreach (var tag in r.Tags)
            {
                if (TagUtils.IsCountryTag(tag))
                {
                    var key = tag.ToLowerInvariant();
                    if (key == "global")
                    {
                        continue;
                    }

                    countryCounts[key] = countryCounts.GetValueOrDefault(key) + 1;
                }
                else
                {
                    tagCounts[tag] = tagCounts.GetValueOrDefault(tag) + 1;
                }
            }
        }

        Accounts = [.. found
            .OrderBy(r => r.SiteName, StringComparer.Ordinal)
            .Select(r => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["site_name"] = r.SiteName,
                ["profile_url"] = r.SiteUrlUser,
                ["url_main"] = r.SiteUrlUser,
                ["tags"] = r.Tags.ToList(),
                ["ids"] = r.IdsData?
                    .Where(kv => !string.Equals(kv.Key, "image", StringComparison.OrdinalIgnoreCase))
                    .Select(kv => (IDictionary<string, object?>)new Dictionary<string, object?>
                    {
                        ["key"] = kv.Key,
                        ["value"] = kv.Value,
                    })
                    .ToList() ?? [],
            })];

        TagSummary = [.. tagCounts
            .OrderByDescending(p => p.Value)
            .Select(p => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["key"] = p.Key,
                ["value"] = p.Value,
            })];

        CountrySummary = [.. countryCounts
            .OrderByDescending(p => p.Value)
            .Select(p => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["key"] = p.Key,
                ["value"] = p.Value,
            })];
    }

    public string Username { get; }
    public string IdType { get; }
    public string GeneratedAt { get; }
    public int TotalChecked { get; }
    public int TotalFound { get; }
    public IReadOnlyList<IDictionary<string, object?>> Accounts { get; }
    public IReadOnlyList<IDictionary<string, object?>> TagSummary { get; }
    public IReadOnlyList<IDictionary<string, object?>> CountrySummary { get; }
    public IReadOnlyDictionary<string, string> Extras { get; }

    /// <summary>
    /// Flattened key/value snapshot suitable for engines that prefer a single
    /// dictionary (e.g. Scriban via <c>ScriptObject.Import</c>).
    /// </summary>
    public IDictionary<string, object?> ToDictionary() => new Dictionary<string, object?>
    {
        ["username"] = Username,
        ["id_type"] = IdType,
        ["generated_at"] = GeneratedAt,
        ["total_checked"] = TotalChecked,
        ["total_found"] = TotalFound,
        ["accounts"] = Accounts,
        ["tag_summary"] = TagSummary,
        ["country_summary"] = CountrySummary,
        ["extras"] = Extras,
    };
}
