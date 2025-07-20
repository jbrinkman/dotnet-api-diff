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
        var command = new CompareCommand(Mock.Of<IServiceProvider>());
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary files for testing
        string oldAssemblyPath = Path.GetTempFileName();
        string newAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                OldAssemblyPath = oldAssemblyPath,
                NewAssemblyPath = newAssemblyPath,
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
            if (File.Exists(oldAssemblyPath))
                File.Delete(oldAssemblyPath);

            if (File.Exists(newAssemblyPath))
                File.Delete(newAssemblyPath);
        }
    }

    [Fact]
    public void Validate_WithInvalidOldAssemblyPath_ReturnsError()
    {
        // Arrange
        var command = new CompareCommand(Mock.Of<IServiceProvider>());
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary file for new assembly
        string newAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                OldAssemblyPath = "non_existent_file.dll",
                NewAssemblyPath = newAssemblyPath,
                OutputFormat = "console"
            };

            // Act
            var result = command.Validate(context, settings);

            // Assert
            Assert.False(result.Successful);
            Assert.Contains("Old assembly file not found", result.Message);
        }
        finally
        {
            // Clean up temporary file
            if (File.Exists(newAssemblyPath))
                File.Delete(newAssemblyPath);
        }
    }

    [Fact]
    public void Validate_WithInvalidNewAssemblyPath_ReturnsError()
    {
        // Arrange
        var command = new CompareCommand(Mock.Of<IServiceProvider>());
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary file for old assembly
        string oldAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                OldAssemblyPath = oldAssemblyPath,
                NewAssemblyPath = "non_existent_file.dll",
                OutputFormat = "console"
            };

            // Act
            var result = command.Validate(context, settings);

            // Assert
            Assert.False(result.Successful);
            Assert.Contains("New assembly file not found", result.Message);
        }
        finally
        {
            // Clean up temporary file
            if (File.Exists(oldAssemblyPath))
                File.Delete(oldAssemblyPath);
        }
    }

    [Fact]
    public void Validate_WithInvalidConfigFile_ReturnsError()
    {
        // Arrange
        var command = new CompareCommand(Mock.Of<IServiceProvider>());
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary files for assemblies
        string oldAssemblyPath = Path.GetTempFileName();
        string newAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                OldAssemblyPath = oldAssemblyPath,
                NewAssemblyPath = newAssemblyPath,
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
            if (File.Exists(oldAssemblyPath))
                File.Delete(oldAssemblyPath);

            if (File.Exists(newAssemblyPath))
                File.Delete(newAssemblyPath);
        }
    }

    [Fact]
    public void Validate_WithInvalidOutputFormat_ReturnsError()
    {
        // Arrange
        var command = new CompareCommand(Mock.Of<IServiceProvider>());
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary files for assemblies
        string oldAssemblyPath = Path.GetTempFileName();
        string newAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                OldAssemblyPath = oldAssemblyPath,
                NewAssemblyPath = newAssemblyPath,
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
            if (File.Exists(oldAssemblyPath))
                File.Delete(oldAssemblyPath);

            if (File.Exists(newAssemblyPath))
                File.Delete(newAssemblyPath);
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

        // Set up mock behavior
        var oldAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        var newAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        var oldApi = new List<ApiMember>();
        var newApi = new List<ApiMember>();
        var comparisonResult = new ComparisonResult
        {
            Differences = new List<ApiDifference>()
        };

        mockAssemblyLoader.Setup(al => al.LoadAssembly(It.IsAny<string>()))
            .Returns(oldAssembly);
        mockApiExtractor.Setup(ae => ae.ExtractApiMembers(It.IsAny<System.Reflection.Assembly>()))
            .Returns(oldApi);
        mockApiComparer.Setup(ac => ac.CompareAssemblies(It.IsAny<System.Reflection.Assembly>(), It.IsAny<System.Reflection.Assembly>()))
            .Returns(comparisonResult);
        mockReportGenerator.Setup(rg => rg.GenerateReport(It.IsAny<ComparisonResult>(), It.IsAny<ReportFormat>()))
            .Returns("Test Report");

        var command = new CompareCommand(mockServiceProvider.Object);
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary files for testing
        string oldAssemblyPath = Path.GetTempFileName();
        string newAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                OldAssemblyPath = oldAssemblyPath,
                NewAssemblyPath = newAssemblyPath,
                OutputFormat = "console"
            };

            // Act
            var result = command.Execute(context, settings);

            // Assert
            Assert.Equal(0, result);
            mockAssemblyLoader.Verify(al => al.LoadAssembly(oldAssemblyPath), Times.Once);
            mockAssemblyLoader.Verify(al => al.LoadAssembly(newAssemblyPath), Times.Once);
            mockApiExtractor.Verify(ae => ae.ExtractApiMembers(It.IsAny<System.Reflection.Assembly>()), Times.Exactly(2));
            mockApiComparer.Verify(ac => ac.CompareAssemblies(oldAssembly, newAssembly), Times.Once);
            mockReportGenerator.Verify(rg => rg.GenerateReport(comparisonResult, ReportFormat.Console), Times.Once);
        }
        finally
        {
            // Clean up temporary files
            if (File.Exists(oldAssemblyPath))
                File.Delete(oldAssemblyPath);

            if (File.Exists(newAssemblyPath))
                File.Delete(newAssemblyPath);
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

        // Set up mock behavior with breaking changes
        var oldAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        var newAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        var oldApi = new List<ApiMember>();
        var newApi = new List<ApiMember>();

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
            .Returns(oldAssembly);
        mockApiExtractor.Setup(ae => ae.ExtractApiMembers(It.IsAny<System.Reflection.Assembly>()))
            .Returns(oldApi);
        mockApiComparer.Setup(ac => ac.CompareAssemblies(It.IsAny<System.Reflection.Assembly>(), It.IsAny<System.Reflection.Assembly>()))
            .Returns(comparisonResult);
        mockReportGenerator.Setup(rg => rg.GenerateReport(It.IsAny<ComparisonResult>(), It.IsAny<ReportFormat>()))
            .Returns("Test Report");

        var command = new CompareCommand(mockServiceProvider.Object);
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary files for testing
        string oldAssemblyPath = Path.GetTempFileName();
        string newAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                OldAssemblyPath = oldAssemblyPath,
                NewAssemblyPath = newAssemblyPath,
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
            if (File.Exists(oldAssemblyPath))
                File.Delete(oldAssemblyPath);

            if (File.Exists(newAssemblyPath))
                File.Delete(newAssemblyPath);
        }
    }

    [Fact]
    public void Execute_WithException_ReturnsErrorExitCode()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<CompareCommand>>();
        var mockAssemblyLoader = new Mock<IAssemblyLoader>();

        // Set up mock services
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ILogger<CompareCommand>)))
            .Returns(mockLogger.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IAssemblyLoader)))
            .Returns(mockAssemblyLoader.Object);

        // Set up mock behavior to throw exception
        mockAssemblyLoader.Setup(al => al.LoadAssembly(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Test exception"));

        var command = new CompareCommand(mockServiceProvider.Object);
        var context = new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null);

        // Create temporary files for testing
        string oldAssemblyPath = Path.GetTempFileName();
        string newAssemblyPath = Path.GetTempFileName();

        try
        {
            var settings = new CompareCommandSettings
            {
                OldAssemblyPath = oldAssemblyPath,
                NewAssemblyPath = newAssemblyPath,
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
            if (File.Exists(oldAssemblyPath))
                File.Delete(oldAssemblyPath);

            if (File.Exists(newAssemblyPath))
                File.Delete(newAssemblyPath);
        }
    }
}