// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace DotNetApiDiff.AssemblyLoading;

/// <summary>
/// Custom assembly load context that provides isolation for loaded assemblies
/// </summary>
public class IsolatedAssemblyLoadContext : AssemblyLoadContext
{
    private readonly string _assemblyDirectory;
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _mainAssemblyPath;
    private readonly ILogger? _logger;

    /// <summary>
    /// Creates a new isolated assembly load context
    /// </summary>
    /// <param name="assemblyPath">Path to the main assembly</param>
    public IsolatedAssemblyLoadContext(string assemblyPath)
        : base(isCollectible: true)
    {
        _mainAssemblyPath = assemblyPath;
        _assemblyDirectory = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
        _resolver = new AssemblyDependencyResolver(assemblyPath);
        _logger = null;
    }

    /// <summary>
    /// Creates a new isolated assembly load context
    /// </summary>
    /// <param name="assemblyPath">Path to the main assembly</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public IsolatedAssemblyLoadContext(string assemblyPath, ILogger logger)
        : this(assemblyPath)
    {
        _logger = logger;
    }

    /// <summary>
    /// Additional search paths for assemblies
    /// </summary>
    public List<string> AdditionalSearchPaths { get; } = new List<string>();

    /// <summary>
    /// Adds an additional search path for assemblies
    /// </summary>
    /// <param name="path">Path to search for assemblies</param>
    public void AddSearchPath(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path) && !AdditionalSearchPaths.Contains(path))
        {
            AdditionalSearchPaths.Add(path);
            _logger?.LogDebug("Added search path: {Path}", path);
        }
    }

    /// <summary>
    /// Loads an assembly with the given name
    /// </summary>
    /// <param name="assemblyName">The assembly name to load</param>
    /// <returns>The loaded assembly or null if not found</returns>
    protected override System.Reflection.Assembly? Load(AssemblyName assemblyName)
    {
        try
        {
            _logger?.LogDebug("Attempting to resolve assembly: {AssemblyName}", assemblyName.FullName);

            // First, try to resolve using the dependency resolver
            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                _logger?.LogDebug("Resolved assembly {AssemblyName} to path: {Path}", assemblyName.FullName, assemblyPath);
                return LoadFromAssemblyPath(assemblyPath);
            }

            // Next, try to find the assembly in the same directory
            string potentialPath = Path.Combine(_assemblyDirectory, $"{assemblyName.Name}.dll");
            if (File.Exists(potentialPath))
            {
                _logger?.LogDebug("Found assembly {AssemblyName} in directory: {Path}", assemblyName.FullName, potentialPath);
                return LoadFromAssemblyPath(potentialPath);
            }

            // If we can't resolve it, return null to let the runtime handle it
            _logger?.LogDebug("Could not resolve assembly: {AssemblyName}", assemblyName.FullName);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "Error resolving assembly {AssemblyName} for {MainAssembly}",
                assemblyName.FullName,
                _mainAssemblyPath);
            throw;
        }
    }

    /// <summary>
    /// Loads a native library with the given name
    /// </summary>
    /// <param name="libName">The library name to load</param>
    /// <returns>The loaded library handle or IntPtr.Zero if not found</returns>
    protected override IntPtr LoadUnmanagedDll(string libName)
    {
        try
        {
            _logger?.LogDebug("Attempting to resolve native library: {LibName}", libName);

            // First, try to resolve using the dependency resolver
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(libName);
            if (libraryPath != null)
            {
                _logger?.LogDebug("Resolved native library {LibName} to path: {Path}", libName, libraryPath);
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            // Next, try to find the library in the same directory
            string potentialPath = Path.Combine(_assemblyDirectory, libName);
            if (File.Exists(potentialPath))
            {
                _logger?.LogDebug("Found native library {LibName} in directory: {Path}", libName, potentialPath);
                return LoadUnmanagedDllFromPath(potentialPath);
            }

            // If we can't resolve it, return IntPtr.Zero to let the runtime handle it
            _logger?.LogDebug("Could not resolve native library: {LibName}", libName);
            return IntPtr.Zero;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "Error resolving native library {LibName} for {MainAssembly}",
                libName,
                _mainAssemblyPath);
            throw;
        }
    }
}
