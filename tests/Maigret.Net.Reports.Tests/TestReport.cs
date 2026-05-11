using System.Globalization;
using System.Text.Json;
using Maigret.Net.Core;
using Maigret.Net.Reports;
using Maigret.Net.Reports.Scriban;
using Maigret.Net.Reports.Writers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Maigret.Net.Reports.Tests;

public class TestReport
{
    private static readonly MaigretCheckResult Claimed1 = new(
        "alex", "GitHub", "https://github.com/alex", MaigretCheckStatus.Claimed,
        idsData: new Dictionary<string, string> { ["fullname"] = "Alex Aim", ["bio"] = "Builder" },
        queryTime: TimeSpan.FromMilliseconds(123),
        tags: new[] { "social", "us" });

    private static readonly MaigretCheckResult Claimed2 = new(
        "alex", "Twitter", "https://twitter.com/alex", MaigretCheckStatus.Claimed,
        queryTime: TimeSpan.FromMilliseconds(456),
        tags: new[] { "social", "global" });

    private static readonly MaigretCheckResult NotFound = new(
        "alex", "FakeBook", "https://fake.invalid/alex", MaigretCheckStatus.Available);

    private static readonly MaigretCheckResult ErrorSite = new(
        "alex", "BrokeSite", "https://broke.invalid/alex", MaigretCheckStatus.Unknown,
        error: new CheckError("Bot protection", "Cloudflare"));

    private static readonly IReadOnlyList<MaigretCheckResult> Sample = new[] { Claimed1, NotFound, Claimed2, ErrorSite };

    private static ReportContext BuildContext() =>
        new("alex", Sample,
            generatedAt: DateTimeOffset.Parse("2026-05-04T10:00:00Z", CultureInfo.InvariantCulture));

    [Fact]
    public async Task Txt_WriterListsClaimedUrls()
    {
        var writer = new TxtReportWriter();
        using var sw = new StringWriter();
        await writer.WriteAsync(sw, BuildContext());
        var output = sw.ToString();
        output.ShouldContain("https://github.com/alex");
        output.ShouldContain("https://twitter.com/alex");
        output.ShouldNotContain("https://fake.invalid/alex");
        output.ShouldEndWith("Total Websites Username Detected On : 2");
    }

    [Fact]
    public async Task Csv_WriterEmitsHeaderAndRows()
    {
        var writer = new CsvReportWriter();
        using var sw = new StringWriter();
        await writer.WriteAsync(sw, BuildContext());
        var lines = sw.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r')).ToArray();
        lines[0].ShouldBe("username,name,url_main,url_user,exists,http_status");
        lines.Length.ShouldBe(5);
    }

    [Fact]
    public async Task Json_Simple_AggregatesClaimed()
    {
        var writer = new JsonReportWriter { ReportType = "simple" };
        using var sw = new StringWriter();
        await writer.WriteAsync(sw, BuildContext());
        using var doc = JsonDocument.Parse(sw.ToString());
        doc.RootElement.EnumerateObject().Count().ShouldBe(2);
        doc.RootElement.GetProperty("GitHub").GetProperty("status").GetString().ShouldBe("Claimed");
    }

    [Fact]
    public async Task Json_NdJson_OneLinePerClaimed()
    {
        var writer = new JsonReportWriter { ReportType = "ndjson" };
        using var sw = new StringWriter();
        await writer.WriteAsync(sw, BuildContext());
        var lines = sw.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(2);
    }

    [Fact]
    public async Task Markdown_RendersAllSections()
    {
        var writer = new MarkdownReportWriter();
        using var sw = new StringWriter();
        await writer.WriteAsync(sw, BuildContext());
        var md = sw.ToString();
        md.ShouldContain("# Report by searching on username \"alex\"");
        md.ShouldContain("returned **2** account(s)");
        md.ShouldContain("### GitHub");
        md.ShouldContain("- fullname: Alex Aim");
        md.ShouldContain("**Country tags:** us (x1)");
        md.ShouldContain("**Website tags:** social (x2)");
    }

    [Fact]
    public async Task Html_WithScribanEngine_RendersExpectedSections()
    {
        var engine = new ScribanTemplateEngine();
        var template = ScribanResources.GetDefaultHtmlTemplate();
        var writer = new HtmlReportWriter(engine, template);

        using var sw = new StringWriter();
        await writer.WriteAsync(sw, BuildContext());
        var html = sw.ToString();
        html.ShouldContain("<!DOCTYPE html>");
        html.ShouldContain("Username search report for alex");
        html.ShouldContain("4 sites checked");
        html.ShouldContain(">2</strong> found");
        html.ShouldContain("GitHub");
        html.ShouldContain("Twitter");
        html.ShouldContain("Alex Aim");
        html.ShouldContain("social");
    }

    [Fact]
    public async Task ReportPipeline_WriteToFolder_CreatesAllFormats()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"maigret-pipeline-{Guid.NewGuid():N}");
        try
        {
            var writers = new IReportWriter[]
            {
                new TxtReportWriter(),
                new CsvReportWriter(),
                new JsonReportWriter(),
                new MarkdownReportWriter(),
            };
            var pipeline = new ReportPipeline(writers);

            await pipeline.WriteToFolderAsync(dir, BuildContext(), new[] { "txt", "csv", "json", "markdown" });

            File.Exists(Path.Combine(dir, "report_alex.txt")).ShouldBeTrue();
            File.Exists(Path.Combine(dir, "report_alex.csv")).ShouldBeTrue();
            File.Exists(Path.Combine(dir, "report_alex.json")).ShouldBeTrue();
            File.Exists(Path.Combine(dir, "report_alex.md")).ShouldBeTrue();
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void DI_AddMaigretReports_RegistersAllBuiltInWriters()
    {
        var services = new ServiceCollection();
        services.AddMaigretReports();
        var sp = services.BuildServiceProvider();
        var writers = sp.GetServices<IReportWriter>().ToArray();

        writers.Length.ShouldBe(4);
        writers.Select(w => w.FormatId).OrderBy(id => id).ShouldBe(new[] { "csv", "json", "markdown", "txt" });

        var pipeline = sp.GetRequiredService<ReportPipeline>();
        pipeline.Supports("txt").ShouldBeTrue();
        pipeline.Supports("html").ShouldBeFalse();
    }

    [Fact]
    public void DI_AddMaigretScribanReports_RegistersHtmlWriter()
    {
        var services = new ServiceCollection();
        services.AddMaigretReports();
        services.AddMaigretScribanReports();
        var sp = services.BuildServiceProvider();

        var writers = sp.GetServices<IReportWriter>().ToArray();
        writers.Length.ShouldBe(5);
        writers.ShouldContain(w => w.FormatId == "html");

        var pipeline = sp.GetRequiredService<ReportPipeline>();
        pipeline.Supports("html").ShouldBeTrue();
    }
}
