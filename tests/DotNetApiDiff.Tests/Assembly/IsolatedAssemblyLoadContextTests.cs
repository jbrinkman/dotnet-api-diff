// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.Reflection;
using System.Runtime.Loader;
using DotNetApiDiff.AssemblyLoading;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests.Assembly;

public class IsolatedAssemblyLoadContextTests : IDisposable
{
    private readonly string _validAssemblyPath;
    private readonly string _invalidAssemblyPath;
    private readonly Mock<ILogger> _loggerMock;

    public IsolatedAssemblyLoadContextTests()
    {
        // Use the current test assembly as a valid assembly for testing
        _validAssemblyPath = typeof(IsolatedAssemblyLoadContextTests).Assembly.Location;

        // Create a path to an invalid assembly (use a text file)
        _invalidAssemblyPath = Path.GetTempFileName();
        File.WriteAllText(_invalidAssemblyPath, "This is not a valid assembly");

        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public void Constructor_WithValidPath_CreatesContext()
    {
        // Act
        var context = new IsolatedAssemblyLoadContext(_validAssemblyPath);

        // Assert
        Assert.NotNull(context);
    }

    [Fact]
    public void Constructor_WithLogger_CreatesContext()
    {
        // Act
        var context = new IsolatedAssemblyLoadContext(_validAssemblyPath, _loggerMock.Object);

        // Assert
        Assert.NotNull(context);
    }

    [Fact]
    public void LoadFromAssemblyPath_WithValidPath_LoadsAssembly()
    {
        // Arrange
        var context = new IsolatedAssemblyLoadContext(_validAssemblyPath);

        // Act
        var assembly = context.LoadFromAssemblyPath(_validAssemblyPath);

        // Assert
        Assert.NotNull(assembly);
        Assert.Equal(typeof(IsolatedAssemblyLoadContextTests).Assembly.GetName().Name, assembly.GetName().Name);
    }

    [Fact]
    public void LoadFromAssemblyPath_WithInvalidPath_ThrowsBadImageFormatException()
    {
        // Arrange
        var context = new IsolatedAssemblyLoadContext(_validAssemblyPath);

        // Act & Assert
        Assert.Throws<BadImageFormatException>(() => context.LoadFromAssemblyPath(_invalidAssemblyPath));
    }

    [Fact]
    public void AddSearchPath_WithValidPath_AddsPath()
    {
        // Arrange
        var context = new IsolatedAssemblyLoadContext(_validAssemblyPath);
        var searchPath = Path.GetDirectoryName(_validAssemblyPath);

        // Act
        context.AddSearchPath(searchPath);

        // Assert
        Assert.Contains(searchPath, context.AdditionalSearchPaths);
    }

    [Fact]
    public void AddSearchPath_WithInvalidPath_DoesNotAddPath()
    {
        // Arrange
        var context = new IsolatedAssemblyLoadContext(_validAssemblyPath);
        var initialCount = context.AdditionalSearchPaths.Count;

        // Act
        context.AddSearchPath(null);
        context.AddSearchPath(string.Empty);
        context.AddSearchPath("   ");
        context.AddSearchPath("C:\\NonExistentPath\\That\\Does\\Not\\Exist");

        // Assert
        Assert.Equal(initialCount, context.AdditionalSearchPaths.Count);
    }

    [Fact]
    public void AddSearchPath_WithDuplicatePath_AddsPathOnce()
    {
        // Arrange
        var context = new IsolatedAssemblyLoadContext(_validAssemblyPath);
        var searchPath = Path.GetDirectoryName(_validAssemblyPath);

        // Act
        context.AddSearchPath(searchPath);
        context.AddSearchPath(searchPath); // Add the same path again

        // Assert
        Assert.Single(context.AdditionalSearchPaths);
        Assert.Contains(searchPath, context.AdditionalSearchPaths);
    }

    [Fact]
    public void AddSearchPath_WithLogger_LogsDebugMessage()
    {
        // Arrange
        var context = new IsolatedAssemblyLoadContext(_validAssemblyPath, _loggerMock.Object);
        var searchPath = Path.GetDirectoryName(_validAssemblyPath);

        // Act
        context.AddSearchPath(searchPath);

        // Assert - Verify logger was called (simplified verification)
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Added search path")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void LoadFromAssemblyPath_WithNonExistentPath_ThrowsFileNotFoundException()
    {
        // Arrange
        var context = new IsolatedAssemblyLoadContext(_validAssemblyPath);
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "NonExistent.dll");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => context.LoadFromAssemblyPath(nonExistentPath));
    }

    [Fact]
    public void LoadFromAssemblyName_WithValidAssemblyName_LoadsAssembly()
    {
        // Arrange
        var context = new IsolatedAssemblyLoadContext(_validAssemblyPath);
        var currentAssembly = typeof(IsolatedAssemblyLoadContextTests).Assembly;
        var assemblyName = currentAssembly.GetName();

        // Copy the assembly to the test directory to simulate dependency resolution
        var testDir = Path.GetDirectoryName(_validAssemblyPath);
        var testAssemblyPath = Path.Combine(testDir, $"{assemblyName.Name}.dll");

        if (!File.Exists(testAssemblyPath))
        {
            File.Copy(currentAssembly.Location, testAssemblyPath, overwrite: true);
        }

        try
        {
            // Act
            var loadedAssembly = context.LoadFromAssemblyName(assemblyName);

            // Assert
            Assert.NotNull(loadedAssembly);
            Assert.Equal(assemblyName.Name, loadedAssembly.GetName().Name);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testAssemblyPath) && testAssemblyPath != currentAssembly.Location)
            {
                try { File.Delete(testAssemblyPath); } catch { /* ignore cleanup errors */ }
            }
        }
    }

    [Fact]
    public void LoadFromAssemblyName_WithInvalidAssemblyName_ThrowsFileNotFoundException()
    {
        // Arrange
        var context = new IsolatedAssemblyLoadContext(_validAssemblyPath);
        var invalidAssemblyName = new AssemblyName("NonExistentAssembly");

        // Act & Assert
        // When LoadFromAssemblyName calls the base implementation after our Load returns null,
        // it will throw FileNotFoundException for non-existent assemblies
        Assert.Throws<FileNotFoundException>(() => context.LoadFromAssemblyName(invalidAssemblyName));
    }

    [Fact]
    public void Context_WithLogger_LogsAssemblyResolutionAttempts()
    {
        // Arrange
        var context = new IsolatedAssemblyLoadContext(_validAssemblyPath, _loggerMock.Object);
        var invalidAssemblyName = new AssemblyName("NonExistentAssembly");

        // Act & Assert
        // This will throw FileNotFoundException, but we catch it to verify logging
        try
        {
            context.LoadFromAssemblyName(invalidAssemblyName);
        }
        catch (FileNotFoundException)
        {
            // Expected - ignore the exception, we just want to verify logging
        }

        // Assert - Verify debug logging occurred with the actual log message
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Attempting to resolve assembly")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Context_IsCollectible_ReturnsTrue()
    {
        // Arrange & Act
        var context = new IsolatedAssemblyLoadContext(_validAssemblyPath);

        // Assert
        Assert.True(context.IsCollectible);
    }

    public void Dispose()
    {
        // Clean up the temporary file
        if (File.Exists(_invalidAssemblyPath))
        {
            File.Delete(_invalidAssemblyPath);
        }
    }
}
