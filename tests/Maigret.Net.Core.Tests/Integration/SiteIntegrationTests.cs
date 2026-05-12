// Real-network smoke tests. Excluded from default CI runs via the trait below;
// invoke explicitly with `dotnet test --filter "Category=Integration"`.

using Shouldly;

namespace Maigret.Net.Core.Tests.Integration;

[Trait("Category", "Integration")]
public class SiteIntegrationTests
{
    private static SearchFilter SiteOnly(string siteName) =>
        new()
        {
            SiteNames = [siteName],
            ScanAllSites = true,  // bypass the top-N cap
            IncludeDisabled = true,
        };

    [Fact]
    public async Task GitHub_KnownUser_Claimed()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        MaigretCheckResult? hit = null;
        await foreach (var r in MaigretFactory.SearchAsync("octocat", filter: SiteOnly("GitHub"), cancellationToken: cts.Token).ConfigureAwait(false))
        {
            hit = r;
        }

        hit.ShouldNotBeNull();
        hit.SiteName.ShouldBe("GitHub");
        hit.Status.ShouldBe(MaigretCheckStatus.Claimed);
        hit.SiteUrlUser.ShouldContain("octocat");
    }

    [Fact]
    public async Task GitHub_BogusUser_Available()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var bogus = "this-username-does-not-exist-" + Guid.NewGuid().ToString("N")[..10];
        MaigretCheckResult? hit = null;
        await foreach (var r in MaigretFactory.SearchAsync(bogus, filter: SiteOnly("GitHub"), cancellationToken: cts.Token).ConfigureAwait(false))
        {
            hit = r;
        }

        hit.ShouldNotBeNull();
        hit.Status.ShouldBe(MaigretCheckStatus.Available);
    }

    [Fact]
    public async Task DeviantArt_KnownUser_Claimed()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        MaigretCheckResult? hit = null;
        await foreach (var r in MaigretFactory.SearchAsync("alexaimephotography", filter: SiteOnly("DeviantArt"), cancellationToken: cts.Token).ConfigureAwait(false))
        {
            hit = r;
        }

        // DeviantArt occasionally returns WAF challenges. Treat WAF/Unknown as a soft skip
        // so the test isn't a permanent flake — we only assert that "claimed" is achievable.
        if (hit?.Status == MaigretCheckStatus.Unknown)
        {
            return;
        }

        hit.ShouldNotBeNull();
        hit.Status.ShouldBe(MaigretCheckStatus.Claimed);
    }

    [Fact]
    public async Task TopFiveSites_KnownUser_AtLeastOneHit()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        var settings = Settings.LoadFromEmbedded();
        settings.TopSitesCount = 5;
        settings.MaxConnections = 5;

        var hits = 0;
        await foreach (var r in MaigretFactory.SearchAsync(
                           "octocat",
                           settings: settings,
                           cancellationToken: cts.Token).ConfigureAwait(false))
        {
            if (r.Status == MaigretCheckStatus.Claimed)
            {
                hits++;
            }
        }

        hits.ShouldBeGreaterThan(0, "octocat should be claimed on at least one popular site");
    }
}
