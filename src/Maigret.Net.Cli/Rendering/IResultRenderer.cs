namespace Maigret.Net.Cli.Rendering;

/// <summary>
/// CLI rendering surface. <see cref="SpectreResultRenderer"/> writes to
/// <see cref="Spectre.Console.AnsiConsole"/>; tests substitute fakes.
/// </summary>
public interface IResultRenderer
{
    public void RenderBanner(string version);
    public void RenderSearchStart(string username, string idType, int siteCount);
    public void RenderResult(MaigretCheckResult result, bool printAll);
    public void RenderSearchComplete(string username, int claimedCount);
    public void RenderError(string message);
}
