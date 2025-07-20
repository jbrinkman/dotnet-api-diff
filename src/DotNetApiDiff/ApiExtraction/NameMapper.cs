// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT

using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DotNetApiDiff.ApiExtraction;

/// <summary>
/// Implements namespace and type name mapping between assemblies
/// </summary>
public class NameMapper : INameMapper
{
    private readonly ILogger<NameMapper> _logger;
    private readonly MappingConfiguration _configuration;
    private readonly StringComparison _stringComparison;

    /// <summary>
    /// Creates a new instance of the NameMapper
    /// </summary>
    /// <param name="configuration">Mapping configuration</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public NameMapper(MappingConfiguration configuration, ILogger<NameMapper> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stringComparison = configuration.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }

    /// <summary>
    /// Gets the mapping configuration
    /// </summary>
    public MappingConfiguration Configuration => _configuration;

    /// <summary>
    /// Maps a source namespace to one or more target namespaces
    /// </summary>
    /// <param name="sourceNamespace">The source namespace to map</param>
    /// <returns>A collection of mapped target namespaces</returns>
    public IEnumerable<string> MapNamespace(string sourceNamespace)
    {
        if (string.IsNullOrEmpty(sourceNamespace))
        {
            return new[] { string.Empty };
        }

        // Check for exact match first
        foreach (var mapping in _configuration.NamespaceMappings)
        {
            if (string.Equals(mapping.Key, sourceNamespace, _stringComparison))
            {
                _logger.LogDebug(
                    "Mapped namespace {SourceNamespace} to {TargetNamespaces}",
                    sourceNamespace,
                    string.Join(", ", mapping.Value));
                return mapping.Value;
            }
        }

        // Check for prefix matches (e.g., "Company.Product" -> "NewCompany.Product")
        foreach (var mapping in _configuration.NamespaceMappings)
        {
            if (sourceNamespace.StartsWith(mapping.Key + ".", _stringComparison))
            {
                var suffix = sourceNamespace.Substring(mapping.Key.Length + 1);
                var results = mapping.Value.Select(target => CombineNamespaceParts(target, suffix)).ToList();

                _logger.LogDebug(
                    "Mapped namespace {SourceNamespace} to {TargetNamespaces} using prefix mapping",
                    sourceNamespace,
                    string.Join(", ", results));
                return results;
            }
        }

        // No mapping found, return the original
        return new[] { sourceNamespace };
    }

    /// <summary>
    /// Maps a source type name to a target type name
    /// </summary>
    /// <param name="sourceTypeName">The source type name to map</param>
    /// <returns>The mapped target type name, or the original if no mapping exists</returns>
    public string MapTypeName(string sourceTypeName)
    {
        if (string.IsNullOrEmpty(sourceTypeName))
        {
            return string.Empty;
        }

        // Check for exact match
        foreach (var mapping in _configuration.TypeMappings)
        {
            if (string.Equals(mapping.Key, sourceTypeName, _stringComparison))
            {
                _logger.LogDebug(
                    "Mapped type name {SourceTypeName} to {TargetTypeName}",
                    sourceTypeName,
                    mapping.Value);
                return mapping.Value;
            }
        }

        // No mapping found, return the original
        return sourceTypeName;
    }

    /// <summary>
    /// Maps a fully qualified type name (namespace + type name) to one or more possible target type names
    /// </summary>
    /// <param name="sourceFullName">The fully qualified source type name</param>
    /// <returns>A collection of possible mapped target type names</returns>
    public IEnumerable<string> MapFullTypeName(string sourceFullName)
    {
        if (string.IsNullOrEmpty(sourceFullName))
        {
            return new[] { string.Empty };
        }

        // Check for exact type mapping first
        foreach (var mapping in _configuration.TypeMappings)
        {
            if (string.Equals(mapping.Key, sourceFullName, _stringComparison))
            {
                _logger.LogDebug(
                    "Mapped full type name {SourceFullName} to {TargetFullName} using exact type mapping",
                    sourceFullName,
                    mapping.Value);
                return new[] { mapping.Value };
            }
        }

        // If no exact match, try to split into namespace and type name
        int lastDotIndex = sourceFullName.LastIndexOf('.');
        if (lastDotIndex <= 0)
        {
            // No namespace part or empty namespace
            return new[] { MapTypeName(sourceFullName) };
        }

        string sourceNamespace = sourceFullName.Substring(0, lastDotIndex);
        string sourceType = sourceFullName.Substring(lastDotIndex + 1);

        // Map the namespace and type separately
        var mappedNamespaces = MapNamespace(sourceNamespace);
        string mappedType = MapTypeName(sourceType);

        // Combine the mapped namespaces with the mapped type
        var results = mappedNamespaces
            .Select(ns => CombineNamespaceParts(ns, mappedType))
            .ToList();

        if (results.Count > 1)
        {
            _logger.LogDebug(
                "Mapped full type name {SourceFullName} to multiple targets: {TargetFullNames}",
                sourceFullName,
                string.Join(", ", results));
        }
        else if (results.Count == 1)
        {
            _logger.LogDebug(
                "Mapped full type name {SourceFullName} to {TargetFullName}",
                sourceFullName,
                results[0]);
        }

        return results;
    }

    /// <summary>
    /// Checks if a type name should be auto-mapped based on configuration
    /// </summary>
    /// <param name="typeName">The type name to check</param>
    /// <returns>True if the type should be auto-mapped, false otherwise</returns>
    public bool ShouldAutoMapType(string typeName)
    {
        if (!_configuration.AutoMapSameNameTypes)
        {
            return false;
        }

        if (string.IsNullOrEmpty(typeName))
        {
            return false;
        }

        // Extract the simple type name (without namespace)
        int lastDotIndex = typeName.LastIndexOf('.');
        if (lastDotIndex <= 0)
        {
            // No namespace part or empty namespace
            return false;
        }

        string simpleTypeName = typeName.Substring(lastDotIndex + 1);

        // Don't auto-map generic type definitions with backticks
        if (simpleTypeName.Contains('`'))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Combines namespace parts with proper handling of empty namespaces
    /// </summary>
    /// <param name="namespacePart">First namespace part</param>
    /// <param name="suffix">Second namespace part</param>
    /// <returns>Combined namespace</returns>
    private string CombineNamespaceParts(string namespacePart, string suffix)
    {
        return string.IsNullOrEmpty(namespacePart) ? suffix : $"{namespacePart}.{suffix}";
    }
}
