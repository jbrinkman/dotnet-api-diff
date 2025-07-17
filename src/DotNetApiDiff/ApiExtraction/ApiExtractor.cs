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
    /// <returns>Collection of public API members</returns>
    public IEnumerable<ApiMember> ExtractApiMembers(Assembly assembly)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        _logger.LogInformation("Extracting API members from assembly: {AssemblyName}", assembly.GetName().Name);
        
        var apiMembers = new List<ApiMember>();
        
        try
        {
            // Get all public types from the assembly
            var types = GetPublicTypes(assembly).ToList();
            _logger.LogDebug("Found {TypeCount} public types in assembly {AssemblyName}", 
                types.Count, assembly.GetName().Name);
            
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
                    
                    _logger.LogDebug("Extracted {MemberCount} members from type {TypeName}", 
                        typeMembers.Count, type.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting members from type {TypeName}", type.FullName);
                }
            }
            
            _logger.LogInformation("Extracted {MemberCount} total API members from assembly {AssemblyName}", 
                apiMembers.Count, assembly.GetName().Name);
            
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
            _logger.LogError(ex, "Error extracting API members from assembly {AssemblyName}", 
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
            _logger.LogError(ex, "Error extracting members from type {TypeName}", type.FullName);
            return members;
        }
    }

    /// <summary>
    /// Gets all public types from the specified assembly
    /// </summary>
    /// <param name="assembly">Assembly to get types from</param>
    /// <returns>Collection of public types</returns>
    public virtual IEnumerable<Type> GetPublicTypes(Assembly assembly)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        try
        {
            // Get all exported (public) types from the assembly
            return assembly.GetExportedTypes()
                .Where(t => !t.IsCompilerGenerated() && !t.IsSpecialName)
                .OrderBy(t => t.FullName);
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogError(ex, "Error loading types from assembly {AssemblyName}", assembly.GetName().Name);
            
            // Return any types that were successfully loaded
            return ex.Types.Where(t => t != null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public types from assembly {AssemblyName}", 
                assembly.GetName().Name);
            return Enumerable.Empty<Type>();
        }
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
        
        // Check for CompilerGeneratedAttribute
        var attributes = type.GetCustomAttributes(true);
        return attributes.Any(a => a.GetType().Name == "CompilerGeneratedAttribute");
    }
}