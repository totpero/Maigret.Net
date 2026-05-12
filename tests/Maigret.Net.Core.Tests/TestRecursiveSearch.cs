using System.Net;
using System.Text.Json;
using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestRecursiveSearch
{
    [Fact]
    public void ParseUsernames_UsernameShapedKeys_AreCaptured()
    {
        var data = new Dictionary<string, string>
        {
            ["username"] = "alice",
            ["screen_name"] = "bob",
            ["login"] = "carol",
            ["other"] = "ignored",
        };
        var ids = RecursiveSearchEngine.ParseUsernames(data).ToArray();

        ids.ShouldContain(("alice", "username"));
        ids.ShouldContain(("bob", "username"));
        ids.ShouldContain(("carol", "username"));
        ids.ShouldNotContain(("ignored", "username"));
    }

    [Fact]
    public void ParseUsernames_SupportedIdKeys_KeepTheirType()
    {
        var data = new Dictionary<string, string>
        {
            ["gaia_id"] = "g123",
            ["vk_id"] = "v456",
        };
        var ids = RecursiveSearchEngine.ParseUsernames(data).ToArray();
        ids.ShouldContain(("g123", "gaia_id"));
        ids.ShouldContain(("v456", "vk_id"));
    }

    [Fact]
    public void ParseUsernames_PythonListLiteral_ExpandsItems()
    {
        var data = new Dictionary<string, string>
        {
            ["usernames"] = "['x','y','z']",
        };
        var ids = RecursiveSearchEngine.ParseUsernames(data).Select(x => x.Id).OrderBy(s => s).ToArray();
        ids.ShouldBe(["x", "y", "z"]);
    }

    [Fact]
    public void ParseUsernames_IgnoresUsernamesPlural_AsUsernameKey()
    {
        // Python special-cases "usernames" (plural) so it does NOT trigger the singular-shape rule.
        var data = new Dictionary<string, string>
        {
            ["usernames"] = "['only']",
        };
        var ids = RecursiveSearchEngine.ParseUsernames(data).ToArray();
        ids.Length.ShouldBe(1);
        ids[0].ShouldBe(("only", "username"));
    }

    private static MaigretDatabase BuildDatabase(params (string name, string url, string presence)[] sites)
    {
        var sitesObj = new Dictionary<string, object?>();
        foreach (var (name, url, presence) in sites)
        {
            sitesObj[name] = new
            {
                url,
                urlMain = "https://example.invalid/",
                checkType = "message",
                type = "username",
                alexaRank = 100,
                presenseStrs = new[] { presence },
                tags = new[] { "social" },
            };
        }
        var json = JsonSerializer.Serialize(new { sites = sitesObj, engines = new { }, tags = Array.Empty<string>() });
        return new MaigretDatabase().LoadFromString(json);
    }

    private sealed class StaticHandler(Func<HttpRequestMessage, HttpResponseMessage> r) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _r = r;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_r(request));
    }

    private sealed class StaticExtractor(string siteName, IReadOnlyDictionary<string, string> data) : IIdExtractor
    {
        private readonly IReadOnlyDictionary<string, string> _data = data;
        private readonly string _siteName = siteName;

        public IReadOnlyDictionary<string, string> Extract(string htmlText, MaigretSite site) =>
            string.Equals(site.Name, _siteName, StringComparison.Ordinal)
                ? _data
                : new Dictionary<string, string>(0);
    }

    [Fact]
    public async Task RecursiveSearch_DiscoversNewUsernameAndReruns()
    {
        var db = BuildDatabase(
            ("FooSite", "https://foo.com/{username}", "WELCOME"),
            ("BarSite", "https://bar.com/{username}", "WELCOME"));

        var settings = Settings.LoadFromEmbedded();
        settings.MaxConnections = 4;

        var hits = new HashSet<string>(StringComparer.Ordinal);
        var checker = new SimpleHttpChecker(new HttpClient(new StaticHandler(req =>
        {
            hits.Add(req.RequestUri!.AbsoluteUri);
            // Both sites return WELCOME → Claimed; FooSite extractor injects a new username.
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("WELCOME body"),
            };
        })));
        var checkers = new Dictionary<string, ICheckerBase> { [string.Empty] = checker };

        // FooSite extracts a new username "bob"; BarSite extracts nothing.
        var extractor = new StaticExtractor("FooSite",
            new Dictionary<string, string> { ["username"] = "bob" });

        var engine = new RecursiveSearchEngine();
        var request = new MaigretSearchRequest
        {
            Usernames = ["alice"],
            Database = db,
            Settings = settings,
            Extractor = extractor,
            Checkers = checkers,
        };
        var results = new List<MaigretCheckResult>();
        await foreach (var r in engine.SearchAsync(request, new RecursiveSearchOptions { MaxDepth = 2 }).ConfigureAwait(false))
        {
            results.Add(r);
        }

        // alice on Foo, alice on Bar, bob on Foo, bob on Bar → 4 results.
        results.Count.ShouldBe(4);
        var byUser = results.GroupBy(r => r.Username).ToDictionary(g => g.Key, g => g.Count());
        byUser["alice"].ShouldBe(2);
        byUser["bob"].ShouldBe(2);
    }

    [Fact]
    public async Task RecursiveSearch_RespectsMaxDepth()
    {
        var db = BuildDatabase(("FooSite", "https://foo.com/{username}", "OK"));
        var settings = Settings.LoadFromEmbedded();

        var checker = new SimpleHttpChecker(new HttpClient(new StaticHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("OK") })));
        var checkers = new Dictionary<string, ICheckerBase> { [string.Empty] = checker };

        // Each call to extractor returns a NEW username — without a depth cap this would loop forever.
        var counter = 0;
        var extractor = new GeneratingExtractor(() => $"u{Interlocked.Increment(ref counter)}");

        var engine = new RecursiveSearchEngine();
        var request = new MaigretSearchRequest
        {
            Usernames = ["seed"],
            Database = db,
            Settings = settings,
            Extractor = extractor,
            Checkers = checkers,
        };
        var results = new List<MaigretCheckResult>();
        await foreach (var r in engine.SearchAsync(request, new RecursiveSearchOptions { MaxDepth = 2 }).ConfigureAwait(false))
        {
            results.Add(r);
            results.Count.ShouldBeLessThan(50, "depth cap should bound the run");
        }

        // depth 0: seed; depth 1: u1 → total 2 distinct users (no further descent past MaxDepth=2)
        results.Select(r => r.Username).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public async Task RecursiveSearch_NoExtractedIds_BehavesLikeFlatSearch()
    {
        var db = BuildDatabase(("FooSite", "https://foo.com/{username}", "OK"));
        var settings = Settings.LoadFromEmbedded();
        var checker = new SimpleHttpChecker(new HttpClient(new StaticHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("OK") })));
        var checkers = new Dictionary<string, ICheckerBase> { [string.Empty] = checker };

        var engine = new RecursiveSearchEngine();
        var request = new MaigretSearchRequest
        {
            Usernames = ["alice"],
            Database = db,
            Settings = settings,
            Checkers = checkers,
        };
        var results = new List<MaigretCheckResult>();
        await foreach (var r in engine.SearchAsync(request, new RecursiveSearchOptions { MaxDepth = 5 }).ConfigureAwait(false))
        {
            results.Add(r);
        }

        results.Count.ShouldBe(1);
        results[0].Status.ShouldBe(MaigretCheckStatus.Claimed);
    }

    private sealed class GeneratingExtractor(Func<string> next) : IIdExtractor
    {
        private readonly Func<string> _next = next;

        public IReadOnlyDictionary<string, string> Extract(string htmlText, MaigretSite site) =>
            new Dictionary<string, string> { ["username"] = _next() };
    }
}
