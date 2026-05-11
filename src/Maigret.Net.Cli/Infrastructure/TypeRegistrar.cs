using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Maigret.Net.Cli.Infrastructure;

/// <summary>
/// Bridges <see cref="Spectre.Console.Cli"/>'s <see cref="ITypeRegistrar"/> to
/// the <see cref="IServiceCollection"/> used by Maigret.
/// </summary>
public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;
    private ServiceProvider? _provider;

    public TypeRegistrar(IServiceCollection services)
    {
        _services = services;
    }

    public ITypeResolver Build()
    {
        _provider ??= _services.BuildServiceProvider();
        return new TypeResolver(_provider);
    }

    public void Register(Type service, Type implementation) =>
        _services.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation) =>
        _services.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> factory) =>
        _services.AddSingleton(service, _ => factory());
}
