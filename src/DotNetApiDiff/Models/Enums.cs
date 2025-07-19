// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
namespace DotNetApiDiff.Models;

/// <summary>
/// Types of changes that can be detected
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// Indicates a new API element was added
    /// </summary>
    Added,

    /// <summary>
    /// Indicates an API element was removed
    /// </summary>
    Removed,

    /// <summary>
    /// Indicates an API element was modified
    /// </summary>
    Modified,

    /// <summary>
    /// Indicates an API element was moved to a different location
    /// </summary>
    Moved,

    /// <summary>
    /// Indicates an API element was excluded from the comparison
    /// </summary>
    Excluded,
}

/// <summary>
/// Types of API elements/members
/// </summary>
public enum ApiElementType
{
    /// <summary>
    /// Represents an assembly element
    /// </summary>
    Assembly,

    /// <summary>
    /// Represents a namespace element
    /// </summary>
    Namespace,

    /// <summary>
    /// Represents a type element (class, interface, struct, etc.)
    /// </summary>
    Type,

    /// <summary>
    /// Represents a method element
    /// </summary>
    Method,

    /// <summary>
    /// Represents a property element
    /// </summary>
    Property,

    /// <summary>
    /// Represents a field element
    /// </summary>
    Field,

    /// <summary>
    /// Represents an event element
    /// </summary>
    Event,

    /// <summary>
    /// Represents a constructor element
    /// </summary>
    Constructor,
}

/// <summary>
/// Types of API members for detailed analysis
/// </summary>
public enum MemberType
{
    /// <summary>
    /// Represents a class type
    /// </summary>
    Class,

    /// <summary>
    /// Represents an interface type
    /// </summary>
    Interface,

    /// <summary>
    /// Represents a structure type
    /// </summary>
    Struct,

    /// <summary>
    /// Represents an enumeration type
    /// </summary>
    Enum,

    /// <summary>
    /// Represents a delegate type
    /// </summary>
    Delegate,

    /// <summary>
    /// Represents a method member
    /// </summary>
    Method,

    /// <summary>
    /// Represents a property member
    /// </summary>
    Property,

    /// <summary>
    /// Represents a field member
    /// </summary>
    Field,

    /// <summary>
    /// Represents an event member
    /// </summary>
    Event,

    /// <summary>
    /// Represents a constructor member
    /// </summary>
    Constructor,
}

/// <summary>
/// Accessibility levels for API members
/// </summary>
public enum AccessibilityLevel
{
    /// <summary>
    /// Accessible only within the containing type
    /// </summary>
    Private,

    /// <summary>
    /// Accessible within the containing type and derived types
    /// </summary>
    Protected,

    /// <summary>
    /// Accessible within the containing assembly
    /// </summary>
    Internal,

    /// <summary>
    /// Accessible within the containing assembly and derived types in any assembly
    /// </summary>
    ProtectedInternal,

    /// <summary>
    /// Accessible within the containing type and derived types within the same assembly
    /// </summary>
    ProtectedPrivate,

    /// <summary>
    /// Accessible without restrictions
    /// </summary>
    Public,
}

/// <summary>
/// Severity levels for changes
/// </summary>
public enum SeverityLevel
{
    /// <summary>
    /// Informational change with no impact on compatibility
    /// </summary>
    Info,

    /// <summary>
    /// Change that may impact compatibility in some scenarios
    /// </summary>
    Warning,

    /// <summary>
    /// Change that will likely break compatibility
    /// </summary>
    Error,

    /// <summary>
    /// Change that will definitely break compatibility in a significant way
    /// </summary>
    Critical,
}

/// <summary>
/// Supported report output formats
/// </summary>
public enum ReportFormat
{
    /// <summary>
    /// Output report to console in a readable text format
    /// </summary>
    Console,

    /// <summary>
    /// Output report in JSON format
    /// </summary>
    Json,

    /// <summary>
    /// Output report in XML format
    /// </summary>
    Xml,

    /// <summary>
    /// Output report in HTML format
    /// </summary>
    Html,

    /// <summary>
    /// Output report in Markdown format
    /// </summary>
    Markdown,
}
