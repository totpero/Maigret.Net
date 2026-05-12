// Default IIdExtractor implementation. Drop-in replacement for the no-op
// NullIdExtractor; consumers register it via AddMaigretIdExtraction() and
// supply rules through ExtractionRule instances or via DI.

using System.Net;

namespace Maigret.Net.Core.IdExtraction;

/// <summary>
/// Concrete <see cref="IIdExtractor"/> backed by a collection of <see cref="ExtractionRule"/>s.
/// Lookups are case-insensitive on <see cref="ExtractionRule.SiteName"/>.
/// </summary>
public sealed class RuleBasedIdExtractor(IEnumerable<ExtractionRule> rules) : IIdExtractor
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ExtractionRule>> _rulesBySite = rules
            .GroupBy(r => r.SiteName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ExtractionRule>)[.. g],
                StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>(0);

    public IReadOnlyDictionary<string, string> Extract(string htmlText, MaigretSite site)
    {
        if (string.IsNullOrEmpty(htmlText) || site is null)
        {
            return Empty;
        }

        if (!_rulesBySite.TryGetValue(site.Name, out var rules))
        {
            return Empty;
        }

        Dictionary<string, string>? result = null;
        foreach (var rule in rules)
        {
            foreach (var pattern in rule.Patterns)
            {
                var match = pattern.Regex.Match(htmlText);
                if (!match.Success || match.Groups.Count < 2)
                {
                    continue;
                }

                var captured = match.Groups[1].Value;
                if (string.IsNullOrEmpty(captured))
                {
                    continue;
                }

                result ??= new Dictionary<string, string>(StringComparer.Ordinal);
                // First match wins per field — mirrors Python's ordering.
                if (!result.ContainsKey(pattern.Field))
                {
                    result[pattern.Field] = WebUtility.HtmlDecode(captured);
                }
            }
        }
        return (IReadOnlyDictionary<string, string>?)result ?? Empty;
    }
}
