using Maigret.Net.Core;
using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestNotify
{
    private static string CaptureConsoleOutput(Action action)
    {
        var prev = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            action();
        }
        finally
        {
            Console.SetOut(prev);
        }
        return writer.ToString();
    }

    [Fact]
    public void Start_NoColor_PrintsTitle()
    {
        var notify = new QueryNotifyConsole(color: false);
        var output = CaptureConsoleOutput(() => notify.Start("alice"));
        output.ShouldContain("[*] Checking username alice on:");
    }

    [Fact]
    public void Start_Silent_PrintsNothing()
    {
        var notify = new QueryNotifyConsole(silent: true);
        var output = CaptureConsoleOutput(() => notify.Start("alice"));
        output.ShouldBeEmpty();
    }

    [Fact]
    public void Update_Claimed_NoColor_PrintsPlusLine()
    {
        var notify = new QueryNotifyConsole(color: false);
        var result = new MaigretCheckResult("alice", "GitHub", "https://github.com/alice", MaigretCheckStatus.Claimed);

        var output = CaptureConsoleOutput(() => notify.Update(result));
        output.ShouldContain("[+] GitHub: https://github.com/alice");
    }

    [Fact]
    public void Update_Available_NoColor_PrintsMinusLine_WhenAllowed()
    {
        var notify = new QueryNotifyConsole(color: false, printFoundOnly: false);
        var result = new MaigretCheckResult("alice", "GitHub", "https://github.com/alice", MaigretCheckStatus.Available);

        var output = CaptureConsoleOutput(() => notify.Update(result));
        output.ShouldContain("[-] GitHub: Not found!");
    }

    [Fact]
    public void Update_Available_Suppressed_WhenPrintFoundOnly()
    {
        var notify = new QueryNotifyConsole(color: false, printFoundOnly: true);
        var result = new MaigretCheckResult("alice", "GitHub", "https://github.com/alice", MaigretCheckStatus.Available);

        var output = CaptureConsoleOutput(() => notify.Update(result));
        output.ShouldBeEmpty();
    }

    [Fact]
    public void Update_Unknown_NoColor_PrintsErrorContext()
    {
        var notify = new QueryNotifyConsole(color: false);
        var result = new MaigretCheckResult(
            "alice", "GitHub", "https://github.com/alice", MaigretCheckStatus.Unknown,
            error: new CheckError("Captcha", "Cloudflare"));

        var output = CaptureConsoleOutput(() => notify.Update(result));
        output.ShouldContain("[?] GitHub: Captcha error: Cloudflare");
    }

    [Fact]
    public void Update_IsSimilar_PrintsQuestionMark()
    {
        var notify = new QueryNotifyConsole(color: false);
        var result = new MaigretCheckResult("alice", "GitHub", "https://github.com/alice", MaigretCheckStatus.Claimed);

        var output = CaptureConsoleOutput(() => notify.Update(result, isSimilar: true));
        output.ShouldContain("[?] GitHub: https://github.com/alice");
    }
}
