// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models;
using DotNetApiDiff.Models.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Xunit;

namespace DotNetApiDiff.Tests.Models.Configuration;

public class ComparisonConfigurationTests
{
    [Fact]
    public void CreateDefault_ReturnsValidConfiguration()
    {
        // Act
        var config = ComparisonConfiguration.CreateDefault();

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Mappings);
        Assert.NotNull(config.Exclusions);
        Assert.NotNull(config.BreakingChangeRules);
        Assert.NotNull(config.Filters);
        Assert.True(config.IsValid());
    }

    [Fact]
    public void LoadFromJsonFile_WithValidFile_LoadsConfiguration()
    {
        // Arrange
        var testDirectory = Path.GetDirectoryName(typeof(ComparisonConfigurationTests).Assembly.Location);
        var configPath = Path.Combine(testDirectory!, "TestData", "sample-config.json");

        // Act
        var config = ComparisonConfiguration.LoadFromJsonFile(configPath);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(2, config.Filters.IncludeNamespaces.Count);
        Assert.Contains("System.Text", config.Filters.IncludeNamespaces);
        Assert.Contains("System.IO", config.Filters.IncludeNamespaces);

        Assert.Equal(2, config.Filters.ExcludeNamespaces.Count);
        Assert.Contains("System.Diagnostics", config.Filters.ExcludeNamespaces);

        Assert.Equal(2, config.Filters.IncludeTypes.Count);
        Assert.Contains("System.Text.*", config.Filters.IncludeTypes);

        Assert.Equal(2, config.Filters.ExcludeTypes.Count);
        Assert.Contains("*.Internal*", config.Filters.ExcludeTypes);

        Assert.False(config.Filters.IncludeInternals);
        Assert.False(config.Filters.IncludeCompilerGenerated);

        Assert.Equal(2, config.Mappings.NamespaceMappings.Count);
        Assert.Equal(2, config.Mappings.TypeMappings.Count);
        Assert.True(config.Mappings.AutoMapSameNameTypes);
        Assert.True(config.Mappings.IgnoreCase);

        Assert.Equal(2, config.Exclusions.ExcludedTypes.Count);
        Assert.Equal(2, config.Exclusions.ExcludedTypePatterns.Count);
        Assert.Equal(2, config.Exclusions.ExcludedMembers.Count);
        Assert.Equal(2, config.Exclusions.ExcludedMemberPatterns.Count);

        Assert.True(config.BreakingChangeRules.TreatTypeRemovalAsBreaking);
        Assert.True(config.BreakingChangeRules.TreatMemberRemovalAsBreaking);
        Assert.False(config.BreakingChangeRules.TreatAddedTypeAsBreaking);
        Assert.False(config.BreakingChangeRules.TreatAddedMemberAsBreaking);
        Assert.True(config.BreakingChangeRules.TreatSignatureChangeAsBreaking);

        Assert.Equal(ReportFormat.Console, config.OutputFormat);
        Assert.Null(config.OutputPath);
        Assert.True(config.FailOnBreakingChanges);
    }

    [Fact]
    public void LoadFromJsonFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var configPath = "non-existent-file.json";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => ComparisonConfiguration.LoadFromJsonFile(configPath));
    }

    [Fact]
    public void LoadFromJsonFile_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "{ invalid json }");

            // Act & Assert
            Assert.Throws<JsonException>(() => ComparisonConfiguration.LoadFromJsonFile(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void SaveToJsonFile_SerializesConfigurationCorrectly()
    {
        // Arrange
        var config = ComparisonConfiguration.CreateDefault();
        config.Filters.IncludeNamespaces.Add("TestNamespace");
        config.Filters.ExcludeTypes.Add("TestType");
        config.Filters.IncludeInternals = true;

        var tempFile = Path.GetTempFileName();
        try
        {
            // Act
            config.SaveToJsonFile(tempFile);
            var loadedConfig = ComparisonConfiguration.LoadFromJsonFile(tempFile);

            // Assert
            Assert.NotNull(loadedConfig);
            Assert.Contains("TestNamespace", loadedConfig.Filters.IncludeNamespaces);
            Assert.Contains("TestType", loadedConfig.Filters.ExcludeTypes);
            Assert.True(loadedConfig.Filters.IncludeInternals);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void IsValid_WithInvalidFilters_ReturnsFalse()
    {
        // Arrange
        var config = ComparisonConfiguration.CreateDefault();
        config.Filters.IncludeNamespaces.Add(string.Empty); // Invalid namespace

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithValidConfiguration_ReturnsTrue()
    {
        // Arrange
        var config = ComparisonConfiguration.CreateDefault();
        config.Filters.IncludeNamespaces.Add("System");
        config.Filters.ExcludeTypes.Add("System.Obsolete*");
        config.Filters.IncludeInternals = true;

        // Act & Assert
        Assert.True(config.IsValid());
    }
}
