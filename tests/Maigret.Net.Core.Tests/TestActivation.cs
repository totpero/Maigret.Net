using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Maigret.Net.Core;
using Maigret.Net.Core.Activators;
using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestActivation
{
    private static MaigretSite SiteWithActivation(string method, object activationFields, IDictionary<string, string>? headers = null)
    {
        var info = new
        {
            url = "https://example.com/{username}",
            urlMain = "https://example.com/",
            checkType = "status_code",
            type = "username",
            activation = MergeActivation(method, activationFields),
            headers,
        };
        var json = JsonSerializer.Serialize(info, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
        using var doc = JsonDocument.Parse(json);
        return new MaigretSite("X", doc.RootElement);
    }

    private static Dictionary<string, object?> MergeActivation(string method, object fields)
    {
        var result = new Dictionary<string, object?> { ["method"] = method };
        var props = fields.GetType().GetProperties();
        foreach (var p in props)
        {
            result[p.Name] = p.GetValue(fields);
        }

        return result;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; init; } = _ => new HttpResponseMessage(HttpStatusCode.OK);
        public List<HttpRequestMessage> Captured { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Captured.Add(request);
            return Task.FromResult(Responder(request));
        }
    }

    private sealed class FakeActivator : ISiteActivator
    {
        public string Method { get; init; } = "test";
        public int Calls { get; private set; }
        public Task ActivateAsync(MaigretSite site, string? probedUrl, CancellationToken cancellationToken = default)
        {
            Calls++;
            site.Headers["x-activated"] = "yes";
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void MethodBased_NoActivation_ReturnsFalse()
    {
        var site = SiteWithActivation("twitter", new { url = "https://x.test", src = "guest_token" });
        // Wipe activation to simulate "no marks found yet" case — provider doesn't gatekeep on marks.
        // We just assert that having no matching activator → CanActivate=false.
        var provider = new MethodBasedActivationProvider(Array.Empty<ISiteActivator>());
        provider.CanActivate(site).ShouldBeFalse();
    }

    [Fact]
    public async Task MethodBased_DispatchesToMatchingActivator()
    {
        var fake = new FakeActivator { Method = "vimeo" };
        var provider = new MethodBasedActivationProvider(new[] { (ISiteActivator)fake });

        var site = SiteWithActivation("vimeo", new { url = "https://api.vimeo.com" });
        provider.CanActivate(site).ShouldBeTrue();

        await provider.ActivateAsync(site, probedUrl: null).ConfigureAwait(false);
        fake.Calls.ShouldBe(1);
        site.Headers.ShouldContainKeyAndValue("x-activated", "yes");
    }

    [Fact]
    public async Task TwitterActivator_PostsAndSetsGuestToken()
    {
        var handler = new StubHandler
        {
            Responder = req =>
            {
                req.Method.ShouldBe(HttpMethod.Post);
                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"guest_token":"GT-1"}""", Encoding.UTF8, "application/json"),
                };
                return resp;
            },
        };
        var activator = new TwitterGuestTokenActivator(new HttpClient(handler));
        var site = SiteWithActivation(
            "twitter",
            new { url = "https://api.twitter.com/1.1/guest/activate.json", src = "guest_token" },
            new Dictionary<string, string> { ["x-guest-token"] = "stale" });

        await activator.ActivateAsync(site, probedUrl: null).ConfigureAwait(false);

        site.Headers["x-guest-token"].ShouldBe("GT-1");
        // The probe must NOT carry the stale token — verify it was stripped on the request:
        handler.Captured[0].Headers.Contains("x-guest-token").ShouldBeFalse();
    }

    [Fact]
    public async Task VimeoActivator_GetsAndSetsAuthorization()
    {
        var handler = new StubHandler
        {
            Responder = req =>
            {
                req.Method.ShouldBe(HttpMethod.Get);
                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"jwt":"VMJWT"}""", Encoding.UTF8, "application/json"),
                };
                return resp;
            },
        };
        var activator = new VimeoJwtActivator(new HttpClient(handler));
        var site = SiteWithActivation(
            "vimeo",
            new { url = "https://vimeo.com/_rv/viewer" },
            new Dictionary<string, string> { ["Authorization"] = "jwt stale" });

        await activator.ActivateAsync(site, probedUrl: null).ConfigureAwait(false);

        site.Headers["Authorization"].ShouldBe("jwt VMJWT");
        handler.Captured[0].Headers.Contains("Authorization").ShouldBeFalse();
    }

    [Fact]
    public async Task OnlyFansActivator_SignsAndStoresHeaders()
    {
        // Indices/constant chosen so that any SHA1 produces a small checksum.
        var handler = new StubHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) };
        var activator = new OnlyFansSignatureActivator(new HttpClient(handler));
        var site = SiteWithActivation(
            "onlyfans",
            new
            {
                url = "https://onlyfans.com/api2/v2/init",
                static_param = "static",
                checksum_indexes = new[] { 0, 1, 2 },
                checksum_constant = -100,
                format = "{0}:{1}",
            });

        await activator.ActivateAsync(site, probedUrl: "https://onlyfans.com/api2/v2/users/me").ConfigureAwait(false);

        site.Headers.ShouldContainKey("time");
        site.Headers.ShouldContainKey("sign");
        site.Headers["sign"].ShouldContain(":");
        site.Headers["x-bc"].Length.ShouldBe(40); // 20 bytes hex
    }
}
