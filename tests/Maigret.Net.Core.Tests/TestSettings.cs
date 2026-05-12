using System.Text.Json;
using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestSettings : IDisposable
{
    private readonly List<string> _temps = [];

    private string WriteTemp(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"maigret-settings-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, contents);
        _temps.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var p in _temps)
        {
            try { File.Delete(p); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void LoadFromEmbedded_PopulatesPythonDefaults()
    {
        var settings = Settings.LoadFromEmbedded();

        settings.Timeout.ShouldBe(30);
        settings.MaxConnections.ShouldBe(100);
        settings.RecursiveSearch.ShouldBeTrue();
        settings.InfoExtracting.ShouldBeTrue();
        settings.TopSitesCount.ShouldBe(500);
        settings.TorProxyUrl.ShouldBe("socks5://127.0.0.1:9050");
        settings.PresenceStrings.ShouldContain("404");
        settings.SupposedUsernames.ShouldContain("admin");
    }

    [Fact]
    public void LoadDefaults_LayersFilesOnTopOfEmbedded()
    {
        var file1 = WriteTemp("""{"timeout": 10, "retries_count": 3, "proxy_url": "http://proxy1"}""");
        var file2 = WriteTemp("""{"timeout": 20, "recursive_search": true}""");
        var file3 = WriteTemp("""{"proxy_url": "http://proxy3", "print_not_found": false}""");

        var settings = Settings.LoadDefaults([file1, file2, file3]);

        settings.RetriesCount.ShouldBe(3);
        settings.Timeout.ShouldBe(20);          // last writer wins
        settings.RecursiveSearch.ShouldBeTrue();
        settings.ProxyUrl.ShouldBe("http://proxy3");
        settings.PrintNotFound.ShouldBeFalse();
    }

    [Fact]
    public void LoadDefaults_MissingPathsAreIgnored()
    {
        var settings = Settings.LoadDefaults([Path.Combine(Path.GetTempPath(), "definitely-not-here.json")]);
        // Embedded defaults still applied:
        settings.MaxConnections.ShouldBe(100);
    }

    [Fact]
    public void MergeFromFile_InvalidJson_Throws()
    {
        var bogus = WriteTemp("{ this is not valid json");
        var settings = Settings.LoadFromEmbedded();
        Should.Throw<JsonException>(() => settings.MergeFromFile(bogus));
    }
}
