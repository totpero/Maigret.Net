// Default ITemplateEngine implementation backed by Scriban.

using Scriban;
using Scriban.Runtime;

namespace Maigret.Net.Reports.Scriban;

/// <summary>
/// Renders Scriban templates against an <see cref="IReportTemplateModel"/> or
/// any object exposing a <c>ToDictionary()</c> method.
/// </summary>
public sealed class ScribanTemplateEngine : ITemplateEngine
{
    private readonly Dictionary<string, Template> _cache = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public string EngineId => "scriban";

    public Task<string> RenderAsync(string templateContent, object model, CancellationToken cancellationToken = default)
    {
        if (templateContent is null)
        {
            throw new ArgumentNullException(nameof(templateContent));
        }

        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var template = GetOrParse(templateContent);
        if (template.HasErrors)
        {
            throw new InvalidOperationException(
                "Scriban template parse errors: " + string.Join("; ", template.Messages));
        }

        var ctx = new TemplateContext { MemberRenamer = m => m.Name };
        var scriptObj = new ScriptObject();

        switch (model)
        {
            case ReportTemplateModel rtm:
                scriptObj.Import(rtm.ToDictionary());
                break;
            case IDictionary<string, object?> dict:
                scriptObj.Import(dict);
                break;
            default:
                scriptObj.Import(model);
                break;
        }

        ctx.PushGlobal(scriptObj);
        return Task.FromResult(template.Render(ctx));
    }

    private Template GetOrParse(string content)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(content, out var cached))
            {
                return cached;
            }

            var parsed = Template.Parse(content);
            _cache[content] = parsed;
            return parsed;
        }
    }
}
