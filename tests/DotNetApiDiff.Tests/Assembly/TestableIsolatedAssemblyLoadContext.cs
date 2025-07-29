using System.Reflection;
using System.Runtime.Loader;
using DotNetApiDiff.AssemblyLoading;
using Microsoft.Extensions.Logging;

namespace DotNetApiDiff.Tests.Assembly;

/// <summary>
/// Testable wrapper for IsolatedAssemblyLoadContext that exposes protected methods for testing
/// </summary>
public class TestableIsolatedAssemblyLoadContext : IsolatedAssemblyLoadContext
{
    public TestableIsolatedAssemblyLoadContext(string mainAssemblyPath, ILogger? logger = null)
        : base(mainAssemblyPath, logger)
    {
    }

    /// <summary>
    /// Exposes the protected Load method for testing
    /// </summary>
    public new System.Reflection.Assembly? Load(AssemblyName assemblyName) => base.Load(assemblyName);

    /// <summary>
    /// Exposes the protected LoadUnmanagedDll method for testing
    /// </summary>
    public new IntPtr LoadUnmanagedDll(string unmanagedDllName) => base.LoadUnmanagedDll(unmanagedDllName);
}
