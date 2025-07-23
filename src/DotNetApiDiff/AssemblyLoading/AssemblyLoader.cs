// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
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
    private readonly ILogger<AssemblyLoader> logger;
    private readonly Dictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();
    private readonly Dictionary<string, IsolatedAssemblyLoadContext> loadContexts = new Dictionary<string, IsolatedAssemblyLoadContext>();

    /// <summary>
    /// Creates a new assembly loader with the specified logger
    /// </summary>
    /// <param name="logger">Logger for diagnostic information</param>
    public AssemblyLoader(ILogger<AssemblyLoader> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            this.logger.LogError("Assembly path cannot be null or empty");
            throw new ArgumentException("Assembly path cannot be null or empty", nameof(assemblyPath));
        }

        // Normalize the path to ensure consistent dictionary keys
        try
        {
            assemblyPath = Path.GetFullPath(assemblyPath);
        }
        catch (PathTooLongException ex)
        {
            this.logger.LogError(ex, "Path too long for assembly: {Path}", assemblyPath);
            throw;
        }
        catch (SecurityException ex)
        {
            this.logger.LogError(ex, "Security exception accessing path: {Path}", assemblyPath);
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error normalizing assembly path: {Path}", assemblyPath);
            throw new ArgumentException($"Invalid assembly path: {assemblyPath}", nameof(assemblyPath), ex);
        }

        // Check if we've already loaded this assembly
        if (this.loadedAssemblies.TryGetValue(assemblyPath, out var loadedAssembly))
        {
            this.logger.LogDebug("Returning previously loaded assembly from {Path}", assemblyPath);
            return loadedAssembly;
        }

        using (this.logger.BeginScope("Loading assembly {Path}", assemblyPath))
        {
            this.logger.LogInformation("Loading assembly from {Path}", assemblyPath);

            try
            {
                // Verify the file exists
                if (!File.Exists(assemblyPath))
                {
                    this.logger.LogError("Assembly file not found: {Path}", assemblyPath);
                    throw new FileNotFoundException($"Assembly file not found: {assemblyPath}", assemblyPath);
                }

                // Verify the file is accessible
                try
                {
                    using (var fileStream = File.OpenRead(assemblyPath))
                    {
                        // Just testing if we can open the file
                    }
                }
                catch (IOException ex)
                {
                    this.logger.LogError(ex, "Cannot access assembly file: {Path}", assemblyPath);
                    throw new IOException($"Cannot access assembly file: {assemblyPath}", ex);
                }

                // Create a new isolated load context for this assembly
                var loadContext = new IsolatedAssemblyLoadContext(assemblyPath, this.logger);

                // Add the directory of the assembly as a search path
                var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
                if (!string.IsNullOrEmpty(assemblyDirectory))
                {
                    loadContext.AddSearchPath(assemblyDirectory);

                    // Also add any subdirectories that might contain dependencies
                    try
                    {
                        foreach (var subDir in Directory.GetDirectories(assemblyDirectory, "*", SearchOption.TopDirectoryOnly))
                        {
                            loadContext.AddSearchPath(subDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail if we can't access subdirectories
                        this.logger.LogWarning(ex, "Could not access subdirectories of {Directory}", assemblyDirectory);
                    }
                }

                // Load the assembly in the isolated context
                Assembly assembly;
                try
                {
                    assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
                }
                catch (BadImageFormatException ex)
                {
                    this.logger.LogError(ex, "Invalid assembly format: {Path}", assemblyPath);

                    // Try to determine if this is a native DLL or other non-.NET assembly
                    if (IsProbablyNativeDll(assemblyPath))
                    {
                        this.logger.LogError("The file appears to be a native DLL, not a .NET assembly: {Path}", assemblyPath);
                        throw new BadImageFormatException($"The file appears to be a native DLL, not a .NET assembly: {assemblyPath}", ex);
                    }

                    throw;
                }
                catch (FileLoadException ex)
                {
                    this.logger.LogError(ex, "Failed to load assembly file: {Path}, FileName: {FileName}", assemblyPath, ex.FileName);
                    throw;
                }

                // Store the assembly and load context for later use
                this.loadedAssemblies[assemblyPath] = assembly;
                this.loadContexts[assemblyPath] = loadContext;

                // Log assembly details
                var assemblyName = assembly.GetName();
                this.logger.LogInformation(
                    "Successfully loaded assembly: {AssemblyName} v{Version} from {Path}",
                    assemblyName.Name,
                    assemblyName.Version,
                    assemblyPath);

                // Log referenced assemblies at debug level
                if (this.logger.IsEnabled(LogLevel.Debug))
                {
                    try
                    {
                        var referencedAssemblies = assembly.GetReferencedAssemblies();
                        this.logger.LogDebug(
                            "Assembly {AssemblyName} references {Count} assemblies",
                            assemblyName.Name,
                            referencedAssemblies.Length);

                        foreach (var reference in referencedAssemblies)
                        {
                            this.logger.LogDebug(
                                "Referenced assembly: {Name} v{Version}",
                                reference.Name,
                                reference.Version);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogDebug(ex, "Error getting referenced assemblies for {AssemblyName}", assemblyName.Name);
                    }
                }

                return assembly;
            }
            catch (FileNotFoundException ex)
            {
                this.logger.LogError(ex, "Assembly file not found: {Path}", assemblyPath);
                throw;
            }
            catch (BadImageFormatException ex)
            {
                this.logger.LogError(ex, "Invalid assembly format: {Path}", assemblyPath);
                throw;
            }
            catch (SecurityException ex)
            {
                this.logger.LogError(ex, "Security exception loading assembly: {Path}", assemblyPath);
                throw;
            }
            catch (PathTooLongException ex)
            {
                this.logger.LogError(ex, "Path too long for assembly: {Path}", assemblyPath);
                throw;
            }
            catch (ReflectionTypeLoadException ex)
            {
                this.logger.LogError(ex, "Failed to load types from assembly: {Path}", assemblyPath);

                // Log the loader exceptions for more detailed diagnostics
                if (ex.LoaderExceptions != null)
                {
                    int loaderExceptionCount = ex.LoaderExceptions.Length;
                    this.logger.LogError("Loader exceptions count: {Count}", loaderExceptionCount);

                    // Log up to 5 loader exceptions to avoid excessive logging
                    int logCount = Math.Min(loaderExceptionCount, 5);
                    for (int i = 0; i < logCount; i++)
                    {
                        var loaderEx = ex.LoaderExceptions[i];
                        if (loaderEx != null)
                        {
                            this.logger.LogError(loaderEx, "Loader exception {Index}: {Message}", i + 1, loaderEx.Message);
                        }
                    }

                    if (loaderExceptionCount > logCount)
                    {
                        this.logger.LogError("... and {Count} more loader exceptions", loaderExceptionCount - logCount);
                    }
                }

                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Unexpected error loading assembly: {Path}", assemblyPath);
                throw;
            }
        }
    }

    /// <summary>
    /// Attempts to determine if a file is likely a native DLL rather than a .NET assembly
    /// </summary>
    /// <param name="filePath">Path to the file to check</param>
    /// <returns>True if the file appears to be a native DLL, false otherwise</returns>
    private bool IsProbablyNativeDll(string filePath)
    {
        try
        {
            // Read the first few bytes to check for the PE header
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (fileStream.Length < 64)
                {
                    return false; // Too small to be a valid DLL
                }

                byte[] buffer = new byte[2];
                int bytesRead = fileStream.Read(buffer, 0, 2);
                if (bytesRead < 2)
                {
                    return false; // Not enough bytes to determine if it's a DLL
                }

                // Check for the MZ header (0x4D, 0x5A)
                if (buffer[0] != 0x4D || buffer[1] != 0x5A)
                {
                    return false; // Not a valid PE file
                }

                // Skip to the PE header offset location
                fileStream.Seek(0x3C, SeekOrigin.Begin);

                // Read the PE header offset
                byte[] offsetBuffer = new byte[4];
                bytesRead = fileStream.Read(offsetBuffer, 0, 4);
                if (bytesRead < 4)
                {
                    return false; // Not enough bytes to determine if it's a DLL
                }

                int peOffset = BitConverter.ToInt32(offsetBuffer, 0);

                // Seek to the PE header
                fileStream.Seek(peOffset, SeekOrigin.Begin);

                // Read the PE signature
                byte[] peBuffer = new byte[4];
                bytesRead = fileStream.Read(peBuffer, 0, 4);
                if (bytesRead < 4)
                {
                    return false; // Not enough bytes to determine if it's a DLL
                }

                // Check for PE signature "PE\0\0"
                if (peBuffer[0] != 0x50 || peBuffer[1] != 0x45 || peBuffer[2] != 0 || peBuffer[3] != 0)
                {
                    return false; // Not a valid PE file
                }

                // It's a valid PE file, but we need more checks to determine if it's a .NET assembly
                // Skip the COFF header (20 bytes)
                fileStream.Seek(peOffset + 4 + 20, SeekOrigin.Begin);

                // Read the Optional Header magic value
                byte[] magicBuffer = new byte[2];
                bytesRead = fileStream.Read(magicBuffer, 0, 2);
                if (bytesRead < 2)
                {
                    return false; // Not enough bytes to determine if it's a DLL
                }

                // PE32 (0x10B) or PE32+ (0x20B)
                ushort magic = BitConverter.ToUInt16(magicBuffer, 0);
                if (magic != 0x10B && magic != 0x20B)
                {
                    return false; // Not a valid PE optional header
                }

                // Skip to the data directories
                int dataDirectoryOffset;
                if (magic == 0x10B)
                {
                    dataDirectoryOffset = 96; // PE32
                }
                else
                {
                    dataDirectoryOffset = 112; // PE32+
                }

                fileStream.Seek(peOffset + 4 + 20 + dataDirectoryOffset, SeekOrigin.Begin);

                // The 15th data directory is the CLR header (14 zero-based index)
                fileStream.Seek(14 * 8, SeekOrigin.Current);

                // Read the CLR header RVA and size
                byte[] clrBuffer = new byte[8];
                bytesRead = fileStream.Read(clrBuffer, 0, 8);
                if (bytesRead < 8)
                {
                    return false; // Not enough bytes to determine if it's a DLL
                }

                uint clrRva = BitConverter.ToUInt32(clrBuffer, 0);
                uint clrSize = BitConverter.ToUInt32(clrBuffer, 4);

                // If the CLR header RVA is 0, it's not a .NET assembly
                return clrRva == 0;
            }
        }
        catch (Exception ex)
        {
            this.logger.LogDebug(ex, "Error checking if file is a native DLL: {Path}", filePath);
            return false; // Assume it's not a native DLL if we can't check
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
            this.logger.LogDebug("Assembly path is null or empty");
            return false;
        }

        try
        {
            // Check if the file exists
            if (!File.Exists(assemblyPath))
            {
                this.logger.LogDebug("Assembly file does not exist: {Path}", assemblyPath);
                return false;
            }

            // Try to load the assembly in a temporary context to validate it
            var tempContext = new IsolatedAssemblyLoadContext(assemblyPath, this.logger);
            try
            {
                var assembly = tempContext.LoadFromAssemblyPath(assemblyPath);

                // If we got here, the assembly is valid
                this.logger.LogDebug("Successfully validated assembly: {Path}", assemblyPath);
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
            this.logger.LogDebug(ex, "Assembly validation failed for {Path}: {Message}", assemblyPath, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Unloads all loaded assemblies and their contexts
    /// </summary>
    public void UnloadAll()
    {
        this.logger.LogInformation("Unloading all assemblies");

        foreach (var context in this.loadContexts.Values)
        {
            try
            {
                context.Unload();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Error unloading assembly context");
            }
        }

        this.loadContexts.Clear();
        this.loadedAssemblies.Clear();
    }

    /// <summary>
    /// Disposes the assembly loader and unloads all assemblies
    /// </summary>
    public void Dispose()
    {
        this.logger.LogDebug("Disposing AssemblyLoader");
        UnloadAll();
        GC.SuppressFinalize(this);
    }
}
