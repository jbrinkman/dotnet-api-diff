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

public class CompareCommandTests
{
    [Fact]
    public void Validate_WithValidPaths_ReturnsSuccess()
    {
        // Arrange
        var command = new CompareCommand(Mock.Of<IServiceProvider>(), Mock.Of<ILogger<CompareCommand>>());
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary files for testing
        string sourceAssemblyPath = Path.GetTempFileName();
        string targetAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = sourceAssemblyPath,
                TargetAssemblyPath = targetAssemblyPath,
                OutputFormat = "console"
            };

            // Act
            var result = command.Validate(context, settings);

            // Assert
            Assert.True(result.Successful);
        }
        finally
        {
            // Clean up temporary files
            if (File.Exists(sourceAssemblyPath))
                File.Delete(sourceAssemblyPath);

            if (File.Exists(targetAssemblyPath))
                File.Delete(targetAssemblyPath);
        }
    }

    [Fact]
    public void Validate_WithInvalidSourceAssemblyPath_ReturnsError()
    {
        // Arrange
        var command = new CompareCommand(Mock.Of<IServiceProvider>(), Mock.Of<ILogger<CompareCommand>>());
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary file for target assembly
        string targetAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = "non_existent_file.dll",
                TargetAssemblyPath = targetAssemblyPath,
                OutputFormat = "console"
            };

            // Act
            var result = command.Validate(context, settings);

            // Assert
            Assert.False(result.Successful);
            Assert.Contains("Source assembly file not found", result.Message);
        }
        finally
        {
            // Clean up temporary file
            if (File.Exists(targetAssemblyPath))
                File.Delete(targetAssemblyPath);
        }
    }

    [Fact]
    public void Validate_WithInvalidTargetAssemblyPath_ReturnsError()
    {
        // Arrange
        var command = new CompareCommand(Mock.Of<IServiceProvider>(), Mock.Of<ILogger<CompareCommand>>());
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary file for source assembly
        string sourceAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = sourceAssemblyPath,
                TargetAssemblyPath = "non_existent_file.dll",
                OutputFormat = "console"
            };

            // Act
            var result = command.Validate(context, settings);

            // Assert
            Assert.False(result.Successful);
            Assert.Contains("Target assembly file not found", result.Message);
        }
        finally
        {
            // Clean up temporary file
            if (File.Exists(sourceAssemblyPath))
                File.Delete(sourceAssemblyPath);
        }
    }

    [Fact]
    public void Validate_WithInvalidConfigFile_ReturnsError()
    {
        // Arrange
        var command = new CompareCommand(Mock.Of<IServiceProvider>(), Mock.Of<ILogger<CompareCommand>>());
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary files for assemblies
        string sourceAssemblyPath = Path.GetTempFileName();
        string targetAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = sourceAssemblyPath,
                TargetAssemblyPath = targetAssemblyPath,
                ConfigFile = "non_existent_config.json",
                OutputFormat = "console"
            };

            // Act
            var result = command.Validate(context, settings);

            // Assert
            Assert.False(result.Successful);
            Assert.Contains("Configuration file not found", result.Message);
        }
        finally
        {
            // Clean up temporary files
            if (File.Exists(sourceAssemblyPath))
                File.Delete(sourceAssemblyPath);

            if (File.Exists(targetAssemblyPath))
                File.Delete(targetAssemblyPath);
        }
    }

    [Fact]
    public void Validate_WithInvalidOutputFormat_ReturnsError()
    {
        // Arrange
        var command = new CompareCommand(Mock.Of<IServiceProvider>(), Mock.Of<ILogger<CompareCommand>>());
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary files for assemblies
        string sourceAssemblyPath = Path.GetTempFileName();
        string targetAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = sourceAssemblyPath,
                TargetAssemblyPath = targetAssemblyPath,
                OutputFormat = "invalid"
            };

            // Act
            var result = command.Validate(context, settings);

            // Assert
            Assert.False(result.Successful);
            Assert.Contains("Invalid output format", result.Message);
        }
        finally
        {
            // Clean up temporary files
            if (File.Exists(sourceAssemblyPath))
                File.Delete(sourceAssemblyPath);

            if (File.Exists(targetAssemblyPath))
                File.Delete(targetAssemblyPath);
        }
    }

    [Fact]
    public void Execute_WithValidSettings_ReturnsSuccessExitCode()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<CompareCommand>>();
        var mockAssemblyLoader = new Mock<IAssemblyLoader>();
        var mockApiExtractor = new Mock<IApiExtractor>();
        var mockApiComparer = new Mock<IApiComparer>();
        var mockReportGenerator = new Mock<IReportGenerator>();
        var mockExitCodeManager = new Mock<IExitCodeManager>();
        var mockExceptionHandler = new Mock<IGlobalExceptionHandler>();

        // Set up mock services
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ILogger<CompareCommand>)))
            .Returns(mockLogger.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IAssemblyLoader)))
            .Returns(mockAssemblyLoader.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IApiExtractor)))
            .Returns(mockApiExtractor.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IApiComparer)))
            .Returns(mockApiComparer.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IReportGenerator)))
            .Returns(mockReportGenerator.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IExitCodeManager)))
            .Returns(mockExitCodeManager.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IGlobalExceptionHandler)))
            .Returns(mockExceptionHandler.Object);

        // Set up exit code manager behavior
        mockExitCodeManager.Setup(ec => ec.GetExitCode(It.IsAny<ComparisonResult>()))
            .Returns((ComparisonResult result) => result.HasBreakingChanges ? 1 : 0);
        mockExitCodeManager.Setup(ec => ec.GetExitCode(It.IsAny<ApiComparison>()))
            .Returns((ApiComparison comparison) => comparison.HasBreakingChanges ? 1 : 0);
        mockExitCodeManager.Setup(ec => ec.GetExitCodeDescription(0))
            .Returns("Success");

        // Set up mock behavior
        var sourceAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        var targetAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        var sourceApi = new List<ApiMember>();
        var targetApi = new List<ApiMember>();
        var comparisonResult = new ComparisonResult
        {
            Differences = new List<ApiDifference>()
        };

        mockAssemblyLoader.Setup(al => al.LoadAssembly(It.IsAny<string>()))
            .Returns(sourceAssembly);
        mockApiExtractor.Setup(ae => ae.ExtractApiMembers(It.IsAny<System.Reflection.Assembly>(), It.IsAny<DotNetApiDiff.Models.Configuration.FilterConfiguration?>()))
            .Returns(sourceApi);
        mockApiComparer.Setup(ac => ac.CompareAssemblies(It.IsAny<System.Reflection.Assembly>(), It.IsAny<System.Reflection.Assembly>()))
            .Returns(comparisonResult);
        mockReportGenerator.Setup(rg => rg.GenerateReport(It.IsAny<ComparisonResult>(), It.IsAny<ReportFormat>()))
            .Returns("Test Report");

        var command = new CompareCommand(mockServiceProvider.Object, mockLogger.Object);
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary files for testing
        string sourceAssemblyPath = Path.GetTempFileName();
        string targetAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = sourceAssemblyPath,
                TargetAssemblyPath = targetAssemblyPath,
                OutputFormat = "console"
            };

            // Act
            var result = command.Execute(context, settings);

            // Assert
            Assert.Equal(0, result);
            mockAssemblyLoader.Verify(al => al.LoadAssembly(sourceAssemblyPath), Times.Once);
            mockAssemblyLoader.Verify(al => al.LoadAssembly(targetAssemblyPath), Times.Once);
            const int EXPECTED_API_EXTRACTION_CALLS = 2; // Number of assemblies processed: source and target
            mockApiExtractor.Verify(ae => ae.ExtractApiMembers(It.IsAny<System.Reflection.Assembly>(), It.IsAny<DotNetApiDiff.Models.Configuration.FilterConfiguration?>()), Times.Exactly(EXPECTED_API_EXTRACTION_CALLS));
            mockApiComparer.Verify(ac => ac.CompareAssemblies(sourceAssembly, targetAssembly), Times.Once);
            mockReportGenerator.Verify(rg => rg.GenerateReport(comparisonResult, ReportFormat.Console), Times.Once);
        }
        finally
        {
            // Clean up temporary files
            if (File.Exists(sourceAssemblyPath))
                File.Delete(sourceAssemblyPath);

            if (File.Exists(targetAssemblyPath))
                File.Delete(targetAssemblyPath);
        }
    }

    [Fact]
    public void Execute_WithBreakingChanges_ReturnsNonZeroExitCode()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<CompareCommand>>();
        var mockAssemblyLoader = new Mock<IAssemblyLoader>();
        var mockApiExtractor = new Mock<IApiExtractor>();
        var mockApiComparer = new Mock<IApiComparer>();
        var mockReportGenerator = new Mock<IReportGenerator>();
        var mockExitCodeManager = new Mock<IExitCodeManager>();
        var mockExceptionHandler = new Mock<IGlobalExceptionHandler>();

        // Set up mock services
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ILogger<CompareCommand>)))
            .Returns(mockLogger.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IAssemblyLoader)))
            .Returns(mockAssemblyLoader.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IApiExtractor)))
            .Returns(mockApiExtractor.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IApiComparer)))
            .Returns(mockApiComparer.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IReportGenerator)))
            .Returns(mockReportGenerator.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IExitCodeManager)))
            .Returns(mockExitCodeManager.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IGlobalExceptionHandler)))
            .Returns(mockExceptionHandler.Object);

        // Set up exit code manager behavior for breaking changes
        mockExitCodeManager.Setup(ec => ec.GetExitCode(It.IsAny<ComparisonResult>()))
            .Returns((ComparisonResult result) => result.HasBreakingChanges ? 1 : 0);
        mockExitCodeManager.Setup(ec => ec.GetExitCode(It.IsAny<ApiComparison>()))
            .Returns((ApiComparison comparison) => comparison.HasBreakingChanges ? 1 : 0);
        mockExitCodeManager.Setup(ec => ec.GetExitCodeDescription(1))
            .Returns("Breaking changes detected");
        mockExitCodeManager.Setup(ec => ec.GetExitCodeDescription(0))
            .Returns("Success");

        // Set up mock behavior with breaking changes
        var sourceAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        var targetAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        var sourceApi = new List<ApiMember>();
        var targetApi = new List<ApiMember>();

        // Create a comparison result with breaking changes
        var comparisonResult = new ComparisonResult
        {
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Removed,
                    IsBreakingChange = true,
                    ElementName = "TestMethod"
                }
            }
        };

        mockAssemblyLoader.Setup(al => al.LoadAssembly(It.IsAny<string>()))
            .Returns(sourceAssembly);
        mockApiExtractor.Setup(ae => ae.ExtractApiMembers(It.IsAny<System.Reflection.Assembly>(), It.IsAny<DotNetApiDiff.Models.Configuration.FilterConfiguration?>()))
            .Returns(sourceApi);
        mockApiComparer.Setup(ac => ac.CompareAssemblies(It.IsAny<System.Reflection.Assembly>(), It.IsAny<System.Reflection.Assembly>()))
            .Returns(comparisonResult);
        mockReportGenerator.Setup(rg => rg.GenerateReport(It.IsAny<ComparisonResult>(), It.IsAny<ReportFormat>()))
            .Returns("Test Report");

        var command = new CompareCommand(mockServiceProvider.Object, mockLogger.Object);
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary files for testing
        string sourceAssemblyPath = Path.GetTempFileName();
        string targetAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = sourceAssemblyPath,
                TargetAssemblyPath = targetAssemblyPath,
                OutputFormat = "console"
            };

            // Act
            var result = command.Execute(context, settings);

            // Assert
            Assert.Equal(1, result); // Non-zero exit code for breaking changes
        }
        finally
        {
            // Clean up temporary files
            if (File.Exists(sourceAssemblyPath))
                File.Delete(sourceAssemblyPath);

            if (File.Exists(targetAssemblyPath))
                File.Delete(targetAssemblyPath);
        }
    }

    [Fact]
    public void Execute_WithException_ReturnsErrorExitCode()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<CompareCommand>>();
        var mockAssemblyLoader = new Mock<IAssemblyLoader>();
        var mockExitCodeManager = new Mock<IExitCodeManager>();
        var mockExceptionHandler = new Mock<IGlobalExceptionHandler>();

        // Set up mock services
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ILogger<CompareCommand>)))
            .Returns(mockLogger.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IAssemblyLoader)))
            .Returns(mockAssemblyLoader.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IExitCodeManager)))
            .Returns(mockExitCodeManager.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IGlobalExceptionHandler)))
            .Returns(mockExceptionHandler.Object);

        // Set up exit code manager behavior for exceptions
        mockExitCodeManager.Setup(ec => ec.GetExitCodeForException(It.IsAny<Exception>()))
            .Returns(2); // Error exit code
        mockExitCodeManager.Setup(ec => ec.GetExitCodeDescription(2))
            .Returns("Error occurred");

        // Set up mock behavior to throw exception
        mockAssemblyLoader.Setup(al => al.LoadAssembly(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Test exception"));

        var command = new CompareCommand(mockServiceProvider.Object, mockLogger.Object);
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary files for testing
        string sourceAssemblyPath = Path.GetTempFileName();
        string targetAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = sourceAssemblyPath,
                TargetAssemblyPath = targetAssemblyPath,
                OutputFormat = "console"
            };

            // Act
            var result = command.Execute(context, settings);

            // Assert
            Assert.Equal(2, result); // Error exit code
        }
        finally
        {
            // Clean up temporary files
            if (File.Exists(sourceAssemblyPath))
                File.Delete(sourceAssemblyPath);

            if (File.Exists(targetAssemblyPath))
                File.Delete(targetAssemblyPath);
        }
    }
}
