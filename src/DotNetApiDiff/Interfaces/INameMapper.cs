// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models.Configuration;

namespace DotNetApiDiff.Interfaces;

/// <summary>
/// Interface for mapping namespaces and type names between assemblies
/// </summary>
public interface INameMapper
{
    /// <summary>
    /// Maps a source namespace to one or more target namespaces
    /// </summary>
    /// <param name="sourceNamespace">The source namespace to map</param>
    /// <returns>A collection of mapped target namespaces</returns>
    IEnumerable<string> MapNamespace(string sourceNamespace);

    /// <summary>
    /// Maps a source type name to a target type name
    /// </summary>
    /// <param name="sourceTypeName">The source type name to map</param>
    /// <returns>The mapped target type name, or the original if no mapping exists</returns>
    string MapTypeName(string sourceTypeName);

    /// <summary>
    /// Maps a fully qualified type name (namespace + type name) to one or more possible target type names
    /// </summary>
    /// <param name="sourceFullName">The fully qualified source type name</param>
    /// <returns>A collection of possible mapped target type names</returns>
    IEnumerable<string> MapFullTypeName(string sourceFullName);

    /// <summary>
    /// Checks if a type name should be auto-mapped based on configuration
    /// </summary>
    /// <param name="typeName">The type name to check</param>
    /// <returns>True if the type should be auto-mapped, false otherwise</returns>
    bool ShouldAutoMapType(string typeName);

    /// <summary>
    /// Gets the mapping configuration
    /// </summary>
    MappingConfiguration Configuration { get; }
}
