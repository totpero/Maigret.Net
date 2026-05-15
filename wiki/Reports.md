# Reports

`Maigret.Net.Reports` ships abstractions and four text-based writers. `Maigret.Net.Reports.Scriban` adds the default HTML template engine.

## Writers shipped out of the box

| `FormatId` | Writer | Extension | Source |
|---|---|---|---|
| `txt` | `TxtReportWriter` | `.txt` | `Maigret.Net.Reports` |
| `csv` | `CsvReportWriter` | `.csv` | `Maigret.Net.Reports` |
| `json` | `JsonReportWriter` (simple or ndjson) | `.json` | `Maigret.Net.Reports` |
| `markdown` | `MarkdownReportWriter` | `.md` | `Maigret.Net.Reports` |
| `html` | `HtmlReportWriter` + `ITemplateEngine` | `.html` | `Maigret.Net.Reports.Scriban` |

The CLI maps `--txt --csv --json --markdown --html` onto these writers and writes them into `--folderoutput`.

## DI registration

```csharp
services.AddMaigretReports();        // TXT / CSV / JSON / Markdown + ReportPipeline
services.AddMaigretScribanReports(); // Scriban ITemplateEngine + HTML writer
```

Skip `AddMaigretScribanReports` if you do not need the HTML report — or replace it with your own template engine.

## Library usage

```csharp
using Maigret.Net.Core;
using Maigret.Net.Reports;
using Maigret.Net.Reports.Models;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddMaigret();
services.AddMaigretReports();
services.AddMaigretScribanReports();
var sp = services.BuildServiceProvider();

var client   = new MaigretClient();
var summary  = await client.SearchAsync("johndoe");
var pipeline = sp.GetRequiredService<ReportPipeline>();

var ctx = new ReportContext(summary.Username, summary.Results);
await pipeline.WriteToFolderAsync("./reports", ctx, new[] { "txt", "csv", "json", "markdown", "html" });
```

Files are written as `report_<username>.<extension>`.

## `ReportContext`

| Field | Notes |
|---|---|
| `Username` | The user being reported |
| `Results` | Full `IReadOnlyList<MaigretCheckResult>` |
| `IdType` | Default `username`; surface for templates |
| `GeneratedAt` | `DateTimeOffset.Now` if you don't pass one |
| `Extras` | Free-form `Dictionary<string, string>` your engine can read |

Convenience views: `Claimed`, `TotalChecked`, `TotalFound`, `GeneratedAtIso8601`.

## Custom writers

Implement `IReportWriter`:

```csharp
public sealed class PdfReportWriter : IReportWriter
{
    public string FormatId => "pdf";
    public string FileExtension => "pdf";

    public async Task WriteAsync(TextWriter writer, ReportContext context, CancellationToken ct = default)
    {
        // ... your PDF generation, then writer.Write(base64) etc.
    }
}

services.AddSingleton<IReportWriter, PdfReportWriter>();
```

The `ReportPipeline` discovers every registered `IReportWriter` and exposes it under the same flow.

## Custom template engines

`ITemplateEngine` is a one-method interface. Drop in Razor, Liquid, Handlebars, or a hand-rolled string formatter:

```csharp
public sealed class MyEngine : ITemplateEngine
{
    public string EngineId => "razor";

    public Task<string> RenderAsync(string templateContent, object model, CancellationToken ct = default)
    {
        // ... use RazorLight / your favorite renderer
    }
}

services.AddSingleton<ITemplateEngine, MyEngine>();
services.AddSingleton<IReportWriter>(sp =>
    new HtmlReportWriter(
        sp.GetRequiredService<ITemplateEngine>(),
        File.ReadAllText("templates/my-report.razor")));
```

Template authors receive an `IReportTemplateModel` view of the search results — see the bundled `Maigret.Net.Reports.Scriban/Resources/simple_report.html.scriban` for the data shape and a worked example.

## NDJSON

```csharp
var ndjsonWriter = new JsonReportWriter { ReportType = "ndjson" };
// Yields one JSON object per claimed site, separated by newlines.
```

Register it as the default JSON writer or alongside the simple one:

```csharp
services.AddSingleton<IReportWriter>(_ => new JsonReportWriter { ReportType = "ndjson" });
```
