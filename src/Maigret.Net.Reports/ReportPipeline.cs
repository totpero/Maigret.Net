// Convenience facade for "write all selected formats to a folder". Replaces the
// static `Report` helper of the previous design — but stays format-agnostic by
// resolving writers through the IReportWriter abstraction.

using System.Text;

namespace Maigret.Net.Reports;

/// <summary>
/// Glue helper that drives a collection of <see cref="IReportWriter"/> instances
/// from a single <see cref="ReportContext"/>.
/// </summary>
public sealed class ReportPipeline
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly IReadOnlyDictionary<string, IReportWriter> _writers;

    public ReportPipeline(IEnumerable<IReportWriter> writers)
    {
        _writers = writers
            .GroupBy(w => w.FormatId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Writers known to this pipeline, keyed by <see cref="IReportWriter.FormatId"/>.</summary>
    public IReadOnlyCollection<string> AvailableFormats => _writers.Keys.ToArray();

    /// <summary>True when a writer is registered for the given <see cref="IReportWriter.FormatId"/>.</summary>
    public bool Supports(string formatId) =>
        !string.IsNullOrEmpty(formatId) && _writers.ContainsKey(formatId);

    /// <summary>
    /// Renders <paramref name="context"/> to <paramref name="textWriter"/> using the
    /// writer registered for <paramref name="formatId"/>.
    /// </summary>
    public Task WriteToAsync(string formatId, ReportContext context, TextWriter textWriter, CancellationToken cancellationToken = default)
    {
        if (!_writers.TryGetValue(formatId, out var writer))
        {
            throw new InvalidOperationException($"No IReportWriter registered for format '{formatId}'. Known: {string.Join(", ", _writers.Keys)}");
        }

        return writer.WriteAsync(textWriter, context, cancellationToken);
    }

    /// <summary>
    /// Writes each requested format to <paramref name="folder"/>, naming files
    /// <c>report_&lt;username&gt;.&lt;extension&gt;</c>. Missing formats raise.
    /// </summary>
    public async Task WriteToFolderAsync(
        string folder,
        ReportContext context,
        IEnumerable<string> formatIds,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(folder);
        var safeUsername = MakeSafeFilename(context.Username);

        foreach (var formatId in formatIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!_writers.TryGetValue(formatId, out var writer))
            {
                throw new InvalidOperationException($"No IReportWriter registered for format '{formatId}'.");
            }

            var path = Path.Combine(folder, $"report_{safeUsername}.{writer.FileExtension}");
            await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await using var fileWriter = new StreamWriter(stream, Utf8NoBom);
            await writer.WriteAsync(fileWriter, context, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string MakeSafeFilename(string raw)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(raw.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
