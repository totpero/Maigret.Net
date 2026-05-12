// Template engine abstraction. Maigret.Net.Reports.Scriban supplies the default
// implementation; consumers can plug in Razor, Liquid, Handlebars, etc.

namespace Maigret.Net.Reports.Abstractions;

/// <summary>
/// Renders an arbitrary text template against a context object. The engine is
/// responsible for caching parsed templates if it wants to.
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// Stable identifier for the engine flavor — used by writers to pick a matching
    /// template (e.g. <c>scriban</c>, <c>liquid</c>, <c>razor</c>).
    /// </summary>
    public string EngineId { get; }

    /// <summary>
    /// Renders <paramref name="templateContent"/> against <paramref name="model"/>
    /// and returns the produced text.
    /// </summary>
    public Task<string> RenderAsync(string templateContent, object model, CancellationToken cancellationToken = default);
}
