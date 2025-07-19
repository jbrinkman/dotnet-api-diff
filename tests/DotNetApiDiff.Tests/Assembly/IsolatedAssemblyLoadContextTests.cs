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
        context.AddSearchPath("");
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

    public void Dispose()
    {
        // Clean up the temporary file
        if (File.Exists(_invalidAssemblyPath))
        {
            File.Delete(_invalidAssemblyPath);
        }
    }
}
