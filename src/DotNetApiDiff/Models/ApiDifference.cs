namespace DotNetApiDiff.Models;

/// <summary>
/// Represents a single API difference between two assemblies
/// </summary>
public class ApiDifference
{
    /// <summary>
    /// Type of change detected
    /// </summary>
    public ChangeType ChangeType { get; set; }

    /// <summary>
    /// Category of the API element that changed
    /// </summary>
    public ApiElementType ElementType { get; set; }

    /// <summary>
    /// Full name of the API element
    /// </summary>
    public string ElementName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the change
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this change is considered breaking
    /// </summary>
    public bool IsBreakingChange { get; set; }

    /// <summary>
    /// Severity level of the change
    /// </summary>
    public SeverityLevel Severity { get; set; }

    /// <summary>
    /// Old signature or value (if applicable)
    /// </summary>
    public string? OldSignature { get; set; }

    /// <summary>
    /// New signature or value (if applicable)
    /// </summary>
    public string? NewSignature { get; set; }
}