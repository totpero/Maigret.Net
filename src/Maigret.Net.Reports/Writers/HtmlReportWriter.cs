// HTML writer that delegates rendering to a pluggable ITemplateEngine. The
// template content is supplied by the caller (so a consumer can drop in their
// own HTML/CSS) — the default template ships with Maigret.Net.Reports.Scriban.

namespace Maigret.Net.Reports.Writers;

/// <summary>
/// Renders an HTML report through an <see cref="ITemplateEngine"/>. The template
/// content is configured at construction time so different consumers can use
/// different layouts.
/// </summary>
public sealed class HtmlReportWriter : IReportWriter
{
    private readonly ITemplateEngine _engine;
    private readonly string _templateContent;

    public HtmlReportWriter(ITemplateEngine engine, string templateContent)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        if (string.IsNullOrWhiteSpace(templateContent))
        {
            throw new ArgumentException("Template content is required.", nameof(templateContent));
        }

        _templateContent = templateContent;
    }

    public string FormatId => "html";
    public string FileExtension => "html";

    public async Task WriteAsync(TextWriter writer, ReportContext context, CancellationToken cancellationToken = default)
    {
        var model = new ReportTemplateModel(context);
        var rendered = await _engine.RenderAsync(_templateContent, model, cancellationToken).ConfigureAwait(false);
        await writer.WriteAsync(rendered).ConfigureAwait(false);
    }
}
