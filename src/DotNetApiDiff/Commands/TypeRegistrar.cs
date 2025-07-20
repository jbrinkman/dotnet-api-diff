// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace DotNetApiDiff.Commands;

/// <summary>
/// Type registrar for Spectre.Console.Cli that uses Microsoft.Extensions.DependencyInjection
/// </summary>
internal sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeRegistrar"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public TypeRegistrar(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeRegistrar"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public TypeRegistrar(IServiceProvider serviceProvider)
    {
        _services = new ServiceCollection();

        // Add the service provider itself
        _services.AddSingleton(serviceProvider);
    }

    /// <summary>
    /// Builds the service provider
    /// </summary>
    /// <returns>The service provider</returns>
    public ITypeResolver Build()
    {
        return new TypeResolver(_services.BuildServiceProvider());
    }

    /// <summary>
    /// Registers a service as a specific type
    /// </summary>
    /// <param name="service">The service type</param>
    /// <param name="implementation">The implementation type</param>
    public void Register(Type service, Type implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    /// <summary>
    /// Registers an instance as a specific type
    /// </summary>
    /// <param name="service">The service type</param>
    /// <param name="implementation">The implementation instance</param>
    public void RegisterInstance(Type service, object implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    /// <summary>
    /// Registers a factory for a specific type
    /// </summary>
    /// <param name="service">The service type</param>
    /// <param name="factory">The factory</param>
    public void RegisterLazy(Type service, Func<object> factory)
    {
        _services.AddSingleton(service, _ => factory());
    }
}

/// <summary>
/// Type resolver for Spectre.Console.Cli that uses Microsoft.Extensions.DependencyInjection
/// </summary>
internal sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeResolver"/> class.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>
    /// Resolves an instance of the specified type
    /// </summary>
    /// <param name="type">The type to resolve</param>
    /// <returns>The resolved instance</returns>
    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        return _provider.GetService(type);
    }

    /// <summary>
    /// Disposes the resolver
    /// </summary>
    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
