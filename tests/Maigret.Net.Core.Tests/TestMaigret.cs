using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestMaigret
{
    /// <summary>
    /// Builds a tiny in-memory database with a couple of fake sites that all
    /// resolve to the local <see cref="StubHandler"/>. Avoids touching the network.
    /// </summary>
    private static MaigretDatabase BuildDatabase(params (string name, string url, string checkType, IEnumerable<string>? presence)[] sites)
    {
        var sitesObj = new Dictionary<string, object?>();
        foreach (var (name, url, checkType, presence) in sites)
        {
            sitesObj[name] = new
            {
                url,
                urlMain = "https://example.invalid/",
                checkType,
                type = "username",
                alexaRank = 100,
                presenseStrs = presence,
                tags = new[] { "social" },
            };
        }

        var json = JsonSerializer.Serialize(new { sites = sitesObj, engines = new { }, tags = Array.Empty<string>() },
            new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        var db = new MaigretDatabase();
        return db.LoadFromString(json);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    [Fact]
    public async Task SearchAsync_TwoSites_StreamsBothResults()
    {
        var db = BuildDatabase(
            ("FooSite", "https://foo.com/{username}", "status_code", null),
            ("BarSite", "https://bar.com/{username}", "status_code", null));

        var settings = Settings.LoadFromEmbedded();
        settings.MaxConnections = 2;

        var checker = new SimpleHttpChecker(new HttpClient(new StubHandler(req =>
            new HttpResponseMessage(req.RequestUri!.Host == "foo.com" ? HttpStatusCode.OK : HttpStatusCode.NotFound))));

        var checkers = new Dictionary<string, ICheckerBase> { [string.Empty] = checker };

        var request = new MaigretSearchRequest
        {
            Usernames = ["alex"],
            Database = db,
            Settings = settings,
            Checkers = checkers,
        };

        var results = new List<MaigretCheckResult>();
        await foreach (var r in MaigretSearchEngine.SearchAsync(request).ConfigureAwait(false))
        {
            results.Add(r);
        }

        results.Count.ShouldBe(2);
        results.ShouldContain(r => r.SiteName == "FooSite" && r.Status == MaigretCheckStatus.Claimed);
        results.ShouldContain(r => r.SiteName == "BarSite" && r.Status == MaigretCheckStatus.Available);
    }

    [Fact]
    public async Task SearchAsync_FilterByName_OnlyReturnsMatching()
    {
        var db = BuildDatabase(
            ("FooSite", "https://foo.com/{username}", "status_code", null),
            ("BarSite", "https://bar.com/{username}", "status_code", null),
            ("QuxSite", "https://qux.com/{username}", "status_code", null));

        var settings = Settings.LoadFromEmbedded();

        var filter = new SearchFilter
        {
            SiteNames = ["FooSite", "QuxSite"],
            ScanAllSites = true,
        };

        var checker = new SimpleHttpChecker(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));
        var checkers = new Dictionary<string, ICheckerBase> { [string.Empty] = checker };

        var request = new MaigretSearchRequest
        {
            Usernames = ["alex"],
            Database = db,
            Settings = settings,
            Filter = filter,
            Checkers = checkers,
        };

        var results = new List<MaigretCheckResult>();
        await foreach (var r in MaigretSearchEngine.SearchAsync(request).ConfigureAwait(false))
        {
            results.Add(r);
        }

        results.Count.ShouldBe(2);
        results.Select(r => r.SiteName).OrderBy(n => n).ShouldBe(["FooSite", "QuxSite"]);
    }

    [Fact]
    public async Task SearchAsync_TopSitesCap_LimitsResults()
    {
        var db = BuildDatabase(
            ("Foo", "https://foo.com/{username}", "status_code", null),
            ("Bar", "https://bar.com/{username}", "status_code", null),
            ("Baz", "https://baz.com/{username}", "status_code", null));

        var settings = Settings.LoadFromEmbedded();
        settings.TopSitesCount = 2;

        var checker = new SimpleHttpChecker(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));
        var checkers = new Dictionary<string, ICheckerBase> { [string.Empty] = checker };

        var request = new MaigretSearchRequest
        {
            Usernames = ["alex"],
            Database = db,
            Settings = settings,
            Checkers = checkers,
        };

        var results = new List<MaigretCheckResult>();
        await foreach (var r in MaigretSearchEngine.SearchAsync(request).ConfigureAwait(false))
        {
            results.Add(r);
        }

        results.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SearchAsync_EmptyUsernameList_YieldsNothing()
    {
        var db = BuildDatabase(("Foo", "https://foo.com/{username}", "status_code", null));
        var settings = Settings.LoadFromEmbedded();

        var request = new MaigretSearchRequest
        {
            Usernames = [],
            Database = db,
            Settings = settings,
        };

        var results = new List<MaigretCheckResult>();
        await foreach (var r in MaigretSearchEngine.SearchAsync(request).ConfigureAwait(false))
        {
            results.Add(r);
        }

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_MockChecker_DoesNotTouchNetwork()
    {
        var db = BuildDatabase(("Foo", "https://foo.com/{username}", "status_code", null));
        var settings = Settings.LoadFromEmbedded();

        var checkers = new Dictionary<string, ICheckerBase> { [string.Empty] = new MockChecker() };

        var request = new MaigretSearchRequest
        {
            Usernames = ["alex"],
            Database = db,
            Settings = settings,
            Checkers = checkers,
        };

        var results = new List<MaigretCheckResult>();
        await foreach (var r in MaigretSearchEngine.SearchAsync(request).ConfigureAwait(false))
        {
            results.Add(r);
        }

        results.Count.ShouldBe(1);
        // MockChecker returns status 0 → check_type=status_code resolves Available.
        results[0].Status.ShouldBe(MaigretCheckStatus.Available);
    }

    [Fact]
    public async Task SearchAsync_PreservesIdsForwardedFromExtractor()
    {
        var db = BuildDatabase(("Foo", "https://foo.com/{username}", "status_code", null));
        var settings = Settings.LoadFromEmbedded();
        settings.InfoExtracting = true;

        var stubExtractor = new StaticExtractor(new Dictionary<string, string> { ["fullname"] = "Alex Aim" });
        var checker = new SimpleHttpChecker(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));
        var checkers = new Dictionary<string, ICheckerBase> { [string.Empty] = checker };

        var request = new MaigretSearchRequest
        {
            Usernames = ["alex"],
            Database = db,
            Settings = settings,
            Extractor = stubExtractor,
            Checkers = checkers,
        };

        MaigretCheckResult? hit = null;
        await foreach (var r in MaigretSearchEngine.SearchAsync(request).ConfigureAwait(false))
        {
            if (r.Status == MaigretCheckStatus.Claimed)
            {
                hit = r;
            }
        }

        hit.ShouldNotBeNull();
        hit.IdsData.ShouldNotBeNull();
        hit.IdsData!["fullname"].ShouldBe("Alex Aim");
    }

    [Fact]
    public void AddMaigret_Registers_Settings_And_Database()
    {
        var services = new ServiceCollection();
        services.AddMaigret(s => s.Timeout = 5);
        var sp = services.BuildServiceProvider();

        sp.GetRequiredService<Settings>().Timeout.ShouldBe(5);
        sp.GetRequiredService<MaigretDatabase>().Sites.Count.ShouldBeGreaterThan(0);
        sp.GetRequiredService<IActivationProvider>().ShouldBeOfType<NullActivationProvider>();
        sp.GetRequiredService<IIdExtractor>().ShouldBeOfType<NullIdExtractor>();
    }

    [Fact]
    public async Task MaigretFactory_SearchAsync_HitsEmbeddedDatabaseWithSiteFilter()
    {
        // Use a site filter with a name that doesn't exist so no real network calls happen.
        var filter = new SearchFilter
        {
            SiteNames = ["this-site-name-definitely-does-not-exist"],
            ScanAllSites = true,
        };
        var settings = Settings.LoadFromEmbedded();

        var results = new List<MaigretCheckResult>();
        await foreach (var r in MaigretFactory.SearchAsync("test", settings, filter).ConfigureAwait(false))
        {
            results.Add(r);
        }

        results.ShouldBeEmpty();
    }

    private sealed class StaticExtractor(IReadOnlyDictionary<string, string> data) : IIdExtractor
    {
        private readonly IReadOnlyDictionary<string, string> _data = data;

        public IReadOnlyDictionary<string, string> Extract(string htmlText, MaigretSite site) => _data;
    }
}
