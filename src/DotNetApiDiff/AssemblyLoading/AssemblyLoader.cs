using System.Reflection;
using System.Runtime.Loader;
using System.Security;
using DotNetApiDiff.Interfaces;
using Microsoft.Extensions.Logging;

namespace DotNetApiDiff.AssemblyLoading;

/// <summary>
/// Implementation of IAssemblyLoader that loads assemblies in isolated contexts
/// </summary>
public class AssemblyLoader : IAssemblyLoader, IDisposable
{
    private readonly ILogger<AssemblyLoader> _logger;
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();
    private readonly Dictionary<string, IsolatedAssemblyLoadContext> _loadContexts = new();
    
    /// <summary>
    /// Creates a new assembly loader with the specified logger
    /// </summary>
    /// <param name="logger">Logger for diagnostic information</param>
    public AssemblyLoader(ILogger<AssemblyLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads an assembly from the specified file path
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file</param>
    /// <returns>Loaded assembly</returns>
    /// <exception cref="ArgumentException">Thrown when assembly path is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when assembly file is not found</exception>
    /// <exception cref="BadImageFormatException">Thrown when assembly file is invalid</exception>
    /// <exception cref="SecurityException">Thrown when there are insufficient permissions to load the assembly</exception>
    /// <exception cref="PathTooLongException">Thrown when the assembly path is too long</exception>
    /// <exception cref="ReflectionTypeLoadException">Thrown when types in the assembly cannot be loaded</exception>
    public Assembly LoadAssembly(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            _logger.LogError("Assembly path cannot be null or empty");
            throw new ArgumentException("Assembly path cannot be null or empty", nameof(assemblyPath));
        }

        // Normalize the path to ensure consistent dictionary keys
        assemblyPath = Path.GetFullPath(assemblyPath);
        
        // Check if we've already loaded this assembly
        if (_loadedAssemblies.TryGetValue(assemblyPath, out var loadedAssembly))
        {
            _logger.LogDebug("Returning previously loaded assembly from {Path}", assemblyPath);
            return loadedAssembly;
        }
        
        _logger.LogInformation("Loading assembly from {Path}", assemblyPath);
        
        try
        {
            // Verify the file exists
            if (!File.Exists(assemblyPath))
            {
                _logger.LogError("Assembly file not found: {Path}", assemblyPath);
                throw new FileNotFoundException($"Assembly file not found: {assemblyPath}", assemblyPath);
            }
            
            // Create a new isolated load context for this assembly
            var loadContext = new IsolatedAssemblyLoadContext(assemblyPath, _logger);
            
            // Add the directory of the assembly as a search path
            var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
            if (!string.IsNullOrEmpty(assemblyDirectory))
            {
                loadContext.AddSearchPath(assemblyDirectory);
            }
            
            // Load the assembly in the isolated context
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            
            // Store the assembly and load context for later use
            _loadedAssemblies[assemblyPath] = assembly;
            _loadContexts[assemblyPath] = loadContext;
            
            _logger.LogInformation("Successfully loaded assembly: {AssemblyName} from {Path}", 
                assembly.GetName().Name, assemblyPath);
            
            return assembly;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Assembly file not found: {Path}", assemblyPath);
            throw;
        }
        catch (BadImageFormatException ex)
        {
            _logger.LogError(ex, "Invalid assembly format: {Path}", assemblyPath);
            throw;
        }
        catch (SecurityException ex)
        {
            _logger.LogError(ex, "Security exception loading assembly: {Path}", assemblyPath);
            throw;
        }
        catch (PathTooLongException ex)
        {
            _logger.LogError(ex, "Path too long for assembly: {Path}", assemblyPath);
            throw;
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogError(ex, "Failed to load types from assembly: {Path}", assemblyPath);
            
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
            
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading assembly: {Path}", assemblyPath);
            throw;
        }
    }

    /// <summary>
    /// Validates that the specified path contains a valid .NET assembly
    /// </summary>
    /// <param name="assemblyPath">Path to validate</param>
    /// <returns>True if path contains a valid assembly, false otherwise</returns>
    public bool IsValidAssembly(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            _logger.LogDebug("Assembly path is null or empty");
            return false;
        }

        try
        {
            // Check if the file exists
            if (!File.Exists(assemblyPath))
            {
                _logger.LogDebug("Assembly file does not exist: {Path}", assemblyPath);
                return false;
            }

            // Try to load the assembly in a temporary context to validate it
            var tempContext = new IsolatedAssemblyLoadContext(assemblyPath);
            try
            {
                var assembly = tempContext.LoadFromAssemblyPath(assemblyPath);
                
                // If we got here, the assembly is valid
                _logger.LogDebug("Successfully validated assembly: {Path}", assemblyPath);
                return true;
            }
            finally
            {
                // Unload the temporary context
                tempContext.Unload();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Assembly validation failed for {Path}: {Message}", assemblyPath, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Unloads all loaded assemblies and their contexts
    /// </summary>
    public void UnloadAll()
    {
        _logger.LogInformation("Unloading all assemblies");
        
        foreach (var context in _loadContexts.Values)
        {
            try
            {
                context.Unload();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error unloading assembly context");
            }
        }
        
        _loadContexts.Clear();
        _loadedAssemblies.Clear();
    }

    /// <summary>
    /// Disposes the assembly loader and unloads all assemblies
    /// </summary>
    public void Dispose()
    {
        _logger.LogDebug("Disposing AssemblyLoader");
        UnloadAll();
        GC.SuppressFinalize(this);
    }
}