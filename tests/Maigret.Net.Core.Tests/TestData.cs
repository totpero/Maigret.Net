// Port of maigret/tests/test_data.py — data.json integrity checks.

using Maigret.Net.Core;
using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestData
{
    private const int TopSitesAlexaRankLimit = 50;

    private static readonly IReadOnlyList<string> KnownSocialDomains = new[]
    {
        "facebook.com", "instagram.com", "twitter.com", "tiktok.com",
        "vk.com", "reddit.com", "pinterest.com", "snapchat.com",
        "linkedin.com", "tumblr.com", "threads.net", "bsky.app",
        "myspace.com", "weibo.com", "mastodon.social", "gab.com",
        "minds.com", "clubhouse.com",
    };

    private static readonly Lazy<MaigretDatabase> Db = new(MaigretResources.LoadEmbeddedDatabase);

    [Fact]
    public void TagsValidity_NoUnknownTags()
    {
        var db = Db.Value;
        var registered = new HashSet<string>(db.Tags, StringComparer.Ordinal);
        var unknown = new HashSet<string>(StringComparer.Ordinal);

        foreach (var site in db.Sites)
        {
            foreach (var tag in site.Tags)
            {
                if (TagUtils.IsCountryTag(tag))
                {
                    continue;
                }

                if (!registered.Contains(tag))
                {
                    unknown.Add(tag);
                }
            }
        }

        unknown.ShouldBeEmpty($"data.json contains tags that aren't in the registry: {string.Join(", ", unknown)}");
    }

    [Fact]
    public void TopSites_HaveCategoryTag()
    {
        var db = Db.Value;
        var topSites = db.Sites
            .Where(s => s.AlexaRank > 0 && s.AlexaRank < long.MaxValue)
            .OrderBy(s => s.AlexaRank)
            .Take(TopSitesAlexaRankLimit)
            .ToList();

        var missing = topSites
            .Where(s => !s.Tags.Any(t => !TagUtils.IsCountryTag(t)))
            .Select(s => $"{s.Name} (rank {s.AlexaRank})")
            .ToList();

        missing.ShouldBeEmpty(
            $"{missing.Count} top-{TopSitesAlexaRankLimit} sites have no category tag: " +
            string.Join(", ", missing.Take(20)));
    }

    [Fact]
    public void RegisteredTags_AllUsed()
    {
        var db = Db.Value;
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var site in db.Sites)
        {
            foreach (var tag in site.Tags)
            {
                if (!TagUtils.IsCountryTag(tag))
                {
                    used.Add(tag);
                }
            }
        }

        var unused = new HashSet<string>(db.Tags, StringComparer.Ordinal);
        unused.ExceptWith(used);

        unused.ShouldBeEmpty($"Tags registered but not used: {string.Join(", ", unused)}");
    }

    [Fact]
    public void KnownSocialNetworks_HaveSocialTag()
    {
        var db = Db.Value;
        var missing = new List<string>();

        foreach (var site in db.Sites)
        {
            if (!Uri.TryCreate(site.UrlMain, UriKind.Absolute, out var uri))
            {
                continue;
            }

            var host = uri.Host;
            foreach (var domain in KnownSocialDomains)
            {
                if (host == domain || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase))
                {
                    if (!site.Tags.Contains("social", StringComparer.Ordinal))
                    {
                        missing.Add($"{site.Name} ({domain})");
                    }

                    break;
                }
            }
        }

        missing.ShouldBeEmpty(
            $"{missing.Count} known social networks missing 'social' tag: " +
            string.Join(", ", missing));
    }
}
