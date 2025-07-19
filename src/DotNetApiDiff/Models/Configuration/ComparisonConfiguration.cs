// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetApiDiff.Models.Configuration;

/// <summary>
/// Main configuration for API comparison
/// </summary>
public class ComparisonConfiguration
{
    /// <summary>
    /// Configuration for namespace and type mappings
    /// </summary>
    [JsonPropertyName("mappings")]
    public MappingConfiguration Mappings { get; set; } = new MappingConfiguration();

    /// <summary>
    /// Configuration for excluding types and members
    /// </summary>
    [JsonPropertyName("exclusions")]
    public ExclusionConfiguration Exclusions { get; set; } = new ExclusionConfiguration();

    /// <summary>
    /// Configuration for breaking change rules
    /// </summary>
    [JsonPropertyName("breakingChangeRules")]
    public BreakingChangeRules BreakingChangeRules { get; set; } = new BreakingChangeRules();

    /// <summary>
    /// Configuration for filtering types and namespaces
    /// </summary>
    [JsonPropertyName("filters")]
    public FilterConfiguration Filters { get; set; } = new FilterConfiguration();

    /// <summary>
    /// Output format for the comparison results
    /// </summary>
    [JsonPropertyName("outputFormat")]
    public ReportFormat OutputFormat { get; set; } = ReportFormat.Console;

    /// <summary>
    /// Path to the output file (if not specified, output is written to console)
    /// </summary>
    [JsonPropertyName("outputPath")]
    public string? OutputPath { get; set; }

    /// <summary>
    /// Whether to fail on breaking changes
    /// </summary>
    [JsonPropertyName("failOnBreakingChanges")]
    public bool FailOnBreakingChanges { get; set; } = true;

    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        return Mappings.IsValid() &&
               Exclusions.IsValid() &&
               Filters.IsValid() &&
               Enum.IsDefined(typeof(ReportFormat), OutputFormat);
    }

    /// <summary>
    /// Creates a default configuration
    /// </summary>
    /// <returns>A default configuration</returns>
    public static ComparisonConfiguration CreateDefault()
    {
        return new ComparisonConfiguration
        {
            Mappings = MappingConfiguration.CreateDefault(),
            Exclusions = ExclusionConfiguration.CreateDefault(),
            BreakingChangeRules = BreakingChangeRules.CreateDefault(),
            Filters = FilterConfiguration.CreateDefault(),
            OutputFormat = ReportFormat.Console,
            OutputPath = null,
            FailOnBreakingChanges = true
        };
    }

    /// <summary>
    /// Loads configuration from a JSON file
    /// </summary>
    /// <param name="path">Path to the JSON file</param>
    /// <returns>The loaded configuration</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid</exception>
    public static ComparisonConfiguration LoadFromJsonFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Configuration file not found: {path}");
        }

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        var config = JsonSerializer.Deserialize<ComparisonConfiguration>(json, options);
        if (config == null)
        {
            throw new JsonException("Failed to deserialize configuration");
        }

        if (!config.IsValid())
        {
            throw new ValidationException("Configuration validation failed");
        }

        return config;
    }

    /// <summary>
    /// Saves configuration to a JSON file
    /// </summary>
    /// <param name="path">Path to the JSON file</param>
    /// <exception cref="IOException">Thrown when the file cannot be written</exception>
    public void SaveToJsonFile(string path)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(path, json);
    }
}
