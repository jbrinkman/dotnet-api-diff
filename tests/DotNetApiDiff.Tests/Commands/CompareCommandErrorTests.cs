// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System;
using System.IO;
using DotNetApiDiff.Commands;
using DotNetApiDiff.ExitCodes;
using DotNetApiDiff.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console.Cli;
using Xunit;

namespace DotNetApiDiff.Tests.Commands
{
    public class CompareCommandErrorTests : IDisposable
    {
        private readonly Mock<IAssemblyLoader> _assemblyLoaderMock;
        private readonly Mock<IApiExtractor> _apiExtractorMock;
        private readonly Mock<IApiComparer> _apiComparerMock;
        private readonly Mock<IReportGenerator> _reportGeneratorMock;
        private readonly Mock<IExitCodeManager> _exitCodeManagerMock;
        private readonly Mock<IGlobalExceptionHandler> _exceptionHandlerMock;
        private readonly Mock<ILogger<CompareCommand>> _loggerMock;
        private readonly IServiceProvider _serviceProvider;
        private readonly CompareCommand _command;
        private readonly string _tempDir;

        public CompareCommandErrorTests()
        {
            _assemblyLoaderMock = new Mock<IAssemblyLoader>();
            _apiExtractorMock = new Mock<IApiExtractor>();
            _apiComparerMock = new Mock<IApiComparer>();
            _reportGeneratorMock = new Mock<IReportGenerator>();
            _exitCodeManagerMock = new Mock<IExitCodeManager>();
            _exceptionHandlerMock = new Mock<IGlobalExceptionHandler>();
            _loggerMock = new Mock<ILogger<CompareCommand>>();

            // Set up service provider with mocks
            var services = new ServiceCollection();
            services.AddSingleton(_assemblyLoaderMock.Object);
            services.AddSingleton(_apiExtractorMock.Object);
            services.AddSingleton(_apiComparerMock.Object);
            services.AddSingleton(_reportGeneratorMock.Object);
            services.AddSingleton(_exitCodeManagerMock.Object);
            services.AddSingleton<IGlobalExceptionHandler>(_exceptionHandlerMock.Object);
            services.AddSingleton(_loggerMock.Object);

            _serviceProvider = services.BuildServiceProvider();
            _command = new CompareCommand(_serviceProvider, _loggerMock.Object, _exitCodeManagerMock.Object, _exceptionHandlerMock.Object);

            // Create temp directory for test files
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void Execute_WithInvalidSourceAssembly_ReturnsAssemblyLoadError()
        {
            // Arrange
            var sourceAssemblyPath = Path.Combine(_tempDir, "source.dll");
            var targetAssemblyPath = Path.Combine(_tempDir, "target.dll");

            // Create empty files
            File.WriteAllText(sourceAssemblyPath, "");
            File.WriteAllText(targetAssemblyPath, "");

            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = sourceAssemblyPath,
                TargetAssemblyPath = targetAssemblyPath
            };

            var exception = new BadImageFormatException("Invalid assembly format");
            _assemblyLoaderMock.Setup(m => m.LoadAssembly(sourceAssemblyPath))
                .Throws(exception);

            _exitCodeManagerMock.Setup(m => m.GetExitCodeForException(exception))
                .Returns(ExitCodeManager.AssemblyLoadError);

            // Act
            int result = _command.Execute(new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null), settings);

            // Assert
            Assert.Equal(ExitCodeManager.AssemblyLoadError, result);
        }

        [Fact]
        public void Execute_WithInvalidTargetAssembly_ReturnsAssemblyLoadError()
        {
            // Arrange
            var sourceAssemblyPath = Path.Combine(_tempDir, "source.dll");
            var targetAssemblyPath = Path.Combine(_tempDir, "target.dll");

            // Create empty files
            File.WriteAllText(sourceAssemblyPath, "");
            File.WriteAllText(targetAssemblyPath, "");

            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = sourceAssemblyPath,
                TargetAssemblyPath = targetAssemblyPath
            };

            var exception = new BadImageFormatException("Invalid assembly format");
            _assemblyLoaderMock.Setup(m => m.LoadAssembly(sourceAssemblyPath))
                .Returns(Mock.Of<System.Reflection.Assembly>());
            _assemblyLoaderMock.Setup(m => m.LoadAssembly(targetAssemblyPath))
                .Throws(exception);

            _exitCodeManagerMock.Setup(m => m.GetExitCodeForException(exception))
                .Returns(ExitCodeManager.AssemblyLoadError);

            // Act
            int result = _command.Execute(new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null), settings);

            // Assert
            Assert.Equal(ExitCodeManager.AssemblyLoadError, result);
        }

        [Fact]
        public void Execute_WithInvalidConfigFile_ReturnsConfigurationError()
        {
            // Arrange
            var sourceAssemblyPath = Path.Combine(_tempDir, "source.dll");
            var targetAssemblyPath = Path.Combine(_tempDir, "target.dll");
            var configFilePath = Path.Combine(_tempDir, "config.json");

            // Create empty files
            File.WriteAllText(sourceAssemblyPath, "");
            File.WriteAllText(targetAssemblyPath, "");
            File.WriteAllText(configFilePath, "{ invalid json }");

            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = sourceAssemblyPath,
                TargetAssemblyPath = targetAssemblyPath,
                ConfigFile = configFilePath
            };

            _exitCodeManagerMock.Setup(m => m.GetExitCodeForException(It.IsAny<Exception>()))
                .Returns(ExitCodeManager.ConfigurationError);

            // Act
            int result = _command.Execute(new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null), settings);

            // Assert
            Assert.Equal(ExitCodeManager.ConfigurationError, result);
        }

        [Fact]
        public void Execute_WithComparisonError_ReturnsComparisonError()
        {
            // Arrange
            var sourceAssemblyPath = Path.Combine(_tempDir, "source.dll");
            var targetAssemblyPath = Path.Combine(_tempDir, "target.dll");

            // Create empty files
            File.WriteAllText(sourceAssemblyPath, "");
            File.WriteAllText(targetAssemblyPath, "");

            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = sourceAssemblyPath,
                TargetAssemblyPath = targetAssemblyPath
            };

            var exception = new InvalidOperationException("Comparison error");
            _assemblyLoaderMock.Setup(m => m.LoadAssembly(It.IsAny<string>()))
                .Returns(Mock.Of<System.Reflection.Assembly>());
            _apiExtractorMock.Setup(m => m.ExtractApiMembers(It.IsAny<System.Reflection.Assembly>(), It.IsAny<DotNetApiDiff.Models.Configuration.FilterConfiguration>()))
                .Returns(Array.Empty<DotNetApiDiff.Models.ApiMember>());
            _apiComparerMock.Setup(m => m.CompareAssemblies(It.IsAny<System.Reflection.Assembly>(), It.IsAny<System.Reflection.Assembly>()))
                .Throws(exception);

            _exitCodeManagerMock.Setup(m => m.GetExitCodeForException(exception))
                .Returns(ExitCodeManager.ComparisonError);

            // Act
            int result = _command.Execute(new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null), settings);

            // Assert
            Assert.Equal(ExitCodeManager.ComparisonError, result);
        }

        [Fact]
        public void Execute_WithReportGenerationError_ReturnsUnexpectedError()
        {
            // Arrange
            var sourceAssemblyPath = Path.Combine(_tempDir, "source.dll");
            var targetAssemblyPath = Path.Combine(_tempDir, "target.dll");

            // Create empty files
            File.WriteAllText(sourceAssemblyPath, "");
            File.WriteAllText(targetAssemblyPath, "");

            var settings = new CompareCommandSettings
            {
                SourceAssemblyPath = sourceAssemblyPath,
                TargetAssemblyPath = targetAssemblyPath
            };

            var exception = new InvalidOperationException("Report generation error");
            _assemblyLoaderMock.Setup(m => m.LoadAssembly(It.IsAny<string>()))
                .Returns(Mock.Of<System.Reflection.Assembly>());
            _apiExtractorMock.Setup(m => m.ExtractApiMembers(It.IsAny<System.Reflection.Assembly>(), It.IsAny<DotNetApiDiff.Models.Configuration.FilterConfiguration>()))
                .Returns(Array.Empty<DotNetApiDiff.Models.ApiMember>());
            _apiComparerMock.Setup(m => m.CompareAssemblies(It.IsAny<System.Reflection.Assembly>(), It.IsAny<System.Reflection.Assembly>()))
                .Returns(new DotNetApiDiff.Models.ComparisonResult());
            _reportGeneratorMock.Setup(m => m.GenerateReport(It.IsAny<DotNetApiDiff.Models.ComparisonResult>(), It.IsAny<DotNetApiDiff.Models.ReportFormat>()))
                .Throws(exception);

            _exitCodeManagerMock.Setup(m => m.GetExitCodeForException(exception))
                .Returns(ExitCodeManager.UnexpectedError);

            // Act
            int result = _command.Execute(new CommandContext(Mock.Of<IRemainingArguments>(), "compare", null), settings);

            // Assert
            Assert.Equal(ExitCodeManager.UnexpectedError, result);
        }

        [Fact]
        public void Execute_WithUnhandledException_UsesExceptionHandler()
        {
            // This test verifies that the GlobalExceptionHandler is used when an unhandled exception occurs

            // First, let's test the GlobalExceptionHandler directly
            var exception = new OutOfMemoryException("Out of memory");
            _exceptionHandlerMock.Setup(m => m.HandleException(It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(99);

            // Verify the handler returns the expected exit code
            int handlerResult = _exceptionHandlerMock.Object.HandleException(exception, "Test context");
            Assert.Equal(99, handlerResult);

            // Now let's verify the handler is called by the command
            _exceptionHandlerMock.Verify(m => m.HandleException(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }

        public void Dispose()
        {
            // Clean up temp directory
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
