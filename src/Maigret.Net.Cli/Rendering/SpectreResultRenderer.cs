using Maigret.Net.Core;
using Spectre.Console;

namespace Maigret.Net.Cli.Rendering;

public sealed class SpectreResultRenderer : IResultRenderer
{
    public void RenderBanner(string version)
    {
        AnsiConsole.MarkupLine($"[bold green]maigret.net[/] [grey]v{Markup.Escape(version)}[/]");
        AnsiConsole.MarkupLine("[grey]Username search across 1,800+ sites — .NET port of soxoj/maigret[/]");
        AnsiConsole.WriteLine();
    }

    public void RenderSearchStart(string username, string idType, int siteCount)
    {
        AnsiConsole.MarkupLine(
            $"[green][[[/][yellow]*[/][green]]][/] Checking [bold]{Markup.Escape(idType)}[/] " +
            $"[white]{Markup.Escape(username)}[/] on [bold]{siteCount}[/] sites:");
    }

    public void RenderResult(MaigretCheckResult result, bool printAll)
    {
        switch (result.Status)
        {
            case MaigretCheckStatus.Claimed:
                AnsiConsole.MarkupLine(
                    $"[white][[[/][green]+[/][white]]][/] [green]{Markup.Escape(result.SiteName)}[/]: {Markup.Escape(result.SiteUrlUser)}");
                if (result.IdsData is { Count: > 0 })
                {
                    foreach (var (k, v) in result.IdsData)
                    {
                        AnsiConsole.MarkupLine($"  [grey]├─ {Markup.Escape(k)}: {Markup.Escape(v)}[/]");
                    }
                }
                break;
            case MaigretCheckStatus.Available when printAll:
                AnsiConsole.MarkupLine(
                    $"[white][[[/][red]-[/][white]]][/] [yellow]{Markup.Escape(result.SiteName)}[/]: Not found!");
                break;
            case MaigretCheckStatus.Unknown when printAll:
                AnsiConsole.MarkupLine(
                    $"[white][[[/][red]?[/][white]]][/] [red]{Markup.Escape(result.SiteName)}[/]: " +
                    Markup.Escape(result.Error?.ToString() ?? "Unknown error"));
                break;
            case MaigretCheckStatus.Illegal when printAll:
                AnsiConsole.MarkupLine(
                    $"[white][[[/][red]-[/][white]]][/] [yellow]{Markup.Escape(result.SiteName)}[/]: Illegal username for this site");
                break;
        }
    }

    public void RenderSearchComplete(string username, int claimedCount)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"[green][[[/][yellow]*[/][green]]][/] [bold]{Markup.Escape(username)}[/]: " +
            $"[bold green]{claimedCount}[/] result(s) found.");
        AnsiConsole.WriteLine();
    }

    public void RenderError(string message) =>
        AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
}
