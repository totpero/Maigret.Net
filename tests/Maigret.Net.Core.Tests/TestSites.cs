using System.Text.Json;
using System.Text.RegularExpressions;
using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestSites
{
    private static readonly Lazy<MaigretDatabase> Database = new(MaigretResources.LoadEmbeddedDatabase);

    [Fact]
    public void EmbeddedDatabase_Loads_WithThousandsOfSites()
    {
        var db = Database.Value;
        db.Sites.Count.ShouldBeGreaterThan(1000);
    }

    [Fact]
    public void EmbeddedDatabase_HasEnginesAndTags()
    {
        var db = Database.Value;
        db.Engines.Count.ShouldBeGreaterThan(0);
        db.Tags.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void EveryEngineSiteWiresUpEngineObj()
    {
        var db = Database.Value;
        var enginesByName = db.EnginesDict;
        var orphanReferences = new List<string>();

        foreach (var site in db.Sites)
        {
            if (string.IsNullOrEmpty(site.Engine))
            {
                continue;
            }

            if (!enginesByName.ContainsKey(site.Engine))
            {
                orphanReferences.Add($"{site.Name} -> {site.Engine}");
                continue;
            }

            site.EngineObj.ShouldNotBeNull($"site '{site.Name}' references engine '{site.Engine}' but EngineObj is null");
        }

        orphanReferences.ShouldBeEmpty();
    }

    [Fact]
    public void EverySiteHasIdentifyingUrl()
    {
        var db = Database.Value;
        // Some sites declare only `url` (full pattern) without a separate `urlMain`.
        // What we require for the engine to function is that at least one of them is present.
        var missing = db.Sites
            .Where(s => string.IsNullOrEmpty(s.UrlMain) && string.IsNullOrEmpty(s.Url))
            .Select(s => s.Name)
            .ToList();
        missing.ShouldBeEmpty();
    }

    [Fact]
    public void SiteNamesAreUnique()
    {
        var db = Database.Value;
        var duplicates = db.Sites
            .GroupBy(s => s.Name, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        duplicates.ShouldBeEmpty();
    }

    [Fact]
    public void DetectUsername_RoundTripsForFacebookFixture()
    {
        var db = Database.Value;
        var facebook = db.SitesDict.GetValueOrDefault("Facebook");
        facebook.ShouldNotBeNull();
        facebook.Url.ShouldContain("{username}");

        var profileUrl = facebook.Url.Replace("{username}", "zuck");
        var detected = facebook.DetectUsername(profileUrl);
        detected.ShouldBe("zuck");
    }

    [Fact]
    public void GetUrlTemplate_ReturnsNonEmptyForKnownSites()
    {
        var db = Database.Value;
        var sample = db.Sites.Take(50);
        foreach (var site in sample)
        {
            var tpl = site.GetUrlTemplate();
            tpl.ShouldNotBeNullOrEmpty();
        }
    }

    [Fact]
    public void Twitter_Entry_HasExpectedConfig()
    {
        // Port of test_twitter.py::test_twitter_site_entry_config — verify the
        // Twitter / X site shipped with data.json wires its activation and probe correctly.
        var db = Database.Value;
        var site = db.SitesDict.GetValueOrDefault("Twitter");
        site.ShouldNotBeNull();

        site.Disabled.ShouldBeFalse();
        site.CheckType.ShouldBe("message");
        site.UrlProbe.ShouldNotBeNullOrWhiteSpace();
        site.UrlProbe.ShouldContain("{username}");
        (site.UrlProbe.Contains("UserByScreenName") || site.UrlProbe.Contains("graphql"))
            .ShouldBeTrue();
        site.RegexCheck.ShouldNotBeNullOrWhiteSpace();
        Regex.IsMatch(site.UsernameClaimed, site.RegexCheck!).ShouldBeTrue();
        Regex.IsMatch(site.UsernameUnclaimed, site.RegexCheck!).ShouldBeTrue();
        site.AbsenceStrs.ShouldNotBeEmpty();
        site.Activation.ValueKind.ShouldBe(JsonValueKind.Object);
        site.Activation.GetProperty("method").GetString().ShouldBe("twitter");
        site.Activation.TryGetProperty("url", out var urlEl).ShouldBeTrue();
        urlEl.GetString().ShouldNotBeNullOrWhiteSpace();
        site.Headers.Keys.Any(k => k.Equals("authorization", StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue();
    }

    [Fact]
    public void Sites_WithEnginePresenseStrings_InheritFromEngine()
    {
        // phpBB/Search engine declares "postprofile" and "username-coloured" presence strings.
        // Any site that picks up that engine should have those strings merged in (mergeMode=true).
        var db = Database.Value;
        var phpBbSites = db.Sites.Where(s => string.Equals(s.Engine, "phpBB/Search", StringComparison.Ordinal)).ToList();
        if (phpBbSites.Count == 0)
        {
            return; // engine present in JSON but unused — nothing to assert.
        }

        foreach (var site in phpBbSites)
        {
            site.PresenseStrs.ShouldContain("postprofile");
        }
    }
}
