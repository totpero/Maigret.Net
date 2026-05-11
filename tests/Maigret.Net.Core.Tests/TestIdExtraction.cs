using System.Text.Json;
using System.Text.RegularExpressions;
using Maigret.Net.Core;
using Maigret.Net.Core.IdExtraction;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestIdExtraction
{
    private static MaigretSite Site(string name) =>
        new(name, JsonDocument.Parse("""{"url":"https://x.test/{username}","urlMain":"https://x.test/","checkType":"status_code","type":"username"}""").RootElement);

    [Fact]
    public void RuleBasedExtractor_NoMatch_ReturnsEmpty()
    {
        var extractor = new RuleBasedIdExtractor(BuiltInExtractionRules.All);
        var result = extractor.Extract("<html>nothing of interest</html>", Site("Unknown"));
        result.ShouldBeEmpty();
    }

    [Fact]
    public void RuleBasedExtractor_GitHub_ExtractsUsernameAndFullname()
    {
        var html = """
            <html>
              <meta name="user-login" content="octocat">
              <span class="p-name vcard-fullname d-block overflow-hidden" itemprop="name" >Octo Cat</span>
            </html>
            """;
        var extractor = new RuleBasedIdExtractor(BuiltInExtractionRules.All);
        var result = extractor.Extract(html, Site("GitHub"));

        result["username"].ShouldBe("octocat");
        result["fullname"].ShouldBe("Octo Cat");
    }

    [Fact]
    public void RuleBasedExtractor_Instagram_ExtractsBio()
    {
        var html = """
            {"username":"alex","full_name":"Alex Aim","biography":"Builder & explorer"}
            """;
        var extractor = new RuleBasedIdExtractor(BuiltInExtractionRules.All);
        var result = extractor.Extract(html, Site("Instagram"));

        result["username"].ShouldBe("alex");
        result["fullname"].ShouldBe("Alex Aim");
        result["bio"].ShouldBe("Builder & explorer");
    }

    [Fact]
    public void RuleBasedExtractor_HtmlEntities_AreDecoded()
    {
        var rule = new ExtractionRule("Foo", new[]
        {
            new ExtractionPattern("name", new Regex(@"name=""([^""]+)""")),
        });
        var extractor = new RuleBasedIdExtractor(new[] { rule });
        var result = extractor.Extract("""<meta name="Alex &amp; Bob">""", Site("Foo"));

        result["name"].ShouldBe("Alex & Bob");
    }

    [Fact]
    public void RuleBasedExtractor_FirstMatch_Wins()
    {
        var rule = new ExtractionRule("Foo", new[]
        {
            new ExtractionPattern("name", new Regex(@"a=([a-z]+)")),
            new ExtractionPattern("name", new Regex(@"b=([a-z]+)")),
        });
        var extractor = new RuleBasedIdExtractor(new[] { rule });
        var result = extractor.Extract("a=alpha b=beta", Site("Foo"));

        result["name"].ShouldBe("alpha");
    }

    [Fact]
    public void DI_AddMaigretIdExtraction_ReplacesNullExtractor()
    {
        var services = new ServiceCollection();
        services.AddMaigret();
        services.AddMaigretIdExtraction();
        var sp = services.BuildServiceProvider();

        var extractor = sp.GetRequiredService<IIdExtractor>();
        extractor.ShouldBeOfType<RuleBasedIdExtractor>();

        // GitHub rule should still trigger end-to-end through DI.
        var html = """<meta name="user-login" content="ada">""";
        extractor.Extract(html, Site("GitHub"))["username"].ShouldBe("ada");
    }

    [Fact]
    public void DI_AddMaigretIdExtraction_AcceptsExtraRules()
    {
        var services = new ServiceCollection();
        services.AddMaigret();
        services.AddSingleton(new ExtractionRule("CustomSite", new[]
        {
            new ExtractionPattern("answer", new Regex(@"answer=(\d+)")),
        }));
        services.AddMaigretIdExtraction();

        var sp = services.BuildServiceProvider();
        var extractor = sp.GetRequiredService<IIdExtractor>();

        extractor.Extract("answer=42", Site("CustomSite"))["answer"].ShouldBe("42");
        // Built-ins still active:
        extractor.Extract("""<meta name="user-login" content="ada">""", Site("GitHub"))["username"].ShouldBe("ada");
    }
}
