using Maigret.Net.Core;
using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestErrors
{
    private static MaigretCheckResult Err(string type) =>
        new("u", "site", "https://x.test/u", MaigretCheckStatus.Unknown, error: new CheckError(type));

    private static MaigretCheckResult Ok() =>
        new("u", "site", "https://x.test/u", MaigretCheckStatus.Claimed);

    [Fact]
    public void ExtractAndGroup_RanksByFrequency()
    {
        var stats = CommonErrors.ExtractAndGroup(new[]
        {
            Err("Captcha"),
            Err("Bot protection"),
            Err("Captcha"),
            Ok(),
        });

        stats.Count.ShouldBe(2);
        stats[0].Type.ShouldBe("Captcha");
        stats[0].Count.ShouldBe(2);
        stats[0].Percent.ShouldBe(50.0);
    }

    [Fact]
    public void NotifyAboutErrors_MatchesPythonOutput()
    {
        var results = new[]
        {
            Err("Captcha"),
            Err("Bot protection"),
            Err("Access denied"),
            Ok(),
        };

        var notifications = CommonErrors.NotifyAboutErrors(results, showStatistics: true);

        notifications.ShouldContain((
            "Too many errors of type \"Captcha\" (25%). Try to switch to another IP address or to use service cookies",
            "!"));
        notifications.ShouldContain((
            "Too many errors of type \"Bot protection\" (25%). Try to switch to another IP address",
            "!"));
        notifications.ShouldContain(("Too many errors of type \"Access denied\" (25%)", "!"));
        notifications.ShouldContain(("Verbose error statistics:", "-"));
        notifications.ShouldContain(("You can see detailed site check errors with a flag `--print-errors`", "-"));
    }

    [Fact]
    public void NotifyAboutErrors_NoImportantNoise_OmitsHeader()
    {
        // 1 Captcha out of 100 = 1% — below threshold (3).
        var rs = new List<MaigretCheckResult> { Err("Captcha") };
        for (var i = 0; i < 99; i++)
        {
            rs.Add(Ok());
        }

        var n = CommonErrors.NotifyAboutErrors(rs);
        n.ShouldBeEmpty();
    }

    [Fact]
    public void IsPermanent_KnownTransient_False()
    {
        CommonErrors.IsPermanent("Request timeout").ShouldBeFalse();
        CommonErrors.IsPermanent("Captcha").ShouldBeTrue();
    }

    [Fact]
    public void Detect_Cloudflare_ReturnsBotProtection()
    {
        var html = "<title>Just a moment</title>";
        var err = CommonErrors.Detect(html);
        err.ShouldNotBeNull();
        err.Type.ShouldBe("Bot protection");
    }
}
