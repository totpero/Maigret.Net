using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestMaigretClient
{
    [Fact]
    public void MaigretClient_DefaultsAreUsable()
    {
        // The client must be instantiable without dependencies — exactly what
        // a "library consumer who just wants results" would expect.
        using var _ = new ServiceCollectionScope(); // no-op marker — declares intent
        var client = new MaigretClient();
        client.ShouldNotBeNull();
    }

    [Fact]
    public async Task SearchAsync_NoMatchingTag_ReturnsEmptySummaryWithoutNetwork()
    {
        var client = new MaigretClient();
        var summary = await client.SearchAsync(
            "octocat",
            new MaigretSearchOptions
            {
                Tags = new[] { "tag-that-does-not-exist-anywhere" },
            }).ConfigureAwait(false);

        summary.Username.ShouldBe("octocat");
        summary.Results.ShouldBeEmpty();
        summary.FoundCount.ShouldBe(0);
        summary.AnyFound.ShouldBeFalse();
        summary.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public async Task SearchAsync_RejectsEmptyUsername()
    {
        var client = new MaigretClient();
        await Should.ThrowAsync<ArgumentException>(
            async () => await client.SearchAsync(string.Empty).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [Fact]
    public async Task StreamAsync_NoMatchingTag_YieldsNothing()
    {
        var client = new MaigretClient();
        var count = 0;
        await foreach (var _ in client.StreamAsync(
                           "octocat",
                           new MaigretSearchOptions
                           {
                               Tags = new[] { "no-such-tag" },
                           }).ConfigureAwait(false))
        {
            count++;
        }
        count.ShouldBe(0);
    }

    [Fact]
    public void MaigretSearchSummary_GroupsClaimedByUsername()
    {
        var aliceClaimed = new MaigretCheckResult("alice", "FooSite", "https://foo/alice", MaigretCheckStatus.Claimed);
        var aliceMissing = new MaigretCheckResult("alice", "BarSite", "https://bar/alice", MaigretCheckStatus.Available);
        var bobClaimed = new MaigretCheckResult("bob", "FooSite", "https://foo/bob", MaigretCheckStatus.Claimed);

        var summary = new MaigretSearchSummary("alice", new[] { aliceClaimed, aliceMissing, bobClaimed }, TimeSpan.FromSeconds(1));

        summary.FoundCount.ShouldBe(2);
        summary.AnyFound.ShouldBeTrue();
        summary.ClaimedSites.Count().ShouldBe(2);
        summary.AvailableSites.Count().ShouldBe(1);
        summary.ClaimedByUsername.Keys.ShouldBe(new[] { "alice", "bob" }, ignoreOrder: true);
        summary.ClaimedByUsername["alice"].Count.ShouldBe(1);
    }

    /// <summary>Simple disposable scope — used purely to mark intent in the smoke test above.</summary>
    private sealed class ServiceCollectionScope : IDisposable
    {
        public void Dispose() { /* nothing */ }
    }
}
