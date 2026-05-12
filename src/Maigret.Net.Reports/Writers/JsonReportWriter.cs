using System.Text.Encodings.Web;
using System.Text.Json;

namespace Maigret.Net.Reports.Writers;

/// <summary>JSON report writer. Defaults to "simple" (object aggregate); set <see cref="ReportType"/> to <c>ndjson</c> for one line per claimed site.</summary>
public sealed class JsonReportWriter : IReportWriter
{
    private static readonly JsonSerializerOptions Compact = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    private static readonly JsonSerializerOptions Indented = new(Compact) { WriteIndented = true };

    public string FormatId => "json";
    public string FileExtension => "json";

    /// <summary>Either <c>simple</c> or <c>ndjson</c>. Anything else falls back to simple.</summary>
    public string ReportType { get; init; } = "simple";

    public Task WriteAsync(TextWriter writer, ReportContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(context);

        var perLine = ReportType.StartsWith("ndjson", StringComparison.OrdinalIgnoreCase);
        var claimed = context.Results.Where(r => r.Status == MaigretCheckStatus.Claimed);

        if (perLine)
        {
            foreach (var r in claimed)
            {
                writer.WriteLine(JsonSerializer.Serialize(ToEntry(context.Username, r), Compact));
            }

            return Task.CompletedTask;
        }

        var aggregate = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var r in claimed)
        {
            aggregate[r.SiteName] = ToEntry(context.Username, r);
        }

        writer.Write(JsonSerializer.Serialize(aggregate, Indented));
        return Task.CompletedTask;
    }

    private static Dictionary<string, object?> ToEntry(string username, MaigretCheckResult r) => new()
    {
        ["username"] = username,
        ["site_name"] = r.SiteName,
        ["url"] = r.SiteUrlUser,
        ["status"] = r.Status.ToString(),
        ["ids"] = r.IdsData ?? (object)new Dictionary<string, string>(),
        ["tags"] = r.Tags,
        ["query_time_ms"] = r.QueryTime?.TotalMilliseconds,
    };
}
