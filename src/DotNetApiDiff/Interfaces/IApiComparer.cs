using System.Reflection;
using DotNetApiDiff.Models;

namespace DotNetApiDiff.Interfaces;

/// <summary>
/// Interface for comparing APIs between two .NET assemblies
/// </summary>
public interface IApiComparer
{
    /// <summary>
    /// Compares the public APIs of two assemblies and returns the differences
    /// </summary>
    /// <param name="oldAssembly">The original assembly</param>
    /// <param name="newAssembly">The new assembly to compare against</param>
    /// <returns>Comparison result containing all detected differences</returns>
    ComparisonResult CompareAssemblies(Assembly oldAssembly, Assembly newAssembly);
    
    /// <summary>
    /// Compares types between two assemblies
    /// </summary>
    /// <param name="oldTypes">Types from the original assembly</param>
    /// <param name="newTypes">Types from the new assembly</param>
    /// <returns>List of type-level differences</returns>
    IEnumerable<ApiDifference> CompareTypes(IEnumerable<Type> oldTypes, IEnumerable<Type> newTypes);
    
    /// <summary>
    /// Compares members (methods, properties, fields) of two types
    /// </summary>
    /// <param name="oldType">Original type</param>
    /// <param name="newType">New type to compare against</param>
    /// <returns>List of member-level differences</returns>
    IEnumerable<ApiDifference> CompareMembers(Type oldType, Type newType);
}