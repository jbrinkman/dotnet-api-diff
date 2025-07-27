// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
namespace DotNetApiDiff.Interfaces;

/// <summary>
/// Interface for loading .NET assemblies from file paths
/// </summary>
public interface IAssemblyLoader
{
    /// <summary>
    /// Loads an assembly from the specified file path
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file</param>
    /// <returns>Loaded assembly</returns>
    /// <exception cref="FileNotFoundException">Thrown when assembly file is not found</exception>
    /// <exception cref="BadImageFormatException">Thrown when assembly file is invalid</exception>
    System.Reflection.Assembly LoadAssembly(string assemblyPath);

    /// <summary>
    /// Validates that the specified path contains a valid .NET assembly
    /// </summary>
    /// <param name="assemblyPath">Path to validate</param>
    /// <returns>True if path contains a valid assembly, false otherwise</returns>
    bool IsValidAssembly(string assemblyPath);
}
