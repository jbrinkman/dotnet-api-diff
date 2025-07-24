// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models.Configuration;
using DotNetApiDiff.Models;
using System.IO;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace DotNetApiDiff.Tests.Integration;

/// <summary>
/// Integration tests for configuration file workflows
/// </summary>
public class ConfigurationWorkflowTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDataPath;
    private readonly string _tempConfigPath;

    public ConfigurationWorkflowTests(ITestOutputHelper output)
    {
        _output = output;
        _testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
        _tempConfigPath = Path.Combine(Path.GetTempPath(), "DotNetApiDiff.ConfigTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempConfigPath);
    }

    [Fact]
    public void LoadConfiguration_WithValidStrictConfig_ShouldLoadCorrectly()
    {
        // Arrange
        var configPath = Path.Combine(_testDataPath, "config-strict-breaking-changes.json");

        // Act
        var config = ComparisonConfiguration.LoadFromJsonFile(configPath);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.IsValid());
        Assert.True(config.BreakingChangeRules.TreatTypeRemovalAsBreaking);
        Assert.True(config.BreakingChangeRules.TreatMemberRemovalAsBreaking);
        Assert.True(config.BreakingChangeRules.TreatSignatureChangeAsBreaking);
        Assert.False(config.BreakingChangeRules.TreatAddedTypeAsBreaking);
        Assert.False(config.BreakingChangeRules.TreatAddedMemberAsBreaking);
        Assert.True(config.FailOnBreakingChanges);
        Assert.Equal(ReportFormat.Console, config.OutputFormat);
    }

    [Fact]
    public void LoadConfiguration_WithValidLenientConfig_ShouldLoadCorrectly()
    {
        // Arrange
        var configPath = Path.Combine(_testDataPath, "config-lenient-changes.json");

        // Act
        var config = ComparisonConfiguration.LoadFromJsonFile(configPath);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.IsValid());
        Assert.False(config.BreakingChangeRules.TreatTypeRemovalAsBreaking);
        Assert.False(config.BreakingChangeRules.TreatMemberRemovalAsBreaking);
        Assert.False(config.BreakingChangeRules.TreatSignatureChangeAsBreaking);
        Assert.False(config.BreakingChangeRules.TreatAddedTypeAsBreaking);
        Assert.False(config.BreakingChangeRules.TreatAddedMemberAsBreaking);
        Assert.False(config.FailOnBreakingChanges);
        Assert.Equal(ReportFormat.Json, config.OutputFormat);
        Assert.True(config.Filters.IncludeInternals);
        Assert.True(config.Filters.IncludeCompilerGenerated);
        Assert.True(config.Mappings.IgnoreCase);
        Assert.Contains("OldNamespace", config.Mappings.NamespaceMappings.Keys);
        Assert.Contains("OldClass", config.Mappings.TypeMappings.Keys);
    }

    [Fact]
    public void LoadConfiguration_WithNamespaceFilteringConfig_ShouldLoadCorrectly()
    {
        // Arrange
        var configPath = Path.Combine(_testDataPath, "config-namespace-filtering.json");

        // Act
        var config = ComparisonConfiguration.LoadFromJsonFile(configPath);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.IsValid());
        Assert.Contains("System.Text", config.Filters.IncludeNamespaces);
        Assert.Contains("System.IO", config.Filters.IncludeNamespaces);
        Assert.Contains("System.Text.Json.Serialization", config.Filters.ExcludeNamespaces);
        Assert.Contains("System.Text.*", config.Filters.IncludeTypes);
        Assert.Contains("System.IO.File*", config.Filters.IncludeTypes);
        Assert.Equal(ReportFormat.Markdown, config.OutputFormat);
    }

    [Fact]
    public void LoadConfiguration_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var configPath = Path.Combine(_testDataPath, "non-existent-config.json");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => ComparisonConfiguration.LoadFromJsonFile(configPath));
    }

    [Fact]
    public void LoadConfiguration_WithMalformedJson_ShouldThrowJsonException()
    {
        // Arrange
        var configPath = Path.Combine(_testDataPath, "config-malformed.json");

        // Act & Assert
        Assert.Throws<JsonException>(() => ComparisonConfiguration.LoadFromJsonFile(configPath));
    }

    [Fact]
    public void SaveAndLoadConfiguration_ShouldRoundTripCorrectly()
    {
        // Arrange
        var originalConfig = new ComparisonConfiguration
        {
            OutputFormat = ReportFormat.Json,
            FailOnBreakingChanges = false,
            Filters = new FilterConfiguration
            {
                IncludeNamespaces = { "Test.Namespace" },
                ExcludeNamespaces = { "Test.Internal" },
                IncludeInternals = true,
                IncludeCompilerGenerated = false
            },
            Mappings = new MappingConfiguration
            {
                NamespaceMappings = { { "Old", new List<string> { "New" } } },
                TypeMappings = { { "OldType", "NewType" } },
                AutoMapSameNameTypes = false,
                IgnoreCase = true
            },
            Exclusions = new ExclusionConfiguration
            {
                ExcludedTypes = { "ExcludedType" },
                ExcludedMembers = { "ExcludedMember" },
                ExcludedTypePatterns = { "*.Test*" },
                ExcludedMemberPatterns = { "*.get_*" }
            },
            BreakingChangeRules = new BreakingChangeRules
            {
                TreatTypeRemovalAsBreaking = false,
                TreatMemberRemovalAsBreaking = true,
                TreatAddedTypeAsBreaking = false,
                TreatAddedMemberAsBreaking = false,
                TreatSignatureChangeAsBreaking = true
            }
        };

        var tempConfigPath = Path.Combine(_tempConfigPath, "roundtrip-config.json");

        // Act
        originalConfig.SaveToJsonFile(tempConfigPath);
        var loadedConfig = ComparisonConfiguration.LoadFromJsonFile(tempConfigPath);

        // Assert
        Assert.NotNull(loadedConfig);
        Assert.True(loadedConfig.IsValid());
        Assert.Equal(originalConfig.OutputFormat, loadedConfig.OutputFormat);
        Assert.Equal(originalConfig.FailOnBreakingChanges, loadedConfig.FailOnBreakingChanges);
        Assert.Equal(originalConfig.Filters.IncludeInternals, loadedConfig.Filters.IncludeInternals);
        Assert.Equal(originalConfig.Filters.IncludeCompilerGenerated, loadedConfig.Filters.IncludeCompilerGenerated);
        Assert.Equal(originalConfig.Mappings.AutoMapSameNameTypes, loadedConfig.Mappings.AutoMapSameNameTypes);
        Assert.Equal(originalConfig.Mappings.IgnoreCase, loadedConfig.Mappings.IgnoreCase);
        Assert.Equal(originalConfig.BreakingChangeRules.TreatTypeRemovalAsBreaking, loadedConfig.BreakingChangeRules.TreatTypeRemovalAsBreaking);
        Assert.Equal(originalConfig.BreakingChangeRules.TreatMemberRemovalAsBreaking, loadedConfig.BreakingChangeRules.TreatMemberRemovalAsBreaking);
        Assert.Equal(originalConfig.BreakingChangeRules.TreatSignatureChangeAsBreaking, loadedConfig.BreakingChangeRules.TreatSignatureChangeAsBreaking);

        // Check collections
        Assert.Contains("Test.Namespace", loadedConfig.Filters.IncludeNamespaces);
        Assert.Contains("Test.Internal", loadedConfig.Filters.ExcludeNamespaces);
        Assert.Contains("Old", loadedConfig.Mappings.NamespaceMappings.Keys);
        Assert.Contains("OldType", loadedConfig.Mappings.TypeMappings.Keys);
        Assert.Contains("ExcludedType", loadedConfig.Exclusions.ExcludedTypes);
        Assert.Contains("ExcludedMember", loadedConfig.Exclusions.ExcludedMembers);
        Assert.Contains("*.Test*", loadedConfig.Exclusions.ExcludedTypePatterns);
        Assert.Contains("*.get_*", loadedConfig.Exclusions.ExcludedMemberPatterns);
    }

    [Fact]
    public void CreateDefaultConfiguration_ShouldBeValid()
    {
        // Act
        var config = ComparisonConfiguration.CreateDefault();

        // Assert
        Assert.NotNull(config);
        Assert.True(config.IsValid());
        Assert.Equal(ReportFormat.Console, config.OutputFormat);
        Assert.True(config.FailOnBreakingChanges);
        Assert.NotNull(config.Filters);
        Assert.NotNull(config.Mappings);
        Assert.NotNull(config.Exclusions);
        Assert.NotNull(config.BreakingChangeRules);
    }

    [Theory]
    [InlineData("config-strict-breaking-changes.json")]
    [InlineData("config-lenient-changes.json")]
    [InlineData("config-namespace-filtering.json")]
    [InlineData("sample-config.json")]
    public void LoadConfiguration_WithAllValidConfigs_ShouldSucceed(string configFileName)
    {
        // Arrange
        var configPath = Path.Combine(_testDataPath, configFileName);

        // Skip test if config file doesn't exist
        if (!File.Exists(configPath))
        {
            _output.WriteLine($"Skipping test - config file not found: {configFileName}");
            return;
        }

        // Act
        var config = ComparisonConfiguration.LoadFromJsonFile(configPath);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.IsValid(), $"Configuration {configFileName} should be valid");

        // Verify all required properties are set
        Assert.NotNull(config.Filters);
        Assert.NotNull(config.Mappings);
        Assert.NotNull(config.Exclusions);
        Assert.NotNull(config.BreakingChangeRules);
        Assert.True(Enum.IsDefined(typeof(ReportFormat), config.OutputFormat));
    }

    [Fact]
    public void ConfigurationValidation_WithInvalidOutputFormat_ShouldFailValidation()
    {
        // Arrange
        var config = ComparisonConfiguration.CreateDefault();
        config.OutputFormat = (ReportFormat)999; // Invalid enum value

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void ConfigurationSerialization_ShouldProduceReadableJson()
    {
        // Arrange
        var config = ComparisonConfiguration.CreateDefault();
        config.Filters.IncludeNamespaces.Add("Test.Namespace");
        config.Mappings.TypeMappings.Add("OldType", "NewType");

        var tempConfigPath = Path.Combine(_tempConfigPath, "readable-config.json");

        // Act
        config.SaveToJsonFile(tempConfigPath);
        var jsonContent = File.ReadAllText(tempConfigPath);

        // Assert
        Assert.False(string.IsNullOrEmpty(jsonContent));
        Assert.Contains("filters", jsonContent);
        Assert.Contains("mappings", jsonContent);
        Assert.Contains("exclusions", jsonContent);
        Assert.Contains("breakingChangeRules", jsonContent);
        Assert.Contains("Test.Namespace", jsonContent);
        Assert.Contains("OldType", jsonContent);
        Assert.Contains("NewType", jsonContent);

        // Verify it's properly formatted (indented)
        Assert.Contains("  ", jsonContent); // Should contain indentation
        Assert.Contains("\n", jsonContent); // Should contain line breaks
    }

    [Fact]
    public void ConfigurationMerging_WithCommandLineOverrides_ShouldWork()
    {
        // This test verifies that configuration can be loaded and then modified
        // to simulate command-line overrides

        // Arrange
        var configPath = Path.Combine(_testDataPath, "sample-config.json");

        // Skip test if config file doesn't exist
        if (!File.Exists(configPath))
        {
            _output.WriteLine("Skipping test - sample config file not found");
            return;
        }

        // Act
        var config = ComparisonConfiguration.LoadFromJsonFile(configPath);

        // Simulate command-line overrides
        config.Filters.IncludeNamespaces.Add("CommandLine.Override");
        config.Filters.IncludeInternals = true;
        config.OutputFormat = ReportFormat.Json;

        // Assert
        Assert.True(config.IsValid());
        Assert.Contains("CommandLine.Override", config.Filters.IncludeNamespaces);
        Assert.True(config.Filters.IncludeInternals);
        Assert.Equal(ReportFormat.Json, config.OutputFormat);
    }

    public void Dispose()
    {
        // Clean up temporary files
        if (Directory.Exists(_tempConfigPath))
        {
            try
            {
                Directory.Delete(_tempConfigPath, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
