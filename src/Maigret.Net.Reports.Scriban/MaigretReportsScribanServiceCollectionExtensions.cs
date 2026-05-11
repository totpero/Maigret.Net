// DI helpers — registers the Scriban template engine and an HtmlReportWriter
// configured with the default embedded template.

using Maigret.Net.Reports.Writers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maigret.Net.Reports.Scriban;

public static class MaigretReportsScribanServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ScribanTemplateEngine"/> as the default
    /// <see cref="ITemplateEngine"/> and an HTML <see cref="IReportWriter"/>
    /// using the bundled default template.
    /// </summary>
    public static IServiceCollection AddMaigretScribanReports(
        this IServiceCollection services,
        string? customHtmlTemplate = null)
    {
        services.TryAddSingleton<ITemplateEngine, ScribanTemplateEngine>();

        services.AddSingleton<IReportWriter>(sp =>
        {
            var engine = sp.GetRequiredService<ITemplateEngine>();
            var template = customHtmlTemplate ?? ScribanResources.GetDefaultHtmlTemplate();
            return new HtmlReportWriter(engine, template);
        });

        return services;
    }
}
