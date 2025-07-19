// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.ComponentModel.DataAnnotations;

namespace DotNetApiDiff.Models;

/// <summary>
/// Represents a change detected between two API members
/// </summary>
public class ApiChange
{
    /// <summary>
    /// Type of change (Added, Removed, Modified, Excluded)
    /// </summary>
    public ChangeType Type { get; set; }

    /// <summary>
    /// The source member (from the original assembly)
    /// </summary>
    public ApiMember? SourceMember { get; set; }

    /// <summary>
    /// The target member (from the new assembly)
    /// </summary>
    public ApiMember? TargetMember { get; set; }

    /// <summary>
    /// Whether this change is considered breaking
    /// </summary>
    public bool IsBreakingChange { get; set; }

    /// <summary>
    /// Human-readable description of the change
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Additional details about the change
    /// </summary>
    public List<string> Details { get; set; } = new List<string>();

    /// <summary>
    /// Validates the ApiChange instance
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        // Must have a description
        if (string.IsNullOrWhiteSpace(Description))
        {
            return false;
        }

        // Must have at least one member for most change types
        if (Type != ChangeType.Added && Type != ChangeType.Removed &&
            SourceMember == null && TargetMember == null)
        {
            return false;
        }

        // Added changes should have target member
        if (Type == ChangeType.Added && TargetMember == null)
        {
            return false;
        }

        // Removed changes should have source member
        if (Type == ChangeType.Removed && SourceMember == null)
        {
            return false;
        }

        // Modified changes should have both members
        if (Type == ChangeType.Modified && (SourceMember == null || TargetMember == null))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the primary member name for this change
    /// </summary>
    public string GetMemberName()
    {
        return TargetMember?.FullName ?? SourceMember?.FullName ?? "Unknown";
    }

    /// <summary>
    /// Gets a string representation of the change
    /// </summary>
    public override string ToString()
    {
        var memberName = GetMemberName();
        var breakingIndicator = IsBreakingChange ? " [BREAKING]" : "";
        return $"{Type}: {memberName}{breakingIndicator}";
    }
}



