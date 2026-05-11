using System.Globalization;

namespace Maigret.Net.Reports.Writers;

/// <summary>CSV report with header <c>username,name,url_main,url_user,exists,http_status</c>.</summary>
public sealed class CsvReportWriter : IReportWriter
{
    public string FormatId => "csv";
    public string FileExtension => "csv";

    public Task WriteAsync(TextWriter writer, ReportContext context, CancellationToken cancellationToken = default)
    {
        writer.WriteLine("username,name,url_main,url_user,exists,http_status");
        foreach (var r in context.Results)
        {
            writer.Write(EscapeCsv(context.Username));
            writer.Write(',');
            writer.Write(EscapeCsv(r.SiteName));
            writer.Write(',');
            writer.Write(EscapeCsv(string.Empty));
            writer.Write(',');
            writer.Write(EscapeCsv(r.SiteUrlUser));
            writer.Write(',');
            writer.Write(EscapeCsv(r.Status.ToString()));
            writer.Write(',');
            writer.WriteLine(r.QueryTime?.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture) ?? "0");
        }
        return Task.CompletedTask;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuote = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
        if (!needsQuote)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
