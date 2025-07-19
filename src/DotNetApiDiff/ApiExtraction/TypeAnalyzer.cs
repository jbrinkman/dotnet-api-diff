// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.Reflection;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using Microsoft.Extensions.Logging;

namespace DotNetApiDiff.ApiExtraction;

/// <summary>
/// Analyzes .NET types and their members to extract API information
/// </summary>
public class TypeAnalyzer : ITypeAnalyzer
{
    private readonly IMemberSignatureBuilder _signatureBuilder;
    private readonly ILogger<TypeAnalyzer> _logger;

    /// <summary>
    /// Creates a new instance of the TypeAnalyzer
    /// </summary>
    /// <param name="signatureBuilder">Signature builder for creating normalized signatures</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public TypeAnalyzer(IMemberSignatureBuilder signatureBuilder, ILogger<TypeAnalyzer> logger)
    {
        _signatureBuilder = signatureBuilder ?? throw new ArgumentNullException(nameof(signatureBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes a type and returns its API member representation
    /// </summary>
    /// <param name="type">Type to analyze</param>
    /// <returns>ApiMember representing the type</returns>
    public ApiMember AnalyzeType(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        try
        {
            var member = new ApiMember
            {
                Name = type.Name,
                FullName = type.FullName ?? type.Name,
                Namespace = type.Namespace ?? string.Empty,
                Signature = _signatureBuilder.BuildTypeSignature(type),
                Attributes = GetTypeAttributes(type),
                Type = GetMemberType(type),
                Accessibility = GetTypeAccessibility(type)
            };

            return member;
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing type {TypeName}", type.Name);
            throw;
        }
    }

    /// <summary>
    /// Analyzes all methods of a type
    /// </summary>
    /// <param name="type">Type to analyze methods for</param>
    /// <returns>Collection of method API members</returns>
    public IEnumerable<ApiMember> AnalyzeMethods(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        try
        {
            var methods = new List<ApiMember>();

            // Get all methods, excluding property accessors, event accessors, and constructors
            var methodInfos = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                             BindingFlags.Instance | BindingFlags.Static |
                                             BindingFlags.DeclaredOnly)
                                  .Where(m => !m.IsSpecialName);

            foreach (var method in methodInfos)
            {
                // Skip non-public methods unless they're overrides of public methods
                if (!IsPublicOrOverride(method))
                {
                    continue;
                }

                var member = new ApiMember
                {
                    Name = method.Name,
                    FullName = $"{type.FullName}.{method.Name}",
                    Namespace = type.Namespace ?? string.Empty,
                    DeclaringType = type.FullName ?? type.Name,
                    Signature = _signatureBuilder.BuildMethodSignature(method),
                    Attributes = GetMemberAttributes(method),
                    Type = MemberType.Method,
                    Accessibility = GetMethodAccessibility(method)
                };

                methods.Add(member);
            }

            return methods;
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing methods for type {TypeName}", type.Name);
            return Enumerable.Empty<ApiMember>();
        }
    }

    /// <summary>
    /// Analyzes all properties of a type
    /// </summary>
    /// <param name="type">Type to analyze properties for</param>
    /// <returns>Collection of property API members</returns>
    public IEnumerable<ApiMember> AnalyzeProperties(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        try
        {
            var properties = new List<ApiMember>();

            var propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                                 BindingFlags.Instance | BindingFlags.Static |
                                                 BindingFlags.DeclaredOnly);

            foreach (var property in propertyInfos)
            {
                // Skip non-public properties unless they're overrides of public properties
                if (!IsPublicOrOverrideProperty(property))
                {
                    continue;
                }

                var member = new ApiMember
                {
                    Name = property.Name,
                    FullName = $"{type.FullName}.{property.Name}",
                    Namespace = type.Namespace ?? string.Empty,
                    DeclaringType = type.FullName ?? type.Name,
                    Signature = _signatureBuilder.BuildPropertySignature(property),
                    Attributes = GetMemberAttributes(property),
                    Type = MemberType.Property,
                    Accessibility = GetPropertyAccessibility(property)
                };

                properties.Add(member);
            }

            return properties;
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing properties for type {TypeName}", type.Name);
            return Enumerable.Empty<ApiMember>();
        }
    }

    /// <summary>
    /// Analyzes all fields of a type
    /// </summary>
    /// <param name="type">Type to analyze fields for</param>
    /// <returns>Collection of field API members</returns>
    public IEnumerable<ApiMember> AnalyzeFields(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        try
        {
            var fields = new List<ApiMember>();

            var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.Instance | BindingFlags.Static |
                                          BindingFlags.DeclaredOnly);

            foreach (var field in fieldInfos)
            {
                // Skip non-public fields and compiler-generated backing fields
                if (!field.IsPublic || field.Name.StartsWith("<"))
                {
                    continue;
                }

                var member = new ApiMember
                {
                    Name = field.Name,
                    FullName = $"{type.FullName}.{field.Name}",
                    Namespace = type.Namespace ?? string.Empty,
                    DeclaringType = type.FullName ?? type.Name,
                    Signature = _signatureBuilder.BuildFieldSignature(field),
                    Attributes = GetMemberAttributes(field),
                    Type = MemberType.Field,
                    Accessibility = GetFieldAccessibility(field)
                };

                fields.Add(member);
            }

            return fields;
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing fields for type {TypeName}", type.Name);
            return Enumerable.Empty<ApiMember>();
        }
    }

    /// <summary>
    /// Analyzes all events of a type
    /// </summary>
    /// <param name="type">Type to analyze events for</param>
    /// <returns>Collection of event API members</returns>
    public IEnumerable<ApiMember> AnalyzeEvents(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        try
        {
            var events = new List<ApiMember>();

            var eventInfos = type.GetEvents(BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.Instance | BindingFlags.Static |
                                          BindingFlags.DeclaredOnly);

            foreach (var eventInfo in eventInfos)
            {
                // Skip non-public events unless they're overrides of public events
                if (!IsPublicOrOverrideEvent(eventInfo))
                {
                    continue;
                }

                var member = new ApiMember
                {
                    Name = eventInfo.Name,
                    FullName = $"{type.FullName}.{eventInfo.Name}",
                    Namespace = type.Namespace ?? string.Empty,
                    DeclaringType = type.FullName ?? type.Name,
                    Signature = _signatureBuilder.BuildEventSignature(eventInfo),
                    Attributes = GetMemberAttributes(eventInfo),
                    Type = MemberType.Event,
                    Accessibility = GetEventAccessibility(eventInfo)
                };

                events.Add(member);
            }

            return events;
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing events for type {TypeName}", type.Name);
            return Enumerable.Empty<ApiMember>();
        }
    }

    /// <summary>
    /// Analyzes all constructors of a type
    /// </summary>
    /// <param name="type">Type to analyze constructors for</param>
    /// <returns>Collection of constructor API members</returns>
    public IEnumerable<ApiMember> AnalyzeConstructors(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        try
        {
            var constructors = new List<ApiMember>();

            var constructorInfos = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic |
                                                      BindingFlags.Instance | BindingFlags.Static |
                                                      BindingFlags.DeclaredOnly);

            foreach (var constructor in constructorInfos)
            {
                // Skip non-public constructors
                if (!constructor.IsPublic && !constructor.IsFamily)
                {
                    continue;
                }

                var member = new ApiMember
                {
                    Name = constructor.Name,
                    FullName = $"{type.FullName}.{constructor.Name}",
                    Namespace = type.Namespace ?? string.Empty,
                    DeclaringType = type.FullName ?? type.Name,
                    Signature = _signatureBuilder.BuildConstructorSignature(constructor),
                    Attributes = GetMemberAttributes(constructor),
                    Type = MemberType.Constructor,
                    Accessibility = GetMethodAccessibility(constructor)
                };

                constructors.Add(member);
            }

            return constructors;
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing constructors for type {TypeName}", type.Name);
            return Enumerable.Empty<ApiMember>();
        }
    }

    /// <summary>
    /// Gets the member type for a .NET type
    /// </summary>
    /// <param name="type">Type to get member type for</param>
    /// <returns>Member type</returns>
    private MemberType GetMemberType(Type type)
    {
        if (type.IsInterface)
        {
            return MemberType.Interface;
        }

        else if (type.IsEnum)
        {
            return MemberType.Enum;
        }

        else if (type.IsValueType)
        {
            return MemberType.Struct;
        }

        else if (type.IsSubclassOf(typeof(MulticastDelegate)))
        {
            return MemberType.Delegate;
        }

        else
        {
            return MemberType.Class;
        }
    }

    /// <summary>
    /// Gets the accessibility level for a type
    /// </summary>
    /// <param name="type">Type to get accessibility for</param>
    /// <returns>Accessibility level</returns>
    private AccessibilityLevel GetTypeAccessibility(Type type)
    {
        if (type.IsNestedPublic || type.IsPublic)
        {
            return AccessibilityLevel.Public;
        }

        else if (type.IsNestedFamily)
        {
            return AccessibilityLevel.Protected;
        }

        else if (type.IsNestedFamORAssem)
        {
            return AccessibilityLevel.ProtectedInternal;
        }

        else if (type.IsNestedFamANDAssem)
        {
            return AccessibilityLevel.ProtectedPrivate;
        }

        else if (type.IsNestedAssembly || type.IsNotPublic)
        {
            return AccessibilityLevel.Internal;
        }

        else
        {
            return AccessibilityLevel.Private;
        }
    }

    /// <summary>
    /// Gets the accessibility level for a method or constructor
    /// </summary>
    /// <param name="method">Method to get accessibility for</param>
    /// <returns>Accessibility level</returns>
    private AccessibilityLevel GetMethodAccessibility(MethodBase method)
    {
        if (method.IsPublic)
        {
            return AccessibilityLevel.Public;
        }

        else if (method.IsFamily)
        {
            return AccessibilityLevel.Protected;
        }

        else if (method.IsFamilyOrAssembly)
        {
            return AccessibilityLevel.ProtectedInternal;
        }

        else if (method.IsFamilyAndAssembly)
        {
            return AccessibilityLevel.ProtectedPrivate;
        }

        else if (method.IsAssembly)
        {
            return AccessibilityLevel.Internal;
        }

        else
        {
            return AccessibilityLevel.Private;
        }
    }

    /// <summary>
    /// Gets the accessibility level for a property
    /// </summary>
    /// <param name="property">Property to get accessibility for</param>
    /// <returns>Accessibility level</returns>
    private AccessibilityLevel GetPropertyAccessibility(PropertyInfo property)
    {
        var getMethod = property.GetMethod;
        var setMethod = property.SetMethod;

        // Use the most accessible between get and set
        if (getMethod != null && setMethod != null)
        {
            var getAccess = GetMethodAccessibility(getMethod);
            var setAccess = GetMethodAccessibility(setMethod);
            return GetMostAccessible(getAccess, setAccess);
        }

        else if (getMethod != null)
        {
            return GetMethodAccessibility(getMethod);
        }

        else if (setMethod != null)
        {
            return GetMethodAccessibility(setMethod);
        }

        return AccessibilityLevel.Private;
    }

    /// <summary>
    /// Gets the accessibility level for a field
    /// </summary>
    /// <param name="field">Field to get accessibility for</param>
    /// <returns>Accessibility level</returns>
    private AccessibilityLevel GetFieldAccessibility(FieldInfo field)
    {
        if (field.IsPublic)
        {
            return AccessibilityLevel.Public;
        }

        else if (field.IsFamily)
        {
            return AccessibilityLevel.Protected;
        }

        else if (field.IsFamilyOrAssembly)
        {
            return AccessibilityLevel.ProtectedInternal;
        }

        else if (field.IsFamilyAndAssembly)
        {
            return AccessibilityLevel.ProtectedPrivate;
        }

        else if (field.IsAssembly)
        {
            return AccessibilityLevel.Internal;
        }

        else
        {
            return AccessibilityLevel.Private;
        }
    }

    /// <summary>
    /// Gets the accessibility level for an event
    /// </summary>
    /// <param name="eventInfo">Event to get accessibility for</param>
    /// <returns>Accessibility level</returns>
    private AccessibilityLevel GetEventAccessibility(EventInfo eventInfo)
    {
        var addMethod = eventInfo.AddMethod;

        if (addMethod != null)
        {
            return GetMethodAccessibility(addMethod);
        }

        return AccessibilityLevel.Private;
    }

    /// <summary>
    /// Gets the most accessible of two accessibility levels
    /// </summary>
    /// <param name="access1">First accessibility level</param>
    /// <param name="access2">Second accessibility level</param>
    /// <returns>Most accessible level</returns>
    private AccessibilityLevel GetMostAccessible(AccessibilityLevel access1, AccessibilityLevel access2)
    {
        return CompareAccessibilityLevels(access1, access2);
    }

    /// <summary>
    /// Compares two accessibility levels and returns the more accessible one
    /// </summary>
    /// <param name="access1">First accessibility level</param>
    /// <param name="access2">Second accessibility level</param>
    /// <returns>Most accessible level</returns>
    private AccessibilityLevel CompareAccessibilityLevels(AccessibilityLevel access1, AccessibilityLevel access2)
    {
        if (access1 == AccessibilityLevel.Public || access2 == AccessibilityLevel.Public)
        {
            return AccessibilityLevel.Public;
        }

        if (access1 == AccessibilityLevel.Protected || access2 == AccessibilityLevel.Protected)
        {
            return AccessibilityLevel.Protected;
        }

        if (access1 == AccessibilityLevel.Internal || access2 == AccessibilityLevel.Internal)
        {
            return AccessibilityLevel.Internal;
        }

        return AccessibilityLevel.Private;
    }

    /// <summary>
    /// Gets the attributes applied to a type
    /// </summary>
    /// <param name="type">Type to get attributes for</param>
    /// <returns>List of attribute names</returns>
    private List<string> GetTypeAttributes(Type type)
    {
        try
        {
            return type.GetCustomAttributes(true)
                .Where(a => !IsCompilerGeneratedAttribute(a))
                .Select(a => a.GetType().Name)
                .ToList();
        }

        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting attributes for type {TypeName}", type.Name);
            return new List<string>();
        }
    }

    /// <summary>
    /// Gets the attributes applied to a member
    /// </summary>
    /// <param name="member">Member to get attributes for</param>
    /// <returns>List of attribute names</returns>
    private List<string> GetMemberAttributes(MemberInfo member)
    {
        try
        {
            return member.GetCustomAttributes(true)
                .Where(a => !IsCompilerGeneratedAttribute(a))
                .Select(a => a.GetType().Name)
                .ToList();
        }

        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting attributes for member {MemberName}", member.Name);
            return new List<string>();
        }
    }

    /// <summary>
    /// Checks if an attribute is compiler-generated
    /// </summary>
    /// <param name="attribute">Attribute to check</param>
    /// <returns>True if compiler-generated, false otherwise</returns>
    private bool IsCompilerGeneratedAttribute(object attribute)
    {
        var typeName = attribute.GetType().Name;
        return typeName == "CompilerGeneratedAttribute" ||
               typeName == "DebuggerHiddenAttribute" ||
               typeName == "DebuggerNonUserCodeAttribute";
    }

    /// <summary>
    /// Checks if a method is public or an override of a public method
    /// </summary>
    /// <param name="method">Method to check</param>
    /// <returns>True if public or override, false otherwise</returns>
    private bool IsPublicOrOverride(MethodInfo method)
    {
        // If the method is public, include it
        if (method.IsPublic)
        {
            return true;
        }

        // If the method is an override, check if it's overriding a public method
        if (method.GetBaseDefinition() != method)
        {
            var baseMethod = method.GetBaseDefinition();
            if (baseMethod != null && baseMethod.IsPublic)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a property is public or an override of a public property
    /// </summary>
    /// <param name="property">Property to check</param>
    /// <returns>True if public or override, false otherwise</returns>
    private bool IsPublicOrOverrideProperty(PropertyInfo property)
    {
        var getMethod = property.GetMethod;
        var setMethod = property.SetMethod;

        // If either accessor is public, include the property
        if ((getMethod != null && getMethod.IsPublic) || (setMethod != null && setMethod.IsPublic))
        {
            return true;
        }

        // Check if either accessor is overriding a public accessor
        if ((getMethod != null && IsPublicOrOverride(getMethod)) ||
            (setMethod != null && IsPublicOrOverride(setMethod)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if an event is public or an override of a public event
    /// </summary>
    /// <param name="eventInfo">Event to check</param>
    /// <returns>True if public or override, false otherwise</returns>
    private bool IsPublicOrOverrideEvent(EventInfo eventInfo)
    {
        var addMethod = eventInfo.AddMethod;
        var removeMethod = eventInfo.RemoveMethod;

        // If either accessor is public, include the event
        if ((addMethod != null && addMethod.IsPublic) || (removeMethod != null && removeMethod.IsPublic))
        {
            return true;
        }

        // Check if either accessor is overriding a public accessor
        if ((addMethod != null && IsPublicOrOverride(addMethod)) ||
            (removeMethod != null && IsPublicOrOverride(removeMethod)))
        {
            return true;
        }

        return false;
    }
}
