using System.Buffers;
using System.Globalization;

namespace Maigret.Net.Reports.Writers;

/// <summary>CSV report with header <c>username,name,url_main,url_user,exists,http_status</c>.</summary>
public sealed class CsvReportWriter : IReportWriter
{
    private static readonly SearchValues<char> CsvSpecialChars = SearchValues.Create(",\"\n\r");

    public string FormatId => "csv";
    public string FileExtension => "csv";

    public Task WriteAsync(TextWriter writer, ReportContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(context);

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

        var needsQuote = value.AsSpan().ContainsAny(CsvSpecialChars);
        return !needsQuote ? value : $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
