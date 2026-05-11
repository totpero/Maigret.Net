// DI registration helpers for Maigret.Net.Reports — registers the writers that
// don't need a template engine (TXT/CSV/JSON/Markdown). HTML is registered
// separately by the chosen template-engine package (e.g. Maigret.Net.Reports.Scriban).

using Maigret.Net.Reports.Writers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maigret.Net.Reports;

public static class MaigretReportsServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="TxtReportWriter"/>, <see cref="CsvReportWriter"/>,
    /// <see cref="JsonReportWriter"/>, <see cref="MarkdownReportWriter"/>, and a
    /// shared <see cref="ReportPipeline"/>.
    /// </summary>
    public static IServiceCollection AddMaigretReports(this IServiceCollection services)
    {
        services.AddSingleton<IReportWriter, TxtReportWriter>();
        services.AddSingleton<IReportWriter, CsvReportWriter>();
        services.AddSingleton<IReportWriter, JsonReportWriter>();
        services.AddSingleton<IReportWriter, MarkdownReportWriter>();

        services.TryAddSingleton(sp =>
            new ReportPipeline(sp.GetServices<IReportWriter>()));

        return services;
    }
}
