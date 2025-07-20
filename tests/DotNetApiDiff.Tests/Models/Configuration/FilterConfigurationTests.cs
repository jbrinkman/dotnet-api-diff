// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models.Configuration;
using Xunit;

namespace DotNetApiDiff.Tests.Models.Configuration;

public class FilterConfigurationTests
{
    [Fact]
    public void CreateDefault_ReturnsValidConfiguration()
    {
        // Act
        var config = FilterConfiguration.CreateDefault();

        // Assert
        Assert.NotNull(config);
        Assert.Empty(config.IncludeNamespaces);
        Assert.Empty(config.ExcludeNamespaces);
        Assert.Empty(config.IncludeTypes);
        Assert.Empty(config.ExcludeTypes);
        Assert.False(config.IncludeInternals);
        Assert.False(config.IncludeCompilerGenerated);
        Assert.True(config.IsValid());
    }

    [Fact]
    public void IsValid_WithEmptyNamespace_ReturnsFalse()
    {
        // Arrange
        var config = FilterConfiguration.CreateDefault();
        config.IncludeNamespaces.Add(string.Empty);

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithWhitespaceNamespace_ReturnsFalse()
    {
        // Arrange
        var config = FilterConfiguration.CreateDefault();
        config.IncludeNamespaces.Add("   ");

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithEmptyTypePattern_ReturnsFalse()
    {
        // Arrange
        var config = FilterConfiguration.CreateDefault();
        config.IncludeTypes.Add(string.Empty);

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithValidPatterns_ReturnsTrue()
    {
        // Arrange
        var config = FilterConfiguration.CreateDefault();
        config.IncludeNamespaces.Add("System");
        config.ExcludeNamespaces.Add("System.Diagnostics");
        config.IncludeTypes.Add("System.Text.*");
        config.ExcludeTypes.Add("*.Internal*");

        // Act & Assert
        Assert.True(config.IsValid());
    }

    [Fact]
    public void IsValid_WithInternalAndCompilerGenerated_ReturnsTrue()
    {
        // Arrange
        var config = FilterConfiguration.CreateDefault();
        config.IncludeInternals = true;
        config.IncludeCompilerGenerated = true;

        // Act & Assert
        Assert.True(config.IsValid());
    }
}
