using System.Reflection;
using DotNetApiDiff.Models;

namespace DotNetApiDiff.Interfaces;

/// <summary>
/// Interface for extracting public API members from .NET assemblies
/// </summary>
public interface IApiExtractor
{
    /// <summary>
    /// Extracts all public API members from the specified assembly
    /// </summary>
    /// <param name="assembly">Assembly to extract API members from</param>
    /// <returns>Collection of public API members</returns>
    IEnumerable<ApiMember> ExtractApiMembers(Assembly assembly);

    /// <summary>
    /// Extracts public API members from a specific type
    /// </summary>
    /// <param name="type">Type to extract members from</param>
    /// <returns>Collection of public API members for the type</returns>
    IEnumerable<ApiMember> ExtractTypeMembers(Type type);

    /// <summary>
    /// Gets all public types from the specified assembly
    /// </summary>
    /// <param name="assembly">Assembly to get types from</param>
    /// <returns>Collection of public types</returns>
    IEnumerable<Type> GetPublicTypes(Assembly assembly);
}
