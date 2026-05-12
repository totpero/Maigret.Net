// A small starter pack of extraction rules covering popular sites.
// Mirrors a subset of socid_extractor SCHEMES — extend as needed; consumers can
// register additional ExtractionRule instances via DI.

using System.Text.RegularExpressions;

namespace Maigret.Net.Core.IdExtraction;

/// <summary>
/// Built-in extraction rules. The set is intentionally minimal — it
/// demonstrates the pattern and covers the most popular sites; richer coverage
/// matches the upstream <c>socid_extractor</c> Python library.
/// </summary>
public static class BuiltInExtractionRules
{
    private const RegexOptions DefaultOptions =
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline;

    /// <summary>Returns the bundled rules.</summary>
    public static IReadOnlyList<ExtractionRule> All { get; } =
    [
        // GitHub HTML profile page
        new ExtractionRule("GitHub",
        [
            new ExtractionPattern("username", new Regex(@"<meta\s+name=""user-login""\s+content=""([^""]+)""", DefaultOptions)),
            new ExtractionPattern("fullname", new Regex(@"<span\s+class=""p-name[^""]*""\s+itemprop=""name""[^>]*>([^<]+)</span>", DefaultOptions)),
            new ExtractionPattern("bio", new Regex(@"<div\s+class=""p-note[^""]*""[^>]*>\s*<div[^>]*>([^<]+)</div>", DefaultOptions)),
            new ExtractionPattern("location", new Regex(@"<span\s+class=""p-label""[^>]*itemprop=""homeLocation""[^>]*>([^<]+)</span>", DefaultOptions)),
        ]),

        // Reddit "old" profile JSON-ish data
        new ExtractionRule("Reddit",
        [
            new ExtractionPattern("username", new Regex(@"""name""\s*:\s*""(t2_[a-z0-9]+)""", DefaultOptions)),
            new ExtractionPattern("karma", new Regex(@"""total_karma""\s*:\s*(\d+)", DefaultOptions)),
            new ExtractionPattern("created_at", new Regex(@"""created_utc""\s*:\s*(\d+)", DefaultOptions)),
        ]),

        // HackerNews user page
        new ExtractionRule("HackerNews",
        [
            new ExtractionPattern("karma", new Regex(@"<a id=""karma""[^>]*>(\d+)</a>", DefaultOptions)),
            new ExtractionPattern("created_at", new Regex(@"<td valign=""top"">created:</td><td>\s*<a[^>]*>([^<]+)</a>", DefaultOptions)),
        ]),

        // Instagram public profile JSON
        new ExtractionRule("Instagram",
        [
            new ExtractionPattern("username", new Regex(@"""username""\s*:\s*""([^""]+)""", DefaultOptions)),
            new ExtractionPattern("fullname", new Regex(@"""full_name""\s*:\s*""([^""]+)""", DefaultOptions)),
            new ExtractionPattern("bio", new Regex(@"""biography""\s*:\s*""([^""]+)""", DefaultOptions)),
        ]),

        // Twitter / X — guest API response
        new ExtractionRule("Twitter",
        [
            new ExtractionPattern("screen_name", new Regex(@"""screen_name""\s*:\s*""([^""]+)""", DefaultOptions)),
            new ExtractionPattern("fullname", new Regex(@"""name""\s*:\s*""([^""]+)""", DefaultOptions)),
            new ExtractionPattern("bio", new Regex(@"""description""\s*:\s*""([^""]+)""", DefaultOptions)),
        ]),
    ];
}
