// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using Xunit;

namespace DotNetApiDiff.Tests.Commands;

public class TypeRegistrarTests
{
    [Fact]
    public void Constructor_WithServiceCollection_InitializesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var registrar = new TypeRegistrar(services);

        // Assert
        Assert.NotNull(registrar);
    }

    [Fact]
    public void Constructor_WithServiceProvider_InitializesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registrar = new TypeRegistrar(serviceProvider);

        // Assert
        Assert.NotNull(registrar);
    }

    [Fact]
    public void Register_WithType_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);

        // Act
        registrar.Register(typeof(ILogger<TypeRegistrar>), typeof(Logger<TypeRegistrar>));

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var resolver = registrar.Build();
        Assert.NotNull(resolver);
    }

    [Fact]
    public void RegisterInstance_WithInstance_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<TypeRegistrar>();

        // Act
        registrar.RegisterInstance(typeof(ILogger<TypeRegistrar>), logger);

        // Assert
        var resolver = registrar.Build();
        Assert.NotNull(resolver);
    }

    [Fact]
    public void RegisterLazy_WithFactory_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);

        // Act
        registrar.RegisterLazy(typeof(ILogger<TypeRegistrar>), () => 
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            return loggerFactory.CreateLogger<TypeRegistrar>();
        });

        // Assert
        var resolver = registrar.Build();
        Assert.NotNull(resolver);
    }

    [Fact]
    public void Build_ReturnsTypeResolver()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var registrar = new TypeRegistrar(services);

        // Act
        var resolver = registrar.Build();

        // Assert
        Assert.NotNull(resolver);
        Assert.IsAssignableFrom<ITypeResolver>(resolver);
    }
}
