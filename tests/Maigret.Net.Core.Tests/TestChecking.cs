using System.Net;
using System.Net.Http;
using System.Text.Json;
using Maigret.Net.Core;
using Maigret.Net.Core.Checkers;
using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestChecking
{
    private static MaigretSite BuildSite(
        string name,
        string url,
        string urlMain,
        string checkType,
        IEnumerable<string>? presence = null,
        IEnumerable<string>? absence = null,
        string? regex = null,
        string type = "username",
        bool ignore403 = false,
        bool disabled = false)
    {
        var info = new
        {
            url,
            urlMain,
            checkType,
            type,
            ignore403,
            disabled,
            regexCheck = regex,
            presenseStrs = presence,
            absenceStrs = absence,
        };
        var json = JsonSerializer.Serialize(info, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
        using var doc = JsonDocument.Parse(json);
        return new MaigretSite(name, doc.RootElement);
    }

    [Fact]
    public void DetectErrorPage_Cloudflare_ReturnsBotProtection()
    {
        var html = "<html><title>Just a moment</title></html>";
        var err = Checking.DetectErrorPage(html, 200, null, ignore403: false);
        err.ShouldNotBeNull();
        err.Type.ShouldBe("Bot protection");
    }

    [Fact]
    public void DetectErrorPage_403_NotIgnored_ReturnsAccessDenied()
    {
        Checking.DetectErrorPage("ok", 403, null, ignore403: false)
            !.Type.ShouldBe("Access denied");
    }

    [Fact]
    public void DetectErrorPage_403_Ignored_ReturnsNull()
    {
        Checking.DetectErrorPage("ok", 403, null, ignore403: true).ShouldBeNull();
    }

    [Fact]
    public void DetectErrorPage_999_ReturnsNull()
    {
        Checking.DetectErrorPage("blocked", 999, null, ignore403: false).ShouldBeNull();
    }

    [Fact]
    public void DetectErrorPage_500_ReturnsServerError()
    {
        Checking.DetectErrorPage("oops", 503, null, ignore403: false)
            !.Type.ShouldBe("Server");
    }

    [Fact]
    public void DetectErrorPage_SiteSpecificFlag_ReturnsSiteSpecific()
    {
        var html = "Account suspended";
        var err = Checking.DetectErrorPage(html, 200, new Dictionary<string, string>
        {
            ["Account suspended"] = "Suspended",
        }, ignore403: false);
        err.ShouldNotBeNull();
        err.Type.ShouldBe("Site-specific");
    }

    [Fact]
    public void InterpretResponse_Message_Presence_NoAbsence_Claimed()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}", "https://foo.com/", "message",
            presence: new[] { "profile" });
        var resp = new CheckResponse("welcome to my profile", 200, null);
        var r = Checking.InterpretResponse(site, "alex", "https://foo.com/alex", resp);
        r.Status.ShouldBe(MaigretCheckStatus.Claimed);
    }

    [Fact]
    public void InterpretResponse_Message_AbsenceWins()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}", "https://foo.com/", "message",
            presence: new[] { "profile" }, absence: new[] { "user not found" });
        var resp = new CheckResponse("user not found, profile missing", 200, null);
        var r = Checking.InterpretResponse(site, "alex", "https://foo.com/alex", resp);
        r.Status.ShouldBe(MaigretCheckStatus.Available);
    }

    [Fact]
    public void InterpretResponse_StatusCode_2xx_Claimed()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}", "https://foo.com/", "status_code");
        var resp = new CheckResponse("", 200, null);
        Checking.InterpretResponse(site, "alex", "https://foo.com/alex", resp).Status
            .ShouldBe(MaigretCheckStatus.Claimed);
    }

    [Fact]
    public void InterpretResponse_StatusCode_404_Available()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}", "https://foo.com/", "status_code");
        var resp = new CheckResponse("", 404, null);
        Checking.InterpretResponse(site, "alex", "https://foo.com/alex", resp).Status
            .ShouldBe(MaigretCheckStatus.Available);
    }

    [Fact]
    public void InterpretResponse_ResponseUrl_2xxAndPresence_Claimed()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}", "https://foo.com/", "response_url",
            presence: new[] { "ok" });
        var resp = new CheckResponse("ok page", 200, null);
        Checking.InterpretResponse(site, "alex", "https://foo.com/alex", resp).Status
            .ShouldBe(MaigretCheckStatus.Claimed);
    }

    [Fact]
    public void InterpretResponse_ResponseUrl_NoPresence_Available()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}", "https://foo.com/", "response_url",
            presence: new[] { "ok" });
        var resp = new CheckResponse("redirected", 200, null);
        Checking.InterpretResponse(site, "alex", "https://foo.com/alex", resp).Status
            .ShouldBe(MaigretCheckStatus.Available);
    }

    [Fact]
    public void InterpretResponse_TransportError_Unknown()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}", "https://foo.com/", "status_code");
        var resp = new CheckResponse("", 0, new CheckError("Request timeout", "10s"));
        var r = Checking.InterpretResponse(site, "alex", "https://foo.com/alex", resp);
        r.Status.ShouldBe(MaigretCheckStatus.Unknown);
        r.Error?.Type.ShouldBe("Request timeout");
    }

    [Fact]
    public void BuildProfileUrl_FillsTemplate()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}/profile", "https://foo.com/", "status_code");
        Checking.BuildProfileUrl(site, "alex").ShouldBe("https://foo.com/alex/profile");
    }

    [Fact]
    public void BuildProfileUrl_CollapsesDuplicateSlashes()
    {
        var site = BuildSite("Foo", "{urlMain}//{username}//foo", "https://foo.com", "status_code");
        Checking.BuildProfileUrl(site, "alex").ShouldBe("https://foo.com/alex/foo");
    }

    [Fact]
    public async Task CheckSiteForUsernameAsync_DisabledSite_ReturnsIllegal()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}", "https://foo.com/", "status_code", disabled: true);
        var options = new QueryOptions { Forced = false };

        var r = await Checking.CheckSiteForUsernameAsync(site, "alex", options).ConfigureAwait(false);
        r.Status.ShouldBe(MaigretCheckStatus.Illegal);
        r.Error?.Type.ShouldBe("Check is disabled");
    }

    [Fact]
    public async Task CheckSiteForUsernameAsync_WrongIdType_ReturnsIllegal()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}", "https://foo.com/", "status_code", type: "vk_id");
        var options = new QueryOptions { IdType = "username" };

        var r = await Checking.CheckSiteForUsernameAsync(site, "alex", options).ConfigureAwait(false);
        r.Status.ShouldBe(MaigretCheckStatus.Illegal);
        r.Error?.Type.ShouldBe("Unsupported identifier type");
    }

    [Fact]
    public async Task CheckSiteForUsernameAsync_RegexCheck_Failed_ReturnsIllegal()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}", "https://foo.com/", "status_code",
            regex: "^[a-z]{3,8}$");
        var options = new QueryOptions();

        var r = await Checking.CheckSiteForUsernameAsync(site, "AlexAndTheVeryLongName", options).ConfigureAwait(false);
        r.Status.ShouldBe(MaigretCheckStatus.Illegal);
        r.Error?.Type.ShouldBe("Unsupported username format");
    }

    [Fact]
    public async Task CheckSiteForUsernameAsync_HashInUsername_ReturnsIllegal()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}", "https://foo.com/", "status_code");
        var options = new QueryOptions();
        var r = await Checking.CheckSiteForUsernameAsync(site, "ale#x", options).ConfigureAwait(false);
        r.Status.ShouldBe(MaigretCheckStatus.Illegal);
    }

    [Fact]
    public async Task CheckSiteForUsernameAsync_StatusCode_PrebakedHandler_Claimed()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}", "https://foo.com/", "status_code");
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var checker = new SimpleHttpChecker(new HttpClient(handler));
        var options = new QueryOptions
        {
            Checkers = new Dictionary<string, ICheckerBase> { [string.Empty] = checker },
        };

        var r = await Checking.CheckSiteForUsernameAsync(site, "alex", options).ConfigureAwait(false);
        r.Status.ShouldBe(MaigretCheckStatus.Claimed);
        r.SiteUrlUser.ShouldBe("https://foo.com/alex");
    }

    [Fact]
    public async Task CheckSiteForUsernameAsync_TimeoutFromHandler_ReturnsUnknown()
    {
        var site = BuildSite("Foo", "https://foo.com/{username}", "https://foo.com/", "status_code");
        var handler = new StubHandler(_ => throw new TaskCanceledException("timeout"));
        var checker = new SimpleHttpChecker(new HttpClient(handler));
        var options = new QueryOptions
        {
            Checkers = new Dictionary<string, ICheckerBase> { [string.Empty] = checker },
        };

        var r = await Checking.CheckSiteForUsernameAsync(site, "alex", options).ConfigureAwait(false);
        r.Status.ShouldBe(MaigretCheckStatus.Unknown);
        r.Error?.Type.ShouldBe("Request timeout");
    }

    [Fact]
    public async Task DomainResolver_Localhost_ResolvesAs200()
    {
        await using var resolver = new DomainResolver();
        var resp = await resolver.CheckAsync("localhost").ConfigureAwait(false);
        resp.StatusCode.ShouldBe(200);
        resp.Body.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DomainResolver_BogusHost_Returns404()
    {
        await using var resolver = new DomainResolver();
        var resp = await resolver.CheckAsync("this-host-definitely-does-not-exist.invalid").ConfigureAwait(false);
        resp.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task MockChecker_AlwaysReturnsEmpty()
    {
        await using var mock = new MockChecker();
        var resp = await mock.CheckAsync("https://anything").ConfigureAwait(false);
        resp.ShouldBe(CheckResponse.Empty);
    }

    [Fact]
    public async Task CurlCffiChecker_ReturnsBotProtectionPlaceholder()
    {
        await using var checker = new CurlCffiChecker();
        var resp = await checker.CheckAsync("https://anything").ConfigureAwait(false);
        resp.Error?.Type.ShouldBe("Bot protection");
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}
