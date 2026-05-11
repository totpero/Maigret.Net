using Maigret.Net.Cli;
using Maigret.Net.Cli.Infrastructure;
using Maigret.Net.Cli.Rendering;
using Maigret.Net.Core;
using Maigret.Net.Core.Activators;
using Maigret.Net.Core.IdExtraction;
using Maigret.Net.Core.RecursiveSearch;
using Maigret.Net.Reports;
using Maigret.Net.Reports.Scriban;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
services.AddMaigret();
services.AddMaigretActivators();
services.AddMaigretIdExtraction();
services.AddMaigretRecursiveSearch();
services.AddMaigretReports();
services.AddMaigretScribanReports();
services.AddSingleton<IResultRenderer, SpectreResultRenderer>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp<SearchCommand>(registrar);

app.Configure(config =>
{
    config.SetApplicationName("maigret");
    config.SetApplicationVersion(MaigretDefaults.Version);
});

return await app.RunAsync(args).ConfigureAwait(false);
