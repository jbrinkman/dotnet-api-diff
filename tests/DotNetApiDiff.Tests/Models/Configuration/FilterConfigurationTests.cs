// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models.Configuration;
using System.Text.Json;
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
    public void IsValid_WithValidConfiguration_ReturnsTrue()
    {
        // Arrange
        var config = new FilterConfiguration
        {
            IncludeNamespaces = new List<string> { "System", "Microsoft.Extensions" },
            ExcludeNamespaces = new List<string> { "System.Diagnostics" },
            IncludeTypes = new List<string> { "ILogger", "DbContext" },
            ExcludeTypes = new List<string> { "Debug", "Trace" }
        };

        // Act & Assert
        Assert.True(config.IsValid());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsValid_WithEmptyIncludeNamespace_ReturnsFalse(string? emptyValue)
    {
        // Arrange
        var config = new FilterConfiguration
        {
            IncludeNamespaces = new List<string> { "System", emptyValue! }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsValid_WithEmptyExcludeNamespace_ReturnsFalse(string? emptyValue)
    {
        // Arrange
        var config = new FilterConfiguration
        {
            ExcludeNamespaces = new List<string> { "System", emptyValue! }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsValid_WithEmptyIncludeType_ReturnsFalse(string? emptyValue)
    {
        // Arrange
        var config = new FilterConfiguration
        {
            IncludeTypes = new List<string> { "ILogger", emptyValue! }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsValid_WithEmptyExcludeType_ReturnsFalse(string? emptyValue)
    {
        // Arrange
        var config = new FilterConfiguration
        {
            ExcludeTypes = new List<string> { "Debug", emptyValue! }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void Serialization_RoundTrip_PreservesValues()
    {
        // Arrange
        var config = new FilterConfiguration
        {
            IncludeNamespaces = new List<string> { "System", "Microsoft.Extensions" },
            ExcludeNamespaces = new List<string> { "System.Diagnostics" },
            IncludeTypes = new List<string> { "ILogger", "DbContext" },
            ExcludeTypes = new List<string> { "Debug", "Trace" },
            IncludeInternals = true,
            IncludeCompilerGenerated = true
        };

        // Act
        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<FilterConfiguration>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(config.IncludeNamespaces.Count, deserialized!.IncludeNamespaces.Count);
        Assert.Equal(config.ExcludeNamespaces.Count, deserialized.ExcludeNamespaces.Count);
        Assert.Equal(config.IncludeTypes.Count, deserialized.IncludeTypes.Count);
        Assert.Equal(config.ExcludeTypes.Count, deserialized.ExcludeTypes.Count);
        Assert.Equal(config.IncludeInternals, deserialized.IncludeInternals);
        Assert.Equal(config.IncludeCompilerGenerated, deserialized.IncludeCompilerGenerated);

        Assert.All(config.IncludeNamespaces, ns => Assert.Contains(ns, deserialized.IncludeNamespaces));
        Assert.All(config.ExcludeNamespaces, ns => Assert.Contains(ns, deserialized.ExcludeNamespaces));
        Assert.All(config.IncludeTypes, t => Assert.Contains(t, deserialized.IncludeTypes));
        Assert.All(config.ExcludeTypes, t => Assert.Contains(t, deserialized.ExcludeTypes));
    }
}
