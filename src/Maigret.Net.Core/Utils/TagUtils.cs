using System.Text.RegularExpressions;

namespace Maigret.Net.Core.Utils;

/// <summary>
/// Helpers around country tags. Maigret tags include both topical labels
/// ("social", "photo") and ISO-like 2-letter country codes ("us", "ru", "fr"),
/// plus the special <c>global</c> tag.
/// Mirrors <c>maigret.utils.is_country_tag</c>.
/// </summary>
public static class TagUtils
{
    private static readonly Regex CountryTagRegex = new("^[a-zA-Z]{2}$", RegexOptions.Compiled);

    /// <summary>True when <paramref name="tag"/> is a 2-letter country code or <c>global</c>.</summary>
    public static bool IsCountryTag(string? tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return false;
        }

        return string.Equals(tag, "global", StringComparison.Ordinal) ? true : CountryTagRegex.IsMatch(tag);
    }

    /// <summary>
    /// Wraps a URL-like string in a clickable HTML anchor for HTML reports.
    /// Mirrors <c>maigret.utils.enrich_link_str</c>.
    /// </summary>
    public static string EnrichLinkStr(string link)
    {
        if (string.IsNullOrEmpty(link))
        {
            return link;
        }

        var trimmed = link.Trim();
        var isLink = trimmed.StartsWith("www.", StringComparison.Ordinal) ||
            (trimmed.StartsWith("http", StringComparison.Ordinal) && trimmed.Contains("//", StringComparison.Ordinal));

        return isLink ? $"<a class=\"auto-link\" href=\"{trimmed}\">{trimmed}</a>" : link;
    }
}
