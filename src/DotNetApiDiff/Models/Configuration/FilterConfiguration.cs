// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DotNetApiDiff.Models.Configuration;

/// <summary>
/// Configuration for filtering types and namespaces during API comparison
/// </summary>
public class FilterConfiguration
{
    /// <summary>
    /// List of namespaces to include in the comparison (if empty, all namespaces are included)
    /// </summary>
    [JsonPropertyName("includeNamespaces")]
    public List<string> IncludeNamespaces { get; set; } = new List<string>();

    /// <summary>
    /// List of namespaces to exclude from the comparison
    /// </summary>
    [JsonPropertyName("excludeNamespaces")]
    public List<string> ExcludeNamespaces { get; set; } = new List<string>();

    /// <summary>
    /// List of type name patterns to include in the comparison (if empty, all types are included)
    /// </summary>
    [JsonPropertyName("includeTypes")]
    public List<string> IncludeTypes { get; set; } = new List<string>();

    /// <summary>
    /// List of type name patterns to exclude from the comparison
    /// </summary>
    [JsonPropertyName("excludeTypes")]
    public List<string> ExcludeTypes { get; set; } = new List<string>();

    /// <summary>
    /// Whether to include internal types in the comparison
    /// </summary>
    [JsonPropertyName("includeInternals")]
    public bool IncludeInternals { get; set; } = false;

    /// <summary>
    /// Whether to include compiler-generated types in the comparison
    /// </summary>
    [JsonPropertyName("includeCompilerGenerated")]
    public bool IncludeCompilerGenerated { get; set; } = false;

    /// <summary>
    /// Creates a default filter configuration
    /// </summary>
    /// <returns>A default filter configuration</returns>
    public static FilterConfiguration CreateDefault()
    {
        return new FilterConfiguration
        {
            IncludeNamespaces = new List<string>(),
            ExcludeNamespaces = new List<string>(),
            IncludeTypes = new List<string>(),
            ExcludeTypes = new List<string>(),
            IncludeInternals = false,
            IncludeCompilerGenerated = false
        };
    }

    /// <summary>
    /// Validates the filter configuration
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        // All patterns should be non-empty
        if (IncludeNamespaces.Any(string.IsNullOrWhiteSpace) ||
            ExcludeNamespaces.Any(string.IsNullOrWhiteSpace) ||
            IncludeTypes.Any(string.IsNullOrWhiteSpace) ||
            ExcludeTypes.Any(string.IsNullOrWhiteSpace))
        {
            return false;
        }

        return true;
    }
}
