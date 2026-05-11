// DI helpers — register the bundled site activators.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maigret.Net.Core.Activators;

public static class MaigretActivatorsServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="TwitterGuestTokenActivator"/>, <see cref="VimeoJwtActivator"/>,
    /// <see cref="OnlyFansSignatureActivator"/> and a <see cref="MethodBasedActivationProvider"/>
    /// dispatcher (replacing the default no-op provider).
    /// </summary>
    public static IServiceCollection AddMaigretActivators(this IServiceCollection services)
    {
        services.AddHttpClient<TwitterGuestTokenActivator>();
        services.AddHttpClient<VimeoJwtActivator>();
        services.AddHttpClient<OnlyFansSignatureActivator>();

        services.AddSingleton<ISiteActivator>(sp => sp.GetRequiredService<TwitterGuestTokenActivator>());
        services.AddSingleton<ISiteActivator>(sp => sp.GetRequiredService<VimeoJwtActivator>());
        services.AddSingleton<ISiteActivator>(sp => sp.GetRequiredService<OnlyFansSignatureActivator>());

        services.Replace(ServiceDescriptor.Singleton<IActivationProvider>(sp =>
            new MethodBasedActivationProvider(
                sp.GetServices<ISiteActivator>(),
                sp.GetService<Microsoft.Extensions.Logging.ILogger<MethodBasedActivationProvider>>())));

        return services;
    }
}
