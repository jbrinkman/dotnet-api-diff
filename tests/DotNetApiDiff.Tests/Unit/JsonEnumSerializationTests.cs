// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetApiDiff.Models;
using DotNetApiDiff.Models.Configuration;
using Xunit;

namespace DotNetApiDiff.Tests.Unit;

/// <summary>
/// Tests for JSON enum serialization/deserialization
/// </summary>
public class JsonEnumSerializationTests
{
    [Fact]
    public void ComparisonConfiguration_JsonDeserialization_ShouldParseEnumCorrectly()
    {
        // Arrange
        var json = """
        {
          "outputFormat": "Html",
          "outputPath": "test.html"
        }
        """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter(null, false) }
        };

        // Act
        var config = JsonSerializer.Deserialize<ComparisonConfiguration>(json, options);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(ReportFormat.Html, config.OutputFormat);
        Assert.Equal("test.html", config.OutputPath);
    }

    [Theory]
    [InlineData("\"Html\"", ReportFormat.Html)]
    [InlineData("\"html\"", ReportFormat.Html)]
    [InlineData("\"HTML\"", ReportFormat.Html)]
    [InlineData("\"Console\"", ReportFormat.Console)]
    [InlineData("\"console\"", ReportFormat.Console)]
    [InlineData("\"Json\"", ReportFormat.Json)]
    [InlineData("\"json\"", ReportFormat.Json)]
    public void ReportFormat_JsonDeserialization_ShouldHandleCaseVariations(string jsonValue, ReportFormat expectedFormat)
    {
        // Arrange
        var json = $"{{\"outputFormat\": {jsonValue}}}";

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter(null, false) }
        };

        // Act
        var config = JsonSerializer.Deserialize<ComparisonConfiguration>(json, options);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(expectedFormat, config.OutputFormat);
    }

    [Fact]
    public void ComparisonConfiguration_LoadFromJsonFile_ShouldWorkWithTestData()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var json = """
        {
          "filters": {
            "includeNamespaces": [],
            "excludeNamespaces": [],
            "includeTypes": [],
            "excludeTypes": [],
            "includeInternals": false,
            "includeCompilerGenerated": false
          },
          "mappings": {
            "namespaceMappings": {},
            "typeMappings": {},
            "autoMapSameNameTypes": true,
            "ignoreCase": false
          },
          "exclusions": {
            "excludedTypes": [],
            "excludedMembers": [],
            "excludedTypePatterns": [],
            "excludedMemberPatterns": []
          },
          "breakingChangeRules": {
            "treatTypeRemovalAsBreaking": true,
            "treatMemberRemovalAsBreaking": true,
            "treatAddedTypeAsBreaking": false,
            "treatAddedMemberAsBreaking": false,
            "treatSignatureChangeAsBreaking": true
          },
          "outputFormat": "Html",
          "outputPath": "comparison-report.html",
          "failOnBreakingChanges": false
        }
        """;

        try
        {
            File.WriteAllText(tempFile, json);

            // Act
            var config = ComparisonConfiguration.LoadFromJsonFile(tempFile);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(ReportFormat.Html, config.OutputFormat);
            Assert.Equal("comparison-report.html", config.OutputPath);
            Assert.False(config.FailOnBreakingChanges);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
