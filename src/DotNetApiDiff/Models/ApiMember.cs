using System.ComponentModel.DataAnnotations;

namespace DotNetApiDiff.Models;

/// <summary>
/// Represents a public API member from a .NET assembly
/// </summary>
public class ApiMember
{
    /// <summary>
    /// Simple name of the member
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full qualified name of the member
    /// </summary>
    [Required]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Type of the member (Class, Method, Property, etc.)
    /// </summary>
    public MemberType Type { get; set; }

    /// <summary>
    /// Accessibility level of the member
    /// </summary>
    public AccessibilityLevel Accessibility { get; set; }

    /// <summary>
    /// Normalized signature of the member for comparison
    /// </summary>
    [Required]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// List of attributes applied to the member
    /// </summary>
    public List<string> Attributes { get; set; } = new List<string>();

    /// <summary>
    /// Name of the type that declares this member
    /// </summary>
    public string DeclaringType { get; set; } = string.Empty;

    /// <summary>
    /// Namespace containing this member
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Validates the ApiMember instance
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(FullName) &&
               !string.IsNullOrWhiteSpace(Signature);
    }

    /// <summary>
    /// Gets a string representation of the member
    /// </summary>
    public override string ToString()
    {
        return $"{Type} {FullName} ({Accessibility})";
    }

    /// <summary>
    /// Determines equality based on FullName and Signature
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not ApiMember other)
        {
            return false;
        }

        return FullName == other.FullName && Signature == other.Signature;
    }

    /// <summary>
    /// Gets hash code based on FullName and Signature
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(FullName, Signature);
    }
}
