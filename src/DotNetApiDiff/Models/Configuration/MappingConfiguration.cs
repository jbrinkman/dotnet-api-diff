// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DotNetApiDiff.Models.Configuration;

/// <summary>
/// Configuration for mapping namespaces and types between assemblies
/// </summary>
public class MappingConfiguration
{
    /// <summary>
    /// Dictionary mapping source namespaces to one or more target namespaces
    /// </summary>
    [JsonPropertyName("namespaceMappings")]
    public Dictionary<string, List<string>> NamespaceMappings { get; set; } = new Dictionary<string, List<string>>();

    /// <summary>
    /// Dictionary mapping source type names to target type names
    /// </summary>
    [JsonPropertyName("typeMappings")]
    public Dictionary<string, string> TypeMappings { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Whether to automatically map types with the same name but different namespaces
    /// </summary>
    [JsonPropertyName("autoMapSameNameTypes")]
    public bool AutoMapSameNameTypes { get; set; } = false;

    /// <summary>
    /// Whether to ignore case when mapping types and namespaces
    /// </summary>
    [JsonPropertyName("ignoreCase")]
    public bool IgnoreCase { get; set; } = false;

    /// <summary>
    /// Creates a default mapping configuration
    /// </summary>
    /// <returns>A default mapping configuration</returns>
    public static MappingConfiguration CreateDefault()
    {
        return new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>(),
            TypeMappings = new Dictionary<string, string>(),
            AutoMapSameNameTypes = false,
            IgnoreCase = false
        };
    }

    /// <summary>
    /// Validates the mapping configuration
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        // Check for empty keys or values in namespace mappings
        if (NamespaceMappings.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key) ||
                                         kvp.Value == null ||
                                         kvp.Value.Count == 0 ||
                                         kvp.Value.Any(string.IsNullOrWhiteSpace)))
        {
            return false;
        }

        // Check for empty keys or values in type mappings
        if (TypeMappings.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key) ||
                                    string.IsNullOrWhiteSpace(kvp.Value)))
        {
            return false;
        }

        // Check for circular references in namespace mappings
        if (HasCircularReferences())
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if there are circular references in namespace mappings
    /// </summary>
    /// <returns>True if circular references exist, false otherwise</returns>
    private bool HasCircularReferences()
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var source in NamespaceMappings.Keys)
        {
            if (DetectCircular(source, visited, recursionStack))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Helper method for circular reference detection using DFS
    /// </summary>
    private bool DetectCircular(string current, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (!visited.Contains(current))
        {
            visited.Add(current);
            recursionStack.Add(current);

            // If the current namespace maps to other namespaces
            if (NamespaceMappings.TryGetValue(current, out var targets))
            {
                foreach (var target in targets)
                {
                    // If target is in recursion stack, we have a cycle
                    if (recursionStack.Contains(target))
                    {
                        return true;
                    }

                    // If target is a source namespace and we detect a cycle in its mappings
                    if (NamespaceMappings.ContainsKey(target) && DetectCircular(target, visited, recursionStack))
                    {
                        return true;
                    }
                }
            }
        }

        recursionStack.Remove(current);
        return false;
    }
}
