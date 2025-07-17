using System.Reflection;

namespace DotNetApiDiff.Interfaces;

/// <summary>
/// Interface for building normalized signatures for API members
/// </summary>
public interface IMemberSignatureBuilder
{
    /// <summary>
    /// Builds a normalized signature for a method
    /// </summary>
    /// <param name="method">Method to build signature for</param>
    /// <returns>Normalized method signature</returns>
    string BuildMethodSignature(MethodInfo method);
    
    /// <summary>
    /// Builds a normalized signature for a property
    /// </summary>
    /// <param name="property">Property to build signature for</param>
    /// <returns>Normalized property signature</returns>
    string BuildPropertySignature(PropertyInfo property);
    
    /// <summary>
    /// Builds a normalized signature for a field
    /// </summary>
    /// <param name="field">Field to build signature for</param>
    /// <returns>Normalized field signature</returns>
    string BuildFieldSignature(FieldInfo field);
    
    /// <summary>
    /// Builds a normalized signature for an event
    /// </summary>
    /// <param name="eventInfo">Event to build signature for</param>
    /// <returns>Normalized event signature</returns>
    string BuildEventSignature(EventInfo eventInfo);
    
    /// <summary>
    /// Builds a normalized signature for a constructor
    /// </summary>
    /// <param name="constructor">Constructor to build signature for</param>
    /// <returns>Normalized constructor signature</returns>
    string BuildConstructorSignature(ConstructorInfo constructor);
    
    /// <summary>
    /// Builds a normalized signature for a type
    /// </summary>
    /// <param name="type">Type to build signature for</param>
    /// <returns>Normalized type signature</returns>
    string BuildTypeSignature(Type type);
}