using System.Reflection;
using DotNetApiDiff.Models;

namespace DotNetApiDiff.Interfaces;

/// <summary>
/// Interface for analyzing individual types and their members
/// </summary>
public interface ITypeAnalyzer
{
    /// <summary>
    /// Analyzes a type and returns its API member representation
    /// </summary>
    /// <param name="type">Type to analyze</param>
    /// <returns>ApiMember representing the type</returns>
    ApiMember AnalyzeType(Type type);
    
    /// <summary>
    /// Analyzes all methods of a type
    /// </summary>
    /// <param name="type">Type to analyze methods for</param>
    /// <returns>Collection of method API members</returns>
    IEnumerable<ApiMember> AnalyzeMethods(Type type);
    
    /// <summary>
    /// Analyzes all properties of a type
    /// </summary>
    /// <param name="type">Type to analyze properties for</param>
    /// <returns>Collection of property API members</returns>
    IEnumerable<ApiMember> AnalyzeProperties(Type type);
    
    /// <summary>
    /// Analyzes all fields of a type
    /// </summary>
    /// <param name="type">Type to analyze fields for</param>
    /// <returns>Collection of field API members</returns>
    IEnumerable<ApiMember> AnalyzeFields(Type type);
    
    /// <summary>
    /// Analyzes all events of a type
    /// </summary>
    /// <param name="type">Type to analyze events for</param>
    /// <returns>Collection of event API members</returns>
    IEnumerable<ApiMember> AnalyzeEvents(Type type);
    
    /// <summary>
    /// Analyzes all constructors of a type
    /// </summary>
    /// <param name="type">Type to analyze constructors for</param>
    /// <returns>Collection of constructor API members</returns>
    IEnumerable<ApiMember> AnalyzeConstructors(Type type);
}