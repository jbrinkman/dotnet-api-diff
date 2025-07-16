namespace DotNetApiDiff.Models;

/// <summary>
/// Types of changes that can be detected
/// </summary>
public enum ChangeType
{
    Added,
    Removed,
    Modified,
    Moved
}

/// <summary>
/// Types of API elements
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
    Constructor
}

/// <summary>
/// Severity levels for changes
/// </summary>
public enum SeverityLevel
{
    Info,
    Warning,
    Error,
    Critical
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
    Markdown
}