// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

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

        // Add the service provider itself so commands can access it
        _services.AddSingleton(serviceProvider);

        // Add logging services from the original provider
        _services.AddSingleton<ILoggerFactory>(provider =>
            serviceProvider.GetRequiredService<ILoggerFactory>());
        _services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        // Add other required services from the original provider
        _services.AddSingleton<IGlobalExceptionHandler>(provider =>
            serviceProvider.GetRequiredService<IGlobalExceptionHandler>());
        _services.AddSingleton<IExitCodeManager>(provider =>
            serviceProvider.GetRequiredService<IExitCodeManager>());

        // Add core API services from the original provider
        _services.AddScoped<IAssemblyLoader>(provider =>
            serviceProvider.GetRequiredService<IAssemblyLoader>());
        _services.AddScoped<IApiExtractor>(provider =>
            serviceProvider.GetRequiredService<IApiExtractor>());
        _services.AddScoped<IApiComparer>(provider =>
            serviceProvider.GetRequiredService<IApiComparer>());
        _services.AddScoped<IReportGenerator>(provider =>
            serviceProvider.GetRequiredService<IReportGenerator>());

        // Register the CompareCommand to be resolved from the original service provider
        _services.AddTransient<CompareCommand>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<CompareCommand>>();
            var exitCodeManager = serviceProvider.GetRequiredService<IExitCodeManager>();
            var exceptionHandler = serviceProvider.GetRequiredService<IGlobalExceptionHandler>();
            return new CompareCommand(serviceProvider, logger, exitCodeManager, exceptionHandler);
        });
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
    public object? Resolve([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type? type)
    {
        if (type == null)
        {
            return null;
        }

        // Settings classes should be created directly by Spectre.Console, not through DI
        // Check both by type name and inheritance to be absolutely sure
        if (typeof(CommandSettings).IsAssignableFrom(type) ||
            type.Name.EndsWith("CommandSettings") ||
            type.BaseType?.Name == "CommandSettings")
        {
            try
            {
                return Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create instance of {type.FullName}. Ensure it has a parameterless constructor.", ex);
            }
        }

        // Try to resolve through DI first, then fallback to Activator for other types
        var service = _provider.GetService(type);
        if (service != null)
        {
            return service;
        }

        // Fallback to Activator for types not registered in DI
        try
        {
            return Activator.CreateInstance(type);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not resolve type '{type.FullName}'. Type is not registered in DI and could not be created via Activator.", ex);
        }
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
