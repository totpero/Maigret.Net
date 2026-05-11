// DI helpers — registers RuleBasedIdExtractor with the bundled BuiltInExtractionRules.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maigret.Net.Core.IdExtraction;

public static class MaigretIdExtractionServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default no-op <see cref="IIdExtractor"/> with a
    /// <see cref="RuleBasedIdExtractor"/> initialized from
    /// <see cref="BuiltInExtractionRules.All"/> plus any extra rules registered
    /// via <see cref="ExtractionRule"/> singletons.
    /// </summary>
    public static IServiceCollection AddMaigretIdExtraction(this IServiceCollection services)
    {
        foreach (var rule in BuiltInExtractionRules.All)
        {
            services.AddSingleton(rule);
        }

        services.Replace(ServiceDescriptor.Singleton<IIdExtractor>(sp =>
            new RuleBasedIdExtractor(sp.GetServices<ExtractionRule>())));

        return services;
    }
}
