// Per-site extraction rule. Mirrors the structure of socid_extractor's SCHEMES
// entries: a list of (field name, regex) pairs applied to the response body.

using System.Text.RegularExpressions;

namespace Maigret.Net.Core.IdExtraction;

/// <summary>
/// One pattern that captures a single field's value out of a profile page.
/// </summary>
public sealed class ExtractionPattern
{
    public ExtractionPattern(string field, Regex regex)
    {
        Field = field;
        Regex = regex;
    }

    /// <summary>Result key (e.g. <c>username</c>, <c>fullname</c>, <c>email</c>).</summary>
    public string Field { get; }

    /// <summary>Regex with at least one capture group; the first group becomes the value.</summary>
    public Regex Regex { get; }

    /// <summary>Builds an extraction pattern from a regex string.</summary>
    public static ExtractionPattern FromString(string field, string regex, RegexOptions options = RegexOptions.Compiled) =>
        new(field, new Regex(regex, options));
}

/// <summary>
/// Bundle of patterns scoped to a specific Maigret site. Multiple rules may match
/// the same site; <see cref="RuleBasedIdExtractor"/> applies all of them.
/// </summary>
public sealed class ExtractionRule
{
    public ExtractionRule(string siteName, IEnumerable<ExtractionPattern> patterns)
    {
        if (string.IsNullOrEmpty(siteName))
        {
            throw new ArgumentException("siteName must be non-empty", nameof(siteName));
        }

        SiteName = siteName;
        Patterns = patterns.ToArray();
    }

    /// <summary>Maigret site name this rule targets (exact match, case-insensitive).</summary>
    public string SiteName { get; }

    /// <summary>Patterns applied to the response body.</summary>
    public IReadOnlyList<ExtractionPattern> Patterns { get; }
}
