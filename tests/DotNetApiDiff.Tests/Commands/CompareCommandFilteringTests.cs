// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Commands;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using DotNetApiDiff.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console.Cli;
using System.Reflection;
using Xunit;

namespace DotNetApiDiff.Tests.Commands;

public class CompareCommandFilteringTests
{
    private readonly Mock<IAssemblyLoader> _mockAssemblyLoader;
    private readonly Mock<IApiExtractor> _mockApiExtractor;
    private readonly Mock<IApiComparer> _mockApiComparer;
    private readonly Mock<IReportGenerator> _mockReportGenerator;
    private readonly Mock<ILogger<CompareCommand>> _mockLogger;
    private readonly ServiceProvider _serviceProvider;
    private readonly System.Reflection.Assembly _mockSourceAssembly;
    private readonly System.Reflection.Assembly _mockTargetAssembly;
    private readonly ComparisonResult _mockComparisonResult;
    private readonly CommandContext _commandContext;

    public CompareCommandFilteringTests()
    {
        _mockAssemblyLoader = new Mock<IAssemblyLoader>();
        _mockApiExtractor = new Mock<IApiExtractor>();
        _mockApiComparer = new Mock<IApiComparer>();
        _mockReportGenerator = new Mock<IReportGenerator>();
        _mockLogger = new Mock<ILogger<CompareCommand>>();

        // Create mock assemblies
        _mockSourceAssembly = typeof(CompareCommandFilteringTests).Assembly;
        _mockTargetAssembly = typeof(CompareCommandFilteringTests).Assembly;

        // Create a command context
        _commandContext = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Set up mock assembly loader
        _mockAssemblyLoader
            .Setup(m => m.LoadAssembly(It.IsAny<string>()))
            .Returns(_mockSourceAssembly);

        // Set up mock API extractor
        _mockApiExtractor
            .Setup(m => m.ExtractApiMembers(It.IsAny<System.Reflection.Assembly>(), It.IsAny<FilterConfiguration?>()))
            .Returns(new List<ApiMember>());

        // Set up mock comparison result
        _mockComparisonResult = new ComparisonResult
        {
            OldAssemblyPath = "source.dll",
            NewAssemblyPath = "target.dll",
            ComparisonTimestamp = DateTime.UtcNow
        };

        // Set up mock API comparer
        _mockApiComparer
            .Setup(m => m.CompareAssemblies(It.IsAny<System.Reflection.Assembly>(), It.IsAny<System.Reflection.Assembly>()))
            .Returns(_mockComparisonResult);

        // Set up mock report generator
        _mockReportGenerator
            .Setup(m => m.GenerateReport(It.IsAny<ComparisonResult>(), It.IsAny<ReportFormat>()))
            .Returns("Mock Report");

        // Set up service provider
        var services = new ServiceCollection();
        services.AddSingleton(_mockLogger.Object);
        services.AddSingleton(_mockAssemblyLoader.Object);
        services.AddSingleton(_mockApiExtractor.Object);
        services.AddSingleton(_mockApiComparer.Object);
        services.AddSingleton(_mockReportGenerator.Object);

        // Add ExitCodeManager
        services.AddSingleton<IExitCodeManager, DotNetApiDiff.ExitCodes.ExitCodeManager>();

        // Add GlobalExceptionHandler
        var mockExceptionHandler = new Mock<IGlobalExceptionHandler>();
        services.AddSingleton(mockExceptionHandler.Object);

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Execute_WithNamespaceFilters_AppliesFiltersToConfiguration()
    {
        // Arrange
        var command = new CompareCommand(_serviceProvider, _mockLogger.Object);
        var settings = new CompareCommandSettings
        {
            SourceAssemblyPath = "source.dll",
            TargetAssemblyPath = "target.dll",
            NamespaceFilters = new[] { "System.Text", "System.IO" }
        };

        // Act
        var result = command.Execute(_commandContext, settings);

        // Assert
        _mockApiExtractor.Verify(m => m.ExtractApiMembers(
            It.IsAny<System.Reflection.Assembly>(),
            It.Is<FilterConfiguration>(c =>
                c.IncludeNamespaces.Contains("System.Text") &&
                c.IncludeNamespaces.Contains("System.IO"))),
            Times.Exactly(2));

        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WithTypePatterns_AppliesFiltersToConfiguration()
    {
        // Arrange
        var command = new CompareCommand(_serviceProvider, _mockLogger.Object);
        var settings = new CompareCommandSettings
        {
            SourceAssemblyPath = "source.dll",
            TargetAssemblyPath = "target.dll",
            TypePatterns = new[] { "System.Text.*", "System.IO.File*" }
        };

        // Act
        var result = command.Execute(_commandContext, settings);

        // Assert
        _mockApiExtractor.Verify(m => m.ExtractApiMembers(
            It.IsAny<System.Reflection.Assembly>(),
            It.Is<FilterConfiguration>(c =>
                c.IncludeTypes.Contains("System.Text.*") &&
                c.IncludeTypes.Contains("System.IO.File*"))),
            Times.Exactly(2));

        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WithExcludePatterns_AppliesExclusionsToConfiguration()
    {
        // Arrange
        var command = new CompareCommand(_serviceProvider, _mockLogger.Object);
        var settings = new CompareCommandSettings
        {
            SourceAssemblyPath = "source.dll",
            TargetAssemblyPath = "target.dll",
            ExcludePatterns = new[] { "Internal", "System.Diagnostics.*" }
        };

        // Act
        var result = command.Execute(_commandContext, settings);

        // Assert
        _mockApiExtractor.Verify(m => m.ExtractApiMembers(
            It.IsAny<System.Reflection.Assembly>(),
            It.Is<FilterConfiguration>(c =>
                c.ExcludeNamespaces.Contains("Internal"))),
            Times.Exactly(2));

        // The System.Diagnostics.* pattern should be added to the excluded type patterns
        _mockApiComparer.Verify(m => m.CompareAssemblies(
            It.IsAny<System.Reflection.Assembly>(),
            It.IsAny<System.Reflection.Assembly>()),
            Times.Once);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WithIncludeInternals_AppliesOptionToConfiguration()
    {
        // Arrange
        var command = new CompareCommand(_serviceProvider, _mockLogger.Object);
        var settings = new CompareCommandSettings
        {
            SourceAssemblyPath = "source.dll",
            TargetAssemblyPath = "target.dll",
            IncludeInternals = true
        };

        // Act
        var result = command.Execute(_commandContext, settings);

        // Assert
        _mockApiExtractor.Verify(m => m.ExtractApiMembers(
            It.IsAny<System.Reflection.Assembly>(),
            It.Is<FilterConfiguration>(c => c.IncludeInternals == true)),
            Times.Exactly(2));

        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WithIncludeCompilerGenerated_AppliesOptionToConfiguration()
    {
        // Arrange
        var command = new CompareCommand(_serviceProvider, _mockLogger.Object);
        var settings = new CompareCommandSettings
        {
            SourceAssemblyPath = "source.dll",
            TargetAssemblyPath = "target.dll",
            IncludeCompilerGenerated = true
        };

        // Act
        var result = command.Execute(_commandContext, settings);

        // Assert
        _mockApiExtractor.Verify(m => m.ExtractApiMembers(
            It.IsAny<System.Reflection.Assembly>(),
            It.Is<FilterConfiguration>(c => c.IncludeCompilerGenerated == true)),
            Times.Exactly(2));

        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WithConfigFile_LoadsConfigurationFromFile()
    {
        // Arrange
        var tempConfigFile = Path.GetTempFileName();
        try
        {
            // Create a test configuration file
            var config = ComparisonConfiguration.CreateDefault();
            config.Filters.IncludeNamespaces.Add("TestNamespace");
            config.SaveToJsonFile(tempConfigFile);

            var command = new CompareCommand(_serviceProvider, _mockLogger.Object);
            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = "source.dll",
                TargetAssemblyPath = "target.dll",
                ConfigFile = tempConfigFile
            };

            // Act
            var result = command.Execute(_commandContext, settings);

            // Assert
            _mockApiExtractor.Verify(m => m.ExtractApiMembers(
                It.IsAny<System.Reflection.Assembly>(),
                It.Is<FilterConfiguration>(c => c.IncludeNamespaces.Contains("TestNamespace"))),
                Times.Exactly(2));

            Assert.Equal(0, result);
        }
        finally
        {
            // Clean up
            if (File.Exists(tempConfigFile))
            {
                File.Delete(tempConfigFile);
            }
        }
    }

    [Fact]
    public void Execute_WithInvalidConfigFile_ReturnsErrorCode()
    {
        // Arrange
        var tempConfigFile = Path.GetTempFileName();
        try
        {
            // Create an invalid JSON file
            File.WriteAllText(tempConfigFile, "{ invalid json }");

            var command = new CompareCommand(_serviceProvider, _mockLogger.Object);
            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = "source.dll",
                TargetAssemblyPath = "target.dll",
                ConfigFile = tempConfigFile
            };

            // Set up exit code manager to return 99 for configuration errors
            var mockExitCodeManager = new Mock<IExitCodeManager>();
            mockExitCodeManager.Setup(m => m.GetExitCodeForException(It.IsAny<Exception>()))
                .Returns(99);

            var services = new ServiceCollection();
            services.AddSingleton(mockExitCodeManager.Object);
            services.AddSingleton(_mockAssemblyLoader.Object);
            services.AddSingleton(_mockApiExtractor.Object);
            services.AddSingleton(_mockApiComparer.Object);
            services.AddSingleton(_mockReportGenerator.Object);
            services.AddSingleton(_mockLogger.Object);
            services.AddSingleton(Mock.Of<IGlobalExceptionHandler>());

            var serviceProvider = services.BuildServiceProvider();
            command = new CompareCommand(serviceProvider, _mockLogger.Object);

            // Act
            var result = command.Execute(_commandContext, settings);

            // Assert
            Assert.Equal(99, result); // Error exit code from our mock
        }
        finally
        {
            // Clean up
            if (File.Exists(tempConfigFile))
            {
                File.Delete(tempConfigFile);
            }
        }
    }

    [Fact]
    public void Execute_WithCombinedFilters_AppliesAllFiltersToConfiguration()
    {
        // Arrange
        var command = new CompareCommand(_serviceProvider, _mockLogger.Object);
        var settings = new CompareCommandSettings
        {
            SourceAssemblyPath = "source.dll",
            TargetAssemblyPath = "target.dll",
            NamespaceFilters = new[] { "System.Text" },
            TypePatterns = new[] { "System.IO.File*" },
            ExcludePatterns = new[] { "Internal" },
            IncludeInternals = true
        };

        // Act
        var result = command.Execute(_commandContext, settings);

        // Assert
        _mockApiExtractor.Verify(m => m.ExtractApiMembers(
            It.IsAny<System.Reflection.Assembly>(),
            It.Is<FilterConfiguration>(c =>
                c.IncludeNamespaces.Contains("System.Text") &&
                c.IncludeTypes.Contains("System.IO.File*") &&
                c.ExcludeNamespaces.Contains("Internal") &&
                c.IncludeInternals == true)),
            Times.Exactly(2));

        Assert.Equal(0, result);
    }
}
