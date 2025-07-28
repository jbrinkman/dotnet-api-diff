// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotNetApiDiff.Tests.Integration;

/// <summary>
/// Tests to validate that all services in the DI container can be properly instantiated
/// </summary>
public class DependencyInjectionTests
{
    /// <summary>
    /// Creates a service provider using the same configuration as the main application
    /// </summary>
    /// <returns>Configured service provider</returns>
    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // Use the exact same service configuration as the main application
        DotNetApiDiff.Program.ConfigureServices(services);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void ServiceProvider_CanResolve_IAssemblyLoader()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IAssemblyLoader>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.AssemblyLoading.AssemblyLoader>(service);
    }

    [Fact]
    public void ServiceProvider_CanResolve_IApiExtractor()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IApiExtractor>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.ApiExtractor>(service);
    }

    [Fact]
    public void ServiceProvider_CanResolve_IMemberSignatureBuilder()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IMemberSignatureBuilder>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.MemberSignatureBuilder>(service);
    }

    [Fact]
    public void ServiceProvider_CanResolve_ITypeAnalyzer()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<ITypeAnalyzer>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.TypeAnalyzer>(service);
    }

    [Fact]
    public void ServiceProvider_CanResolve_IDifferenceCalculator()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IDifferenceCalculator>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.DifferenceCalculator>(service);
    }

    [Fact]
    public void ServiceProvider_CanResolve_INameMapper()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<INameMapper>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.NameMapper>(service);
    }

    [Fact]
    public void ServiceProvider_CanResolve_IChangeClassifier()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IChangeClassifier>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.ChangeClassifier>(service);
    }

    [Fact]
    public void ServiceProvider_CanResolve_IApiComparer()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IApiComparer>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ApiExtraction.ApiComparer>(service);
    }

    [Fact]
    public void ServiceProvider_CanResolve_IReportGenerator()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IReportGenerator>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.Reporting.ReportGenerator>(service);
    }

    [Fact]
    public void ServiceProvider_CanResolve_IExitCodeManager()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IExitCodeManager>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ExitCodes.ExitCodeManager>(service);
    }

    [Fact]
    public void ServiceProvider_CanResolve_IGlobalExceptionHandler()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider();

        // Act & Assert
        var service = serviceProvider.GetRequiredService<IGlobalExceptionHandler>();
        Assert.NotNull(service);
        Assert.IsType<DotNetApiDiff.ExitCodes.GlobalExceptionHandler>(service);
    }

    [Fact]
    public void ServiceProvider_CanResolve_AllServices_InSingleScope()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider();

        // Act & Assert - Try to resolve all services in a single scope to ensure no circular dependencies
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
        var exitCodeManager = scopedProvider.GetRequiredService<IExitCodeManager>();
        var globalExceptionHandler = scopedProvider.GetRequiredService<IGlobalExceptionHandler>();

        // Verify all services were created
        Assert.NotNull(assemblyLoader);
        Assert.NotNull(apiExtractor);
        Assert.NotNull(memberSignatureBuilder);
        Assert.NotNull(typeAnalyzer);
        Assert.NotNull(differenceCalculator);
        Assert.NotNull(nameMapper);
        Assert.NotNull(changeClassifier);
        Assert.NotNull(apiComparer);
        Assert.NotNull(reportGenerator);
        Assert.NotNull(exitCodeManager);
        Assert.NotNull(globalExceptionHandler);
    }

    [Fact]
    public void ServiceProvider_ValidatesDependencyChain_ForApiComparer()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider();

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
