// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotNetApiDiff.Tests.Integration;

/// <summary>
/// Tests to validate that the root DI container can properly instantiate command services,
/// and that command-specific containers work with business logic services
/// </summary>
public class DependencyInjectionTests
{
    /// <summary>
    /// Creates a service provider using the same configuration as the main application root container
    /// </summary>
    /// <returns>Configured service provider</returns>
    private static ServiceProvider CreateRootServiceProvider()
    {
        var services = new ServiceCollection();

        // Use the exact same service configuration as the main application
        DotNetApiDiff.Program.ConfigureServices(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a command-specific service provider with business logic services and test configuration
    /// </summary>
    /// <returns>Configured command-specific service provider</returns>
    private static ServiceProvider CreateCommandServiceProvider()
    {
        // Create command-specific container with its own logging
        var commandServices = new ServiceCollection();

        // Add logging directly
        commandServices.AddLogging();

        // Add test configuration
        var config = ComparisonConfiguration.CreateDefault();
        commandServices.AddSingleton(config);

        // Add all business logic services
        commandServices.AddScoped<IAssemblyLoader, DotNetApiDiff.AssemblyLoading.AssemblyLoader>();
        commandServices.AddScoped<IApiExtractor, DotNetApiDiff.ApiExtraction.ApiExtractor>();
        commandServices.AddScoped<IMemberSignatureBuilder, DotNetApiDiff.ApiExtraction.MemberSignatureBuilder>();
        commandServices.AddScoped<ITypeAnalyzer, DotNetApiDiff.ApiExtraction.TypeAnalyzer>();
        commandServices.AddScoped<IDifferenceCalculator, DotNetApiDiff.ApiExtraction.DifferenceCalculator>();
        commandServices.AddScoped<IReportGenerator, DotNetApiDiff.Reporting.ReportGenerator>();

        // Add configuration-specific services
        commandServices.AddScoped<INameMapper>(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            return new DotNetApiDiff.ApiExtraction.NameMapper(config.Mappings, loggerFactory.CreateLogger<DotNetApiDiff.ApiExtraction.NameMapper>());
        });

        commandServices.AddScoped<IChangeClassifier>(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            return new DotNetApiDiff.ApiExtraction.ChangeClassifier(config.BreakingChangeRules, config.Exclusions,
                loggerFactory.CreateLogger<DotNetApiDiff.ApiExtraction.ChangeClassifier>());
        });

        commandServices.AddScoped<IApiComparer, DotNetApiDiff.ApiExtraction.ApiComparer>();

        return commandServices.BuildServiceProvider();
    }

    [Fact]
    public void RootServiceProvider_CanResolve_IExitCodeManager()
    {
        // Arrange
        using var serviceProvider = CreateRootServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IExitCodeManager>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ExitCodes.ExitCodeManager>(service);
    }

    [Fact]
    public void RootServiceProvider_CanResolve_IGlobalExceptionHandler()
    {
        // Arrange
        using var serviceProvider = CreateRootServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IGlobalExceptionHandler>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ExitCodes.GlobalExceptionHandler>(service);
    }

    [Fact]
    public void RootServiceProvider_CanResolve_ILoggerFactory()
    {
        // Arrange
        using var serviceProvider = CreateRootServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<ILoggerFactory>();
        Assert.NotNull(service);
    }

    [Fact]
    public void CommandServiceProvider_CanResolve_IAssemblyLoader()
    {
        // Arrange
        using var serviceProvider = CreateCommandServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IAssemblyLoader>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.AssemblyLoading.AssemblyLoader>(service);
    }

    [Fact]
    public void CommandServiceProvider_CanResolve_IApiExtractor()
    {
        // Arrange
        using var serviceProvider = CreateCommandServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IApiExtractor>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.ApiExtractor>(service);
    }

    [Fact]
    public void CommandServiceProvider_CanResolve_IMemberSignatureBuilder()
    {
        // Arrange
        using var serviceProvider = CreateCommandServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IMemberSignatureBuilder>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.MemberSignatureBuilder>(service);
    }

    [Fact]
    public void CommandServiceProvider_CanResolve_ITypeAnalyzer()
    {
        // Arrange
        using var serviceProvider = CreateCommandServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<ITypeAnalyzer>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.TypeAnalyzer>(service);
    }

    [Fact]
    public void CommandServiceProvider_CanResolve_IDifferenceCalculator()
    {
        // Arrange
        using var serviceProvider = CreateCommandServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IDifferenceCalculator>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.DifferenceCalculator>(service);
    }

    [Fact]
    public void CommandServiceProvider_CanResolve_INameMapper()
    {
        // Arrange
        using var serviceProvider = CreateCommandServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<INameMapper>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.NameMapper>(service);
    }

    [Fact]
    public void CommandServiceProvider_CanResolve_IChangeClassifier()
    {
        // Arrange
        using var serviceProvider = CreateCommandServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IChangeClassifier>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.ChangeClassifier>(service);
    }

    [Fact]
    public void CommandServiceProvider_CanResolve_IApiComparer()
    {
        // Arrange
        using var serviceProvider = CreateCommandServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IApiComparer>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.ApiComparer>(service);
    }

    [Fact]
    public void CommandServiceProvider_CanResolve_IReportGenerator()
    {
        // Arrange
        using var serviceProvider = CreateCommandServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IReportGenerator>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.Reporting.ReportGenerator>(service);
    }

    [Fact]
    public void CommandServiceProvider_CanResolve_AllBusinessLogicServices_InSingleScope()
    {
        // Arrange
        using var serviceProvider = CreateCommandServiceProvider();

        // Act & Assert - Try to resolve all business logic services in a single scope to ensure no circular dependencies
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var assemblyLoader = scopedProvider.GetRequiredService<IAssemblyLoader>();
        var apiExtractor = scopedProvider.GetRequiredService<IApiExtractor>();
        var memberSignatureBuilder = scopedProvider.GetRequiredService<IMemberSignatureBuilder>();
        var typeAnalyzer = scopedProvider.GetRequiredService<ITypeAnalyzer>();
        var differenceCalculator = scopedProvider.GetRequiredService<IDifferenceCalculator>();
        var nameMapper = scopedProvider.GetRequiredService<INameMapper>();
        var changeClassifier = scopedProvider.GetRequiredService<IChangeClassifier>();
        var apiComparer = scopedProvider.GetRequiredService<IApiComparer>();
        var reportGenerator = scopedProvider.GetRequiredService<IReportGenerator>();

        // Verify all business logic services were created
        Assert.NotNull(assemblyLoader);
        Assert.NotNull(apiExtractor);
        Assert.NotNull(memberSignatureBuilder);
        Assert.NotNull(typeAnalyzer);
        Assert.NotNull(differenceCalculator);
        Assert.NotNull(nameMapper);
        Assert.NotNull(changeClassifier);
        Assert.NotNull(apiComparer);
        Assert.NotNull(reportGenerator);
    }

    [Fact]
    public void RootServiceProvider_CanResolve_AllInfrastructureServices_InSingleScope()
    {
        // Arrange
        using var serviceProvider = CreateRootServiceProvider();

        // Act & Assert - Try to resolve all infrastructure services in a single scope
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var exitCodeManager = scopedProvider.GetRequiredService<IExitCodeManager>();
        var globalExceptionHandler = scopedProvider.GetRequiredService<IGlobalExceptionHandler>();

        // Verify all infrastructure services were created
        Assert.NotNull(exitCodeManager);
        Assert.NotNull(globalExceptionHandler);
    }

    [Fact]
    public void CommandServiceProvider_ValidatesDependencyChain_ForApiComparer()
    {
        // Arrange
        using var serviceProvider = CreateCommandServiceProvider();

        // Act - Get ApiComparer which depends on many other services
        var apiComparer = serviceProvider.GetRequiredService<IApiComparer>();

        // Assert
        Assert.NotNull(apiComparer);

        // Verify the dependency chain by checking that we can create multiple instances
        var apiComparer2 = serviceProvider.GetRequiredService<IApiComparer>();
        Assert.NotNull(apiComparer2);

        // Since they're scoped, they should be different instances in different scopes
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        var scopedComparer1 = scope1.ServiceProvider.GetRequiredService<IApiComparer>();
        var scopedComparer2 = scope2.ServiceProvider.GetRequiredService<IApiComparer>();

        Assert.NotNull(scopedComparer1);
        Assert.NotNull(scopedComparer2);
        Assert.NotSame(scopedComparer1, scopedComparer2);
    }
}
