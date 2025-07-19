namespace DotNetApiDiff.Models;

/// <summary>
/// Types of changes that can be detected
/// </summary>
public enum ChangeType
{
    Added,
    Removed,
    Modified,
    Moved,
    Excluded,
}

/// <summary>
/// Types of API elements/members
/// </summary>
public enum ApiElementType
{
    Assembly,
    Namespace,
    Type,
    Method,
    Property,
    Field,
    Event,
    Constructor,
}

/// <summary>
/// Types of API members for detailed analysis
/// </summary>
public enum MemberType
{
    Class,
    Interface,
    Struct,
    Enum,
    Delegate,
    Method,
    Property,
    Field,
    Event,
    Constructor,
}

/// <summary>
/// Accessibility levels for API members
/// </summary>
public enum AccessibilityLevel
{
    Private,
    Protected,
    Internal,
    ProtectedInternal,
    ProtectedPrivate,
    Public,
}

/// <summary>
/// Severity levels for changes
/// </summary>
public enum SeverityLevel
{
    Info,
    Warning,
    Error,
    Critical,
}

/// <summary>
/// Supported report output formats
/// </summary>
public enum ReportFormat
{
    Console,
    Json,
    Xml,
    Html,
    Markdown,
}