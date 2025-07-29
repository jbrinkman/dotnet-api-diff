using System.Reflection;
using DotNetApiDiff.ExitCodes;
using DotNetApiDiff.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests;

public class ProgramTests
{
    [Fact]
    public void ConfigureServices_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Program.ConfigureServices(services);

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Verify required services are registered
        Assert.NotNull(serviceProvider.GetService<ILogger<Program>>());
        Assert.NotNull(serviceProvider.GetService<IExitCodeManager>());
        Assert.NotNull(serviceProvider.GetService<IGlobalExceptionHandler>());
    }

    [Fact]
    public void ConfigureServices_ConfiguresLoggingWithConsoleProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Program.ConfigureServices(services);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

        Assert.NotNull(loggerFactory);

        // Create a logger to verify it works
        var logger = loggerFactory.CreateLogger<ProgramTests>();
        Assert.NotNull(logger);
    }

    [Fact]
    public void ConfigureServices_RegistersExitCodeManagerAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Program.ConfigureServices(services);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var exitCodeManager1 = serviceProvider.GetService<IExitCodeManager>();
        var exitCodeManager2 = serviceProvider.GetService<IExitCodeManager>();

        Assert.NotNull(exitCodeManager1);
        Assert.NotNull(exitCodeManager2);
        Assert.Same(exitCodeManager1, exitCodeManager2); // Should be same instance (singleton)
    }

    [Fact]
    public void ConfigureServices_RegistersGlobalExceptionHandlerAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Program.ConfigureServices(services);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var handler1 = serviceProvider.GetService<IGlobalExceptionHandler>();
        var handler2 = serviceProvider.GetService<IGlobalExceptionHandler>();

        Assert.NotNull(handler1);
        Assert.NotNull(handler2);
        Assert.Same(handler1, handler2); // Should be same instance (singleton)
    }

    [Theory]
    [InlineData("Trace")]
    [InlineData("Debug")]
    [InlineData("Information")]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("Critical")]
    public void ConfigureServices_WithLogLevelEnvironmentVariable_SetsCorrectLogLevel(string logLevelString)
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("DOTNET_API_DIFF_LOG_LEVEL");

        try
        {
            Environment.SetEnvironmentVariable("DOTNET_API_DIFF_LOG_LEVEL", logLevelString);
            var services = new ServiceCollection();

            // Act
            Program.ConfigureServices(services);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            Assert.NotNull(loggerFactory);

            // Create a logger to verify it's configured
            var logger = loggerFactory.CreateLogger<ProgramTests>();
            Assert.NotNull(logger);

            // Verify the log level is enabled appropriately
            var expectedLogLevel = Enum.Parse<LogLevel>(logLevelString);
            Assert.Equal(expectedLogLevel <= LogLevel.Debug, logger.IsEnabled(LogLevel.Debug));
        }
        finally
        {
            // Restore original environment variable
            Environment.SetEnvironmentVariable("DOTNET_API_DIFF_LOG_LEVEL", originalValue);
        }
    }

    [Fact]
    public void ConfigureServices_WithInvalidLogLevelEnvironmentVariable_UsesDefaultLogLevel()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("DOTNET_API_DIFF_LOG_LEVEL");

        try
        {
            Environment.SetEnvironmentVariable("DOTNET_API_DIFF_LOG_LEVEL", "InvalidLogLevel");
            var services = new ServiceCollection();

            // Act
            Program.ConfigureServices(services);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            Assert.NotNull(loggerFactory);

            // Should fall back to Information level (default)
            var logger = loggerFactory.CreateLogger<ProgramTests>();
            Assert.True(logger.IsEnabled(LogLevel.Information));
        }
        finally
        {
            // Restore original environment variable
            Environment.SetEnvironmentVariable("DOTNET_API_DIFF_LOG_LEVEL", originalValue);
        }
    }

    [Fact]
    public void Main_WithValidArguments_ReturnsZero()
    {
        // Arrange
        var args = new[] { "compare", "--help" }; // Use help to avoid actual comparison

        // Act
        var result = Program.Main(args);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithInvalidArguments_ReturnsNonZero()
    {
        // Arrange
        var args = new[] { "invalid-command" };

        // Act
        var result = Program.Main(args);

        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Main_WithEmptyArguments_ShowsHelp()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = Program.Main(args);

        // Assert
        // Empty arguments show help and return 0
        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithIncompleteArguments_CompareOnly_ReturnsNonZero()
    {
        // Arrange - Missing required arguments
        var args = new[] { "compare" };

        // Act
        var result = Program.Main(args);

        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Main_WithIncompleteArguments_MissingTarget_ReturnsNonZero()
    {
        // Arrange - Missing target assembly
        var args = new[] { "compare", "nonexistent.dll" };

        // Act
        var result = Program.Main(args);

        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Main_ConfiguresSpectreConsoleApp()
    {
        // This test verifies that Main method sets up the Spectre.Console CommandApp correctly
        // We can't easily test the internal configuration, but we can verify it doesn't throw
        // and handles help commands correctly

        // Arrange
        var args = new[] { "--help" };

        // Act & Assert
        var result = Program.Main(args);

        // Should complete successfully and show help
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetBuildTime_WithValidAssembly_ReturnsFormattedDateTime()
    {
        // We need to use reflection to test the private GetBuildTime method
        var getBuildTimeMethod = typeof(Program).GetMethod("GetBuildTime",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(getBuildTimeMethod);

        // Act
        var result = getBuildTimeMethod.Invoke(null, null) as string;

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual("Unknown", result);

        // Should be in the format "yyyy-MM-dd HH:mm:ss"
        Assert.True(DateTime.TryParseExact(result, "yyyy-MM-dd HH:mm:ss",
            null, System.Globalization.DateTimeStyles.None, out _));
    }

    [Fact]
    public void Main_LogsApplicationStartInformation()
    {
        // This test verifies that the Main method logs startup information
        // Since we can't easily intercept the actual logging, we verify the method completes
        // without throwing exceptions when logging is involved

        // Arrange
        var args = new[] { "compare", "--help" };

        // Act & Assert
        var result = Program.Main(args);

        // Should complete successfully
        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_SetsUpGlobalExceptionHandling()
    {
        // This test verifies that global exception handling is set up
        // We test this by ensuring the method completes and returns expected results

        // Arrange
        var args = new[] { "--version" }; // A simple command that should work

        // Act
        var result = Program.Main(args);

        // Assert
        // Should handle the command gracefully (whether it succeeds or fails predictably)
        Assert.True(result >= 0); // Non-negative exit codes are expected
    }

    [Fact]
    public void Main_WithNullArgs_ShowsHelp()
    {
        // Act & Assert
        var result = Program.Main(null!);

        // Null arguments are treated as empty and show help
        Assert.Equal(0, result);
    }
    [Fact]
    public void ConfigureServices_WithEmptyServiceCollection_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Record.Exception(() => Program.ConfigureServices(services));

        Assert.Null(exception);
        Assert.True(services.Count > 0); // Should have added services
    }
}
