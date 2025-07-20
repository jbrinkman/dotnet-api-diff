// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT

using System.Reflection;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using Microsoft.Extensions.Logging;

namespace DotNetApiDiff.ApiExtraction;

/// <summary>
/// Extracts public API members from .NET assemblies using reflection
/// </summary>
public class ApiExtractor : IApiExtractor
{
    private readonly ITypeAnalyzer _typeAnalyzer;
    private readonly ILogger<ApiExtractor> _logger;

    /// <summary>
    /// Creates a new instance of the ApiExtractor
    /// </summary>
    /// <param name="typeAnalyzer">Type analyzer for detailed member analysis</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public ApiExtractor(ITypeAnalyzer typeAnalyzer, ILogger<ApiExtractor> logger)
    {
        _typeAnalyzer = typeAnalyzer ?? throw new ArgumentNullException(nameof(typeAnalyzer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extracts all public API members from the specified assembly
    /// </summary>
    /// <param name="assembly">Assembly to extract API members from</param>
    /// <param name="filterConfig">Optional filter configuration to apply</param>
    /// <returns>Collection of public API members</returns>
    public IEnumerable<ApiMember> ExtractApiMembers(
        Assembly assembly,
        Models.Configuration.FilterConfiguration? filterConfig = null)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        _logger.LogInformation("Extracting API members from assembly: {AssemblyName}", assembly.GetName().Name);

        if (filterConfig != null)
        {
            _logger.LogInformation(
                "Applying filter configuration with {IncludeCount} includes and {ExcludeCount} excludes",
                filterConfig.IncludeNamespaces.Count + filterConfig.IncludeTypes.Count,
                filterConfig.ExcludeNamespaces.Count + filterConfig.ExcludeTypes.Count);
        }

        var apiMembers = new List<ApiMember>();

        try
        {
            // Get all public types from the assembly, applying filters if provided
            var types = GetPublicTypes(assembly, filterConfig).ToList();
            _logger.LogDebug(
                "Found {TypeCount} public types in assembly {AssemblyName} after filtering",
                types.Count,
                assembly.GetName().Name);

            // Process each type
            foreach (var type in types)
            {
                try
                {
                    // Add the type itself
                    var typeMember = _typeAnalyzer.AnalyzeType(type);
                    apiMembers.Add(typeMember);

                    // Add all members of the type
                    var typeMembers = ExtractTypeMembers(type).ToList();
                    apiMembers.AddRange(typeMembers);

                    _logger.LogDebug(
                        "Extracted {MemberCount} members from type {TypeName}",
                        typeMembers.Count,
                        type.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting members from type {TypeName}", type.FullName);
                }
            }

            _logger.LogInformation(
                "Extracted {MemberCount} total API members from assembly {AssemblyName}",
                apiMembers.Count,
                assembly.GetName().Name);

            return apiMembers;
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogError(ex, "Error loading types from assembly {AssemblyName}", assembly.GetName().Name);

            // Log the loader exceptions for more detailed diagnostics
            if (ex.LoaderExceptions != null)
            {
                foreach (var loaderEx in ex.LoaderExceptions)
                {
                    if (loaderEx != null)
                    {
                        _logger.LogError(loaderEx, "Loader exception: {Message}", loaderEx.Message);
                    }
                }
            }

            // Return any types that were successfully loaded
            return apiMembers;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error extracting API members from assembly {AssemblyName}",
                assembly.GetName().Name);
            return apiMembers;
        }
    }

    /// <summary>
    /// Extracts public API members from a specific type
    /// </summary>
    /// <param name="type">Type to extract members from</param>
    /// <returns>Collection of public API members for the type</returns>
    public IEnumerable<ApiMember> ExtractTypeMembers(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var members = new List<ApiMember>();

        try
        {
            // Extract methods
            members.AddRange(_typeAnalyzer.AnalyzeMethods(type));

            // Extract properties
            members.AddRange(_typeAnalyzer.AnalyzeProperties(type));

            // Extract fields
            members.AddRange(_typeAnalyzer.AnalyzeFields(type));

            // Extract events
            members.AddRange(_typeAnalyzer.AnalyzeEvents(type));

            // Extract constructors
            members.AddRange(_typeAnalyzer.AnalyzeConstructors(type));

            return members;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error extracting members from type {TypeName}",
                type.FullName);
            return members;
        }
    }

    /// <summary>
    /// Gets all public types from the specified assembly
    /// </summary>
    /// <param name="assembly">Assembly to get types from</param>
    /// <param name="filterConfig">Optional filter configuration to apply</param>
    /// <returns>Collection of public types</returns>
    public virtual IEnumerable<Type> GetPublicTypes(
        Assembly assembly,
        Models.Configuration.FilterConfiguration? filterConfig = null)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        try
        {
            // Get all exported (public) types from the assembly
            var types = assembly.GetExportedTypes()
                .Where(t => !t.IsCompilerGenerated() && !t.IsSpecialName);

            // Apply filtering if configuration is provided
            if (filterConfig != null)
            {
                // Filter by namespace includes if specified
                if (filterConfig.IncludeNamespaces.Count > 0)
                {
                    _logger.LogDebug(
                        "Filtering types to include only namespaces: {Namespaces}",
                        string.Join(", ", filterConfig.IncludeNamespaces));

                    types = types.Where(
                        t =>
                            filterConfig.IncludeNamespaces.Any(
                                ns =>
                                    (t.Namespace ?? string.Empty).StartsWith(ns, StringComparison.OrdinalIgnoreCase)));
                }

                // Filter by namespace excludes
                if (filterConfig.ExcludeNamespaces.Count > 0)
                {
                    _logger.LogDebug(
                        "Filtering types to exclude namespaces: {Namespaces}",
                        string.Join(", ", filterConfig.ExcludeNamespaces));

                    types = types.Where(
                        t =>
                            !filterConfig.ExcludeNamespaces.Any(
                                ns =>
                                    (t.Namespace ?? string.Empty).StartsWith(ns, StringComparison.OrdinalIgnoreCase)));
                }

                // Filter by type name includes if specified
                if (filterConfig.IncludeTypes.Count > 0)
                {
                    _logger.LogDebug(
                        "Filtering types to include only types matching patterns: {Patterns}",
                        string.Join(", ", filterConfig.IncludeTypes));

                    types = types.Where(
                        t =>
                            filterConfig.IncludeTypes.Any(
                                pattern =>
                                    IsTypeMatchingPattern(t, pattern)));
                }

                // Filter by type name excludes
                if (filterConfig.ExcludeTypes.Count > 0)
                {
                    _logger.LogDebug(
                        "Filtering types to exclude types matching patterns: {Patterns}",
                        string.Join(", ", filterConfig.ExcludeTypes));

                    types = types.Where(
                        t =>
                            !filterConfig.ExcludeTypes.Any(
                                pattern =>
                                    IsTypeMatchingPattern(t, pattern)));
                }

                // Filter internal types if not included
                if (!filterConfig.IncludeInternals)
                {
                    types = types.Where(t => t.IsPublic || t.IsNestedPublic);
                }

                // Filter compiler-generated types if not included
                if (!filterConfig.IncludeCompilerGenerated)
                {
                    types = types.Where(t => !IsCompilerGeneratedType(t));
                }
            }

            return types.OrderBy(t => t.FullName);
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogError(
                ex,
                "Error loading types from assembly {AssemblyName}",
                assembly.GetName().Name);

            // Return any types that were successfully loaded
            return ex.Types.Where(t => t != null).Cast<Type>();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting public types from assembly {AssemblyName}",
                assembly.GetName().Name);
            return Enumerable.Empty<Type>();
        }
    }

    /// <summary>
    /// Checks if a type matches a wildcard pattern
    /// </summary>
    /// <param name="type">Type to check</param>
    /// <param name="pattern">Pattern to match against</param>
    /// <returns>True if the type matches the pattern, false otherwise</returns>
    private bool IsTypeMatchingPattern(Type type, string pattern)
    {
        var typeName = type.FullName ?? type.Name;

        // Convert wildcard pattern to regex
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(
                typeName,
                regexPattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        catch
        {
            // If regex fails, fall back to simple string comparison
            return typeName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Checks if a type is compiler-generated
    /// </summary>
    /// <param name="type">Type to check</param>
    /// <returns>True if compiler-generated, false otherwise</returns>
    private bool IsCompilerGeneratedType(Type type)
    {
        // Check for compiler-generated attributes
        if (type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true))
        {
            return true;
        }

        // Check for compiler-generated naming patterns
        return type.Name.Contains('<') ||
               type.Name.StartsWith("__") ||
               type.Name.Contains("AnonymousType") ||
               type.Name.Contains("DisplayClass");
    }
}

/// <summary>
/// Extension methods for reflection types
/// </summary>
public static class ReflectionExtensions
{
    /// <summary>
    /// Checks if a type is compiler-generated
    /// </summary>
    /// <param name="type">Type to check</param>
    /// <returns>True if compiler-generated, false otherwise</returns>
    public static bool IsCompilerGenerated(this Type type)
    {
        // Check for compiler-generated types like closures, iterators, etc.
        if (type.Name.Contains('<') || type.Name.StartsWith("__"))
        {
            return true;
        }

        // Check for CompilerGeneratedAttribute using IsDefined for better performance
        return type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true);
    }
}
