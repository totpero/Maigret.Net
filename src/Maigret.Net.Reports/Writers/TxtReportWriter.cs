using Maigret.Net.Core;

namespace Maigret.Net.Reports.Writers;

/// <summary>Plain-text report (one claimed profile URL per line + a summary footer).</summary>
public sealed class TxtReportWriter : IReportWriter
{
    public string FormatId => "txt";
    public string FileExtension => "txt";

    public Task WriteAsync(TextWriter writer, ReportContext context, CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var r in context.Results)
        {
            if (r.Status != MaigretCheckStatus.Claimed)
            {
                continue;
            }

            count++;
            writer.WriteLine(r.SiteUrlUser);
        }
        writer.Write($"Total Websites Username Detected On : {count}");
        return Task.CompletedTask;
    }
}
