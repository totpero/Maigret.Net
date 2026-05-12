// Strategy contract for a single report format. Multiple implementations are
// resolved via DI; consumers iterate `IEnumerable<IReportWriter>` and pick by
// `FormatId`.

namespace Maigret.Net.Reports.Abstractions;

/// <summary>
/// One report format. Implementations are stateless and may be shared across
/// concurrent renders.
/// </summary>
public interface IReportWriter
{
    /// <summary>
    /// Stable, lowercase identifier for the format (e.g. <c>txt</c>, <c>csv</c>,
    /// <c>json</c>, <c>markdown</c>, <c>html</c>). Used by the CLI to map
    /// flags onto writers.
    /// </summary>
    public string FormatId { get; }

    /// <summary>Default file extension (no leading dot).</summary>
    public string FileExtension { get; }

    /// <summary>Renders <paramref name="context"/> to <paramref name="writer"/>.</summary>
    public Task WriteAsync(TextWriter writer, ReportContext context, CancellationToken cancellationToken = default);
}
