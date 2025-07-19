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
        Assert.Equal(ReportFormat.Console, config.OutputFormat);
        Assert.Null(config.OutputPath);
        Assert.True(config.FailOnBreakingChanges);
        Assert.True(config.IsValid());
    }

    [Fact]
    public void IsValid_WithValidConfiguration_ReturnsTrue()
    {
        // Arrange
        var config = ComparisonConfiguration.CreateDefault();

        // Act & Assert
        Assert.True(config.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidMappings_ReturnsFalse()
    {
        // Arrange
        var config = ComparisonConfiguration.CreateDefault();
        config.Mappings.NamespaceMappings.Add("", new List<string> { "NewNamespace" }); // Invalid empty key

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidExclusions_ReturnsFalse()
    {
        // Arrange
        var config = ComparisonConfiguration.CreateDefault();
        config.Exclusions.ExcludedTypes.Add(""); // Invalid empty type

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidFilters_ReturnsFalse()
    {
        // Arrange
        var config = ComparisonConfiguration.CreateDefault();
        config.Filters.IncludeNamespaces.Add(""); // Invalid empty namespace

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidOutputFormat_ReturnsFalse()
    {
        // Arrange
        var config = ComparisonConfiguration.CreateDefault();
        config.OutputFormat = (ReportFormat)999; // Invalid enum value

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void Serialization_RoundTrip_PreservesValues()
    {
        // Arrange
        var config = new ComparisonConfiguration
        {
            Mappings = new MappingConfiguration
            {
                NamespaceMappings = new Dictionary<string, List<string>>
                {
                    { "OldNamespace", new List<string> { "NewNamespace" } }
                },
                TypeMappings = new Dictionary<string, string>
                {
                    { "OldType", "NewType" }
                },
                AutoMapSameNameTypes = true
            },
            Exclusions = new ExclusionConfiguration
            {
                ExcludedTypes = new List<string> { "System.Diagnostics.Debug" },
                ExcludeObsolete = true
            },
            BreakingChangeRules = new BreakingChangeRules
            {
                TreatAddedInterfaceAsBreaking = true,
                TreatParameterNameChangeAsBreaking = true
            },
            Filters = new FilterConfiguration
            {
                IncludeNamespaces = new List<string> { "System" },
                IncludeInternals = true
            },
            OutputFormat = ReportFormat.Json,
            OutputPath = "output.json",
            FailOnBreakingChanges = false
        };

        // Act
        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<ComparisonConfiguration>(json);

        // Assert
        Assert.NotNull(deserialized);

        // Check mappings
        Assert.Single(deserialized!.Mappings.NamespaceMappings);
        Assert.True(deserialized.Mappings.NamespaceMappings.ContainsKey("OldNamespace"));
        Assert.Single(deserialized.Mappings.TypeMappings);
        Assert.True(deserialized.Mappings.TypeMappings.ContainsKey("OldType"));
        Assert.True(deserialized.Mappings.AutoMapSameNameTypes);

        // Check exclusions
        Assert.Single(deserialized.Exclusions.ExcludedTypes);
        Assert.Contains("System.Diagnostics.Debug", deserialized.Exclusions.ExcludedTypes);
        Assert.True(deserialized.Exclusions.ExcludeObsolete);

        // Check breaking change rules
        Assert.True(deserialized.BreakingChangeRules.TreatAddedInterfaceAsBreaking);
        Assert.True(deserialized.BreakingChangeRules.TreatParameterNameChangeAsBreaking);

        // Check filters
        Assert.Single(deserialized.Filters.IncludeNamespaces);
        Assert.Contains("System", deserialized.Filters.IncludeNamespaces);
        Assert.True(deserialized.Filters.IncludeInternals);

        // Check other properties
        Assert.Equal(ReportFormat.Json, deserialized.OutputFormat);
        Assert.Equal("output.json", deserialized.OutputPath);
        Assert.False(deserialized.FailOnBreakingChanges);
    }

    [Fact]
    public void LoadFromJsonFile_WithValidJson_LoadsConfiguration()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            var config = new ComparisonConfiguration
            {
                OutputFormat = ReportFormat.Json,
                OutputPath = "output.json",
                FailOnBreakingChanges = false
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            File.WriteAllText(tempFile, json);

            // Act
            var loaded = ComparisonConfiguration.LoadFromJsonFile(tempFile);

            // Assert
            Assert.NotNull(loaded);
            Assert.Equal(ReportFormat.Json, loaded.OutputFormat);
            Assert.Equal("output.json", loaded.OutputPath);
            Assert.False(loaded.FailOnBreakingChanges);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadFromJsonFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => ComparisonConfiguration.LoadFromJsonFile(nonExistentFile));
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
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadFromJsonFile_WithInvalidConfiguration_ThrowsValidationException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            var json = @"{
                ""mappings"": {
                    ""namespaceMappings"": {
                        """": [""NewNamespace""]
                    }
                }
            }";

            File.WriteAllText(tempFile, json);

            // Act & Assert
            Assert.Throws<ValidationException>(() => ComparisonConfiguration.LoadFromJsonFile(tempFile));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void SaveToJsonFile_WritesValidJson()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            var config = new ComparisonConfiguration
            {
                OutputFormat = ReportFormat.Json,
                OutputPath = "output.json",
                FailOnBreakingChanges = false
            };

            // Act
            config.SaveToJsonFile(tempFile);

            // Assert
            var json = File.ReadAllText(tempFile);
            Assert.Contains("\"outputFormat\"", json);
            Assert.Contains("\"outputPath\"", json);
            Assert.Contains("\"failOnBreakingChanges\"", json);

            // Verify we can deserialize it back
            var deserialized = JsonSerializer.Deserialize<ComparisonConfiguration>(json);
            Assert.NotNull(deserialized);
            Assert.Equal(ReportFormat.Json, deserialized!.OutputFormat);
            Assert.Equal("output.json", deserialized.OutputPath);
            Assert.False(deserialized.FailOnBreakingChanges);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
