// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.Reflection;
using System.Text;
using DotNetApiDiff.Interfaces;
using Microsoft.Extensions.Logging;

namespace DotNetApiDiff.ApiExtraction;

/// <summary>
/// Builds normalized signatures for API members to enable consistent comparison
/// </summary>
public class MemberSignatureBuilder : IMemberSignatureBuilder
{
    private readonly ILogger<MemberSignatureBuilder> _logger;

    /// <summary>
    /// Creates a new instance of the MemberSignatureBuilder
    /// </summary>
    /// <param name="logger">Logger for diagnostic information</param>
    public MemberSignatureBuilder(ILogger<MemberSignatureBuilder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds a normalized signature for a method
    /// </summary>
    /// <param name="method">Method to build signature for</param>
    /// <returns>Normalized method signature</returns>
    public string BuildMethodSignature(MethodInfo method)
    {
        if (method == null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        try
        {
            var signature = new StringBuilder();

            // Add accessibility
            signature.Append(GetAccessibilityString(method));
            signature.Append(' ');

            // Add static/virtual/abstract modifiers
            if (method.IsStatic)
            {
                signature.Append("static ");
            }
            else if (method.IsVirtual && !method.IsFinal)
            {
                if (method.IsAbstract)
                {
                    signature.Append("abstract ");
                }
                else if (method.DeclaringType?.IsInterface == false)
                {
                    signature.Append("virtual ");
                }
            }
            else if (method.IsFinal && method.IsVirtual)
            {
                signature.Append("sealed override ");
            }

            // Add return type
            signature.Append(GetTypeName(method.ReturnType));
            signature.Append(' ');

            // Add method name
            signature.Append(method.Name);

            // Add generic type parameters if any
            if (method.IsGenericMethod)
            {
                signature.Append('<');
                var genericArgs = method.GetGenericArguments();
                for (int i = 0; i < genericArgs.Length; i++)
                {
                    if (i > 0)
                    {
                        signature.Append(", ");
                    }
                    signature.Append(GetGenericParameterName(genericArgs[i]));
                }
                signature.Append('>');
            }

            // Add parameters
            signature.Append('(');
            var parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    signature.Append(", ");
                }
                signature.Append(BuildParameterSignature(parameters[i]));
            }
            signature.Append(')');

            return signature.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building signature for method {MethodName}", method.Name);
            return $"Error: {method.Name}";
        }
    }

    /// <summary>
    /// Builds a normalized signature for a property
    /// </summary>
    /// <param name="property">Property to build signature for</param>
    /// <returns>Normalized property signature</returns>
    public string BuildPropertySignature(PropertyInfo property)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        try
        {
            var signature = new StringBuilder();

            // Get the most accessible accessor between getter and setter
            var getMethod = property.GetMethod;
            var setMethod = property.SetMethod;

            // Add accessibility
            if (getMethod != null && setMethod != null)
            {
                // Use the most accessible between get and set
                var getAccess = GetAccessibilityString(getMethod);
                var setAccess = GetAccessibilityString(setMethod);
                signature.Append(GetMostAccessible(getAccess, setAccess));
            }
            else if (getMethod != null)
            {
                signature.Append(GetAccessibilityString(getMethod));
            }
            else if (setMethod != null)
            {
                signature.Append(GetAccessibilityString(setMethod));
            }
            else
            {
                signature.Append("private");
            }
            signature.Append(' ');

            // Add static modifier if applicable
            if ((getMethod != null && getMethod.IsStatic) || (setMethod != null && setMethod.IsStatic))
            {
                signature.Append("static ");
            }

            // Add virtual/abstract modifiers
            if (getMethod != null && getMethod.IsVirtual && !getMethod.IsFinal)
            {
                if (getMethod.IsAbstract)
                {
                    signature.Append("abstract ");
                }
                else if (property.DeclaringType?.IsInterface == false)
                {
                    signature.Append("virtual ");
                }
            }
            else if (getMethod != null && getMethod.IsFinal && getMethod.IsVirtual)
            {
                signature.Append("sealed override ");
            }

            // Add property type
            signature.Append(GetTypeName(property.PropertyType));
            signature.Append(' ');

            // Add property name
            signature.Append(property.Name);

            // Add accessors
            signature.Append(" { ");
            if (getMethod != null)
            {
                signature.Append("get; ");
            }
            if (setMethod != null)
            {
                signature.Append("set; ");
            }
            signature.Append('}');

            return signature.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building signature for property {PropertyName}", property.Name);
            return $"Error: {property.Name}";
        }
    }

    /// <summary>
    /// Builds a normalized signature for a field
    /// </summary>
    /// <param name="field">Field to build signature for</param>
    /// <returns>Normalized field signature</returns>
    public string BuildFieldSignature(FieldInfo field)
    {
        if (field == null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        try
        {
            var signature = new StringBuilder();

            // Add accessibility
            signature.Append(GetAccessibilityString(field));
            signature.Append(' ');

            // Add static/readonly/const modifiers
            if (field.IsStatic)
            {
                signature.Append("static ");
            }
            if (field.IsInitOnly)
            {
                signature.Append("readonly ");
            }
            if (field.IsLiteral)
            {
                signature.Append("const ");
            }

            // Add field type
            signature.Append(GetTypeName(field.FieldType));
            signature.Append(' ');

            // Add field name
            signature.Append(field.Name);

            return signature.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building signature for field {FieldName}", field.Name);
            return $"Error: {field.Name}";
        }
    }

    /// <summary>
    /// Builds a normalized signature for an event
    /// </summary>
    /// <param name="eventInfo">Event to build signature for</param>
    /// <returns>Normalized event signature</returns>
    public string BuildEventSignature(EventInfo eventInfo)
    {
        if (eventInfo == null)
        {
            throw new ArgumentNullException(nameof(eventInfo));
        }

        try
        {
            var signature = new StringBuilder();

            // Get the add method to determine accessibility
            var addMethod = eventInfo.AddMethod;

            // Add accessibility
            if (addMethod != null)
            {
                signature.Append(GetAccessibilityString(addMethod));
                signature.Append(' ');

                // Add static modifier if applicable
                if (addMethod.IsStatic)
                {
                    signature.Append("static ");
                }

                // Add virtual/abstract modifiers
                if (addMethod.IsVirtual && !addMethod.IsFinal)
                {
                    if (addMethod.IsAbstract)
                    {
                        signature.Append("abstract ");
                    }
                    else if (eventInfo.DeclaringType?.IsInterface == false)
                    {
                        signature.Append("virtual ");
                    }
                }
                else if (addMethod.IsFinal && addMethod.IsVirtual)
                {
                    signature.Append("sealed override ");
                }
            }

            // Add event keyword and event handler type
            signature.Append("event ");
            signature.Append(GetTypeName(eventInfo.EventHandlerType ?? typeof(object)));
            signature.Append(' ');

            // Add event name
            signature.Append(eventInfo.Name);

            return signature.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building signature for event {EventName}", eventInfo.Name);
            return $"Error: {eventInfo.Name}";
        }
    }

    /// <summary>
    /// Builds a normalized signature for a constructor
    /// </summary>
    /// <param name="constructor">Constructor to build signature for</param>
    /// <returns>Normalized constructor signature</returns>
    public string BuildConstructorSignature(ConstructorInfo constructor)
    {
        if (constructor == null)
        {
            throw new ArgumentNullException(nameof(constructor));
        }

        try
        {
            var signature = new StringBuilder();

            // Add accessibility
            signature.Append(GetAccessibilityString(constructor));
            signature.Append(' ');

            // Add static modifier for static constructors
            if (constructor.IsStatic)
            {
                signature.Append("static ");
            }

            // Add constructor name (use declaring type name)
            var typeName = constructor.DeclaringType?.Name ?? "Unknown";
            if (typeName.Contains('`'))
            {
                typeName = typeName.Substring(0, typeName.IndexOf('`'));
            }
            signature.Append(typeName);

            // Add parameters
            signature.Append('(');
            var parameters = constructor.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    signature.Append(", ");
                }
                signature.Append(BuildParameterSignature(parameters[i]));
            }
            signature.Append(')');

            return signature.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building signature for constructor in type {TypeName}",
                constructor.DeclaringType?.Name ?? "Unknown");
            return $"Error: Constructor in {constructor.DeclaringType?.Name ?? "Unknown"}";
        }
    }

    /// <summary>
    /// Builds a normalized signature for a type
    /// </summary>
    /// <param name="type">Type to build signature for</param>
    /// <returns>Normalized type signature</returns>
    public string BuildTypeSignature(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        try
        {
            var signature = new StringBuilder();

            // Add accessibility
            signature.Append(GetTypeAccessibilityString(type));
            signature.Append(' ');

            // Add sealed/abstract modifiers
            if (type.IsSealed && !type.IsValueType && !type.IsEnum)
            {
                if (type.IsAbstract)
                {
                    signature.Append("static ");
                }
                else
                {
                    signature.Append("sealed ");
                }
            }
            else if (type.IsAbstract && !type.IsInterface)
            {
                signature.Append("abstract ");
            }

            // Add type kind (class, struct, interface, enum, delegate)
            if (type.IsInterface)
            {
                signature.Append("interface ");
            }
            else if (type.IsEnum)
            {
                signature.Append("enum ");
            }
            else if (type.IsValueType)
            {
                signature.Append("struct ");
            }
            else if (type.IsSubclassOf(typeof(MulticastDelegate)))
            {
                signature.Append("delegate ");
            }
            else
            {
                signature.Append("class ");
            }

            // Add type name
            signature.Append(type.Name);

            // Add generic parameters if any
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // For constructed generic types, include the type arguments
                var genericArgs = type.GetGenericArguments();
                signature.Append('<');
                for (int i = 0; i < genericArgs.Length; i++)
                {
                    if (i > 0)
                    {
                        signature.Append(", ");
                    }
                    signature.Append(GetTypeName(genericArgs[i]));
                }
                signature.Append('>');
            }
            else if (type.IsGenericTypeDefinition)
            {
                // For generic type definitions, include the type parameter names
                var genericArgs = type.GetGenericArguments();
                signature.Append('<');
                for (int i = 0; i < genericArgs.Length; i++)
                {
                    if (i > 0)
                    {
                        signature.Append(", ");
                    }
                    signature.Append(GetGenericParameterName(genericArgs[i]));
                }
                signature.Append('>');
            }

            // Add base type if not Object or ValueType
            if (type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType) &&
                type.BaseType != typeof(Enum))
            {
                signature.Append(" : ");
                signature.Append(GetTypeName(type.BaseType));
            }

            // Add implemented interfaces
            var interfaces = type.GetInterfaces();
            if (interfaces.Length > 0 && !type.IsInterface)
            {
                if (type.BaseType == null || type.BaseType == typeof(object) || type.BaseType == typeof(ValueType) ||
                    type.BaseType == typeof(Enum))
                {
                    signature.Append(" : ");
                }
                else
                {
                    signature.Append(", ");
                }

                for (int i = 0; i < interfaces.Length; i++)
                {
                    if (i > 0)
                    {
                        signature.Append(", ");
                    }
                    signature.Append(GetTypeName(interfaces[i]));
                }
            }
            else if (type.IsInterface && interfaces.Length > 0)
            {
                // For interfaces, show base interfaces
                signature.Append(" : ");
                for (int i = 0; i < interfaces.Length; i++)
                {
                    if (i > 0)
                    {
                        signature.Append(", ");
                    }
                    signature.Append(GetTypeName(interfaces[i]));
                }
            }

            return signature.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building signature for type {TypeName}", type.Name);
            return $"Error: {type.Name}";
        }
    }

    /// <summary>
    /// Builds a signature for a method parameter
    /// </summary>
    /// <param name="parameter">Parameter to build signature for</param>
    /// <returns>Parameter signature</returns>
    private string BuildParameterSignature(ParameterInfo parameter)
    {
        var signature = new StringBuilder();

        // Add parameter modifiers
        if (parameter.IsOut)
        {
            signature.Append("out ");
        }
        else if (parameter.ParameterType.IsByRef)
        {
            signature.Append("ref ");
        }
        else if (parameter.IsDefined(typeof(ParamArrayAttribute), false))
        {
            signature.Append("params ");
        }

        // Add parameter type
        var paramType = parameter.ParameterType;
        if (paramType.IsByRef)
        {
            paramType = paramType.GetElementType() ?? paramType;
        }
        signature.Append(GetTypeName(paramType));

        // Add parameter name
        signature.Append(' ');
        signature.Append(parameter.Name);

        // Add default value if parameter is optional
        if (parameter.HasDefaultValue)
        {
            signature.Append(" = ");
            if (parameter.DefaultValue == null)
            {
                signature.Append("null");
            }
            else if (parameter.DefaultValue is string stringValue)
            {
                signature.Append($"\"{stringValue}\"");
            }
            else
            {
                signature.Append(parameter.DefaultValue);
            }
        }

        return signature.ToString();
    }

    /// <summary>
    /// Gets a string representation of a type name
    /// </summary>
    /// <param name="type">Type to get name for</param>
    /// <returns>Type name</returns>
    private string GetTypeName(Type type)
    {
        if (type == null)
        {
            return "void";
        }

        // Handle void return type
        if (type == typeof(void))
        {
            return "void";
        }

        // Handle by-ref types
        if (type.IsByRef)
        {
            return GetTypeName(type.GetElementType() ?? type);
        }

        // Handle array types
        if (type.IsArray)
        {
            var elementType = type.GetElementType() ?? type;
            var rank = type.GetArrayRank();
            if (rank == 1)
            {
                return $"{GetTypeName(elementType)}[]";
            }
            else
            {
                var commas = new string(',', rank - 1);
                return $"{GetTypeName(elementType)}[{commas}]";
            }
        }

        // Handle pointer types
        if (type.IsPointer)
        {
            var elementType = type.GetElementType() ?? type;
            return $"{GetTypeName(elementType)}*";
        }

        // Handle generic type parameters
        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        // Handle primitive types with C# keywords
        if (type == typeof(bool)) return "bool";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(sbyte)) return "sbyte";
        if (type == typeof(char)) return "char";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(int)) return "int";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(long)) return "long";
        if (type == typeof(ulong)) return "ulong";
        if (type == typeof(short)) return "short";
        if (type == typeof(ushort)) return "ushort";
        if (type == typeof(string)) return "string";
        if (type == typeof(object)) return "object";

        // Handle generic types
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();
            var typeName = genericTypeDef.Name;

            // Remove the `n suffix from generic type names
            if (typeName.Contains('`'))
            {
                typeName = typeName.Substring(0, typeName.IndexOf('`'));
            }

            // Handle common generic collections
            if (genericTypeDef == typeof(Nullable<>))
            {
                return $"{GetTypeName(genericArgs[0])}?";
            }

            // For other generic types, use the standard format
            var sb = new StringBuilder();
            sb.Append(typeName);
            sb.Append('<');

            for (int i = 0; i < genericArgs.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(GetTypeName(genericArgs[i]));
            }

            sb.Append('>');
            return sb.ToString();
        }

        // For regular types, just return the name
        return type.Name;
    }

    /// <summary>
    /// Gets the name of a generic parameter
    /// </summary>
    /// <param name="type">Generic parameter type</param>
    /// <returns>Generic parameter name</returns>
    private string GetGenericParameterName(Type type)
    {
        if (!type.IsGenericParameter)
        {
            return GetTypeName(type);
        }

        var constraints = new List<string>();

        // Add class/struct constraint
        if (type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
        {
            constraints.Add("class");
        }
        else if (type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
        {
            constraints.Add("struct");
        }

        // Add new() constraint
        if (type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint) &&
            !type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
        {
            constraints.Add("new()");
        }

        // Add interface and base class constraints
        foreach (var constraint in type.GetGenericParameterConstraints())
        {
            if (constraint != typeof(object))
            {
                constraints.Add(GetTypeName(constraint));
            }
        }

        if (constraints.Count == 0)
        {
            return type.Name;
        }

        return $"{type.Name} : {string.Join(", ", constraints)}";
    }

    /// <summary>
    /// Gets a string representation of a member's accessibility
    /// </summary>
    /// <param name="methodBase">Method or constructor to get accessibility for</param>
    /// <returns>Accessibility string</returns>
    private string GetAccessibilityString(MethodBase methodBase)
    {
        if (methodBase.IsPublic)
        {
            return "public";
        }
        else if (methodBase.IsFamily)
        {
            return "protected";
        }
        else if (methodBase.IsFamilyOrAssembly)
        {
            return "protected internal";
        }
        else if (methodBase.IsFamilyAndAssembly)
        {
            return "private protected";
        }
        else if (methodBase.IsAssembly)
        {
            return "internal";
        }
        else
        {
            return "private";
        }
    }

    /// <summary>
    /// Gets a string representation of a field's accessibility
    /// </summary>
    /// <param name="field">Field to get accessibility for</param>
    /// <returns>Accessibility string</returns>
    private string GetAccessibilityString(FieldInfo field)
    {
        if (field.IsPublic)
        {
            return "public";
        }
        else if (field.IsFamily)
        {
            return "protected";
        }
        else if (field.IsFamilyOrAssembly)
        {
            return "protected internal";
        }
        else if (field.IsFamilyAndAssembly)
        {
            return "private protected";
        }
        else if (field.IsAssembly)
        {
            return "internal";
        }
        else
        {
            return "private";
        }
    }

    /// <summary>
    /// Gets a string representation of a type's accessibility
    /// </summary>
    /// <param name="type">Type to get accessibility for</param>
    /// <returns>Accessibility string</returns>
    private string GetTypeAccessibilityString(Type type)
    {
        if (type.IsNestedPublic || type.IsPublic)
        {
            return "public";
        }
        else if (type.IsNestedFamily)
        {
            return "protected";
        }
        else if (type.IsNestedFamORAssem)
        {
            return "protected internal";
        }
        else if (type.IsNestedFamANDAssem)
        {
            return "private protected";
        }
        else if (type.IsNestedAssembly || type.IsNotPublic)
        {
            return "internal";
        }
        else
        {
            return "private";
        }
    }

    /// <summary>
    /// Gets the most accessible of two accessibility strings
    /// </summary>
    /// <param name="access1">First accessibility string</param>
    /// <param name="access2">Second accessibility string</param>
    /// <returns>Most accessible string</returns>
    private string GetMostAccessible(string access1, string access2)
    {
        var accessRank = new Dictionary<string, int>
        {
            { "public", 5 },
            { "protected internal", 4 },
            { "internal", 3 },
            { "protected", 2 },
            { "private protected", 1 },
            { "private", 0 }
        };

        if (accessRank.TryGetValue(access1, out int rank1) && accessRank.TryGetValue(access2, out int rank2))
        {
            return rank1 >= rank2 ? access1 : access2;
        }

        return access1; // Default to first if not found
    }
}


