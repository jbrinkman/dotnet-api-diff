// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using Microsoft.Extensions.Logging;

namespace DotNetApiDiff.ApiExtraction;

/// <summary>
/// Calculates detailed API differences between members and types
/// </summary>
public class DifferenceCalculator : IDifferenceCalculator
{
    private readonly ITypeAnalyzer _typeAnalyzer;
    private readonly ILogger<DifferenceCalculator> _logger;

    /// <summary>
    /// Creates a new instance of the DifferenceCalculator
    /// </summary>
    /// <param name="typeAnalyzer">Type analyzer for analyzing types</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public DifferenceCalculator(ITypeAnalyzer typeAnalyzer, ILogger<DifferenceCalculator> logger)
    {
        _typeAnalyzer = typeAnalyzer ?? throw new ArgumentNullException(nameof(typeAnalyzer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Calculates an ApiDifference for an added type
    /// </summary>
    /// <param name="newType">The new type that was added</param>
    /// <returns>ApiDifference representing the addition</returns>
    public ApiDifference CalculateAddedType(Type newType)
    {
        if (newType == null)
        {
            throw new ArgumentNullException(nameof(newType));
        }

        try
        {
            var typeMember = _typeAnalyzer.AnalyzeType(newType);

            return new ApiDifference
            {
                ChangeType = ChangeType.Added,
                ElementType = ApiElementType.Type,
                ElementName = newType.FullName ?? newType.Name,
                Description = $"Added {GetTypeKindString(newType)} '{newType.FullName ?? newType.Name}'",
                IsBreakingChange = false, // Adding types is not breaking
                Severity = SeverityLevel.Info,
                NewSignature = typeMember.Signature
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating difference for added type {TypeName}", newType.Name);
            throw;
        }
    }

    /// <summary>
    /// Calculates an ApiDifference for a removed type
    /// </summary>
    /// <param name="oldType">The old type that was removed</param>
    /// <returns>ApiDifference representing the removal</returns>
    public ApiDifference CalculateRemovedType(Type oldType)
    {
        if (oldType == null)
        {
            throw new ArgumentNullException(nameof(oldType));
        }

        try
        {
            var typeMember = _typeAnalyzer.AnalyzeType(oldType);

            return new ApiDifference
            {
                ChangeType = ChangeType.Removed,
                ElementType = ApiElementType.Type,
                ElementName = oldType.FullName ?? oldType.Name,
                Description = $"Removed {GetTypeKindString(oldType)} '{oldType.FullName ?? oldType.Name}'",
                IsBreakingChange = true, // Removing types is breaking
                Severity = SeverityLevel.Error,
                OldSignature = typeMember.Signature
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating difference for removed type {TypeName}", oldType.Name);
            throw;
        }
    }

    /// <summary>
    /// Calculates an ApiDifference for changes between two types
    /// </summary>
    /// <param name="oldType">The original type</param>
    /// <param name="newType">The new type</param>
    /// <param name="signaturesEquivalent">Whether the signatures are equivalent after applying type mappings</param>
    /// <returns>ApiDifference representing the changes, or null if no changes</returns>
    public ApiDifference? CalculateTypeChanges(Type oldType, Type newType, bool signaturesEquivalent = false)
    {
        if (oldType == null)
        {
            throw new ArgumentNullException(nameof(oldType));
        }

        if (newType == null)
        {
            throw new ArgumentNullException(nameof(newType));
        }

        try
        {
            var oldTypeMember = _typeAnalyzer.AnalyzeType(oldType);
            var newTypeMember = _typeAnalyzer.AnalyzeType(newType);

            // For testing purposes, always return a difference if accessibility changes
            if (oldTypeMember.Accessibility != newTypeMember.Accessibility)
            {
                bool accessibilityBreaking = IsReducedAccessibility(oldTypeMember.Accessibility, newTypeMember.Accessibility);
                SeverityLevel accessibilitySeverity = accessibilityBreaking ? SeverityLevel.Error : SeverityLevel.Info;

                return new ApiDifference
                {
                    ChangeType = ChangeType.Modified,
                    ElementType = ApiElementType.Type,
                    ElementName = oldType.FullName ?? oldType.Name,
                    Description = $"Modified {GetTypeKindString(oldType)} '{oldType.FullName ?? oldType.Name}'",
                    IsBreakingChange = accessibilityBreaking,
                    Severity = accessibilitySeverity,
                    OldSignature = oldTypeMember.Signature,
                    NewSignature = newTypeMember.Signature
                };
            }

            // If signatures are different but equivalent after type mappings, no change
            if (signaturesEquivalent)
            {
                return null;
            }

            // If signatures are different, we have changes
            if (oldTypeMember.Signature != newTypeMember.Signature)
            {
                return new ApiDifference
                {
                    ChangeType = ChangeType.Modified,
                    ElementType = ApiElementType.Type,
                    ElementName = oldType.FullName ?? oldType.Name,
                    Description = $"Modified {GetTypeKindString(oldType)} '{oldType.FullName ?? oldType.Name}'",
                    IsBreakingChange = true, // Assume signature changes are breaking
                    Severity = SeverityLevel.Warning,
                    OldSignature = oldTypeMember.Signature,
                    NewSignature = newTypeMember.Signature
                };
            }

            // If signatures are identical and no other changes detected, return null
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error calculating differences between types {OldType} and {NewType}",
                oldType.Name,
                newType.Name);
            return null;
        }
    }

    /// <summary>
    /// Calculates an ApiDifference for an added member
    /// </summary>
    /// <param name="newMember">The new member that was added</param>
    /// <returns>ApiDifference representing the addition</returns>
    public ApiDifference CalculateAddedMember(ApiMember newMember)
    {
        if (newMember == null)
        {
            throw new ArgumentNullException(nameof(newMember));
        }

        try
        {
            return new ApiDifference
            {
                ChangeType = ChangeType.Added,
                ElementType = GetApiElementType(newMember.Type),
                ElementName = newMember.FullName,
                Description = $"Added {GetMemberTypeString(newMember.Type)} '{newMember.FullName}'",
                IsBreakingChange = false, // Adding members is not breaking
                Severity = SeverityLevel.Info,
                NewSignature = newMember.Signature
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating difference for added member {MemberName}", newMember.Name);
            throw;
        }
    }

    /// <summary>
    /// Calculates an ApiDifference for a removed member
    /// </summary>
    /// <param name="oldMember">The old member that was removed</param>
    /// <returns>ApiDifference representing the removal</returns>
    public ApiDifference CalculateRemovedMember(ApiMember oldMember)
    {
        if (oldMember == null)
        {
            throw new ArgumentNullException(nameof(oldMember));
        }

        try
        {
            return new ApiDifference
            {
                ChangeType = ChangeType.Removed,
                ElementType = GetApiElementType(oldMember.Type),
                ElementName = oldMember.FullName,
                Description = $"Removed {GetMemberTypeString(oldMember.Type)} '{oldMember.FullName}'",
                IsBreakingChange = true, // Removing members is breaking
                Severity = SeverityLevel.Error,
                OldSignature = oldMember.Signature
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating difference for removed member {MemberName}", oldMember.Name);
            throw;
        }
    }

    /// <summary>
    /// Calculates an ApiDifference for changes between two members
    /// </summary>
    /// <param name="oldMember">The original member</param>
    /// <param name="newMember">The new member</param>
    /// <returns>ApiDifference representing the changes, or null if no changes</returns>
    public ApiDifference? CalculateMemberChanges(ApiMember oldMember, ApiMember newMember)
    {
        if (oldMember == null)
        {
            throw new ArgumentNullException(nameof(oldMember));
        }

        if (newMember == null)
        {
            throw new ArgumentNullException(nameof(newMember));
        }

        try
        {
            // If signatures are identical, no changes
            if (oldMember.Signature == newMember.Signature)
            {
                return null;
            }

            List<string> changes = new List<string>();
            bool isBreaking = false;
            SeverityLevel severity = SeverityLevel.Info;

            // Check for accessibility changes
            if (oldMember.Accessibility != newMember.Accessibility)
            {
                var accessibilityChange = $"Accessibility changed from '{oldMember.Accessibility}' to '{newMember.Accessibility}'";
                changes.Add(accessibilityChange);

                // Reducing accessibility is breaking
                if (IsReducedAccessibility(oldMember.Accessibility, newMember.Accessibility))
                {
                    isBreaking = true;
                    severity = SeverityLevel.Error;
                }
            }

            // Check for attribute changes
            var removedAttributes = oldMember.Attributes.Except(newMember.Attributes).ToList();
            var addedAttributes = newMember.Attributes.Except(oldMember.Attributes).ToList();

            foreach (var removedAttr in removedAttributes)
            {
                changes.Add($"Removed attribute '{removedAttr}'");
            }

            foreach (var addedAttr in addedAttributes)
            {
                changes.Add($"Added attribute '{addedAttr}'");
            }

            // If no changes were detected but signatures differ, add a generic change
            if (!changes.Any())
            {
                changes.Add("Member signature changed");

                // Signature changes are potentially breaking
                isBreaking = true;
                severity = SeverityLevel.Warning;
            }

            return new ApiDifference
            {
                ChangeType = ChangeType.Modified,
                ElementType = GetApiElementType(oldMember.Type),
                ElementName = oldMember.FullName,
                Description = $"Modified {GetMemberTypeString(oldMember.Type)} '{oldMember.FullName}'",
                IsBreakingChange = isBreaking,
                Severity = severity,
                OldSignature = oldMember.Signature,
                NewSignature = newMember.Signature
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error calculating differences between members {OldMember} and {NewMember}",
                oldMember.Name,
                newMember.Name);
            return null;
        }
    }

    /// <summary>
    /// Gets a string representation of a type kind
    /// </summary>
    /// <param name="type">Type to get kind string for</param>
    /// <returns>Type kind string</returns>
    private string GetTypeKindString(Type type)
    {
        if (type.IsInterface)
        {
            return "interface";
        }
        else if (type.IsEnum)
        {
            return "enum";
        }
        else if (type.IsValueType)
        {
            return "struct";
        }
        else if (type.IsSubclassOf(typeof(MulticastDelegate)))
        {
            return "delegate";
        }
        else
        {
            return "class";
        }
    }

    /// <summary>
    /// Gets a string representation of a member type
    /// </summary>
    /// <param name="memberType">Member type to get string for</param>
    /// <returns>Member type string</returns>
    private string GetMemberTypeString(MemberType memberType)
    {
        return memberType.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Maps a MemberType to an ApiElementType
    /// </summary>
    /// <param name="memberType">Member type to map</param>
    /// <returns>Corresponding API element type</returns>
    private ApiElementType GetApiElementType(MemberType memberType)
    {
        return memberType switch
        {
            MemberType.Class => ApiElementType.Type,
            MemberType.Interface => ApiElementType.Type,
            MemberType.Struct => ApiElementType.Type,
            MemberType.Enum => ApiElementType.Type,
            MemberType.Delegate => ApiElementType.Type,
            MemberType.Method => ApiElementType.Method,
            MemberType.Property => ApiElementType.Property,
            MemberType.Field => ApiElementType.Field,
            MemberType.Event => ApiElementType.Event,
            MemberType.Constructor => ApiElementType.Constructor,
            _ => ApiElementType.Type
        };
    }

    /// <summary>
    /// Checks if accessibility has been reduced
    /// </summary>
    /// <param name="oldAccessibility">Original accessibility</param>
    /// <param name="newAccessibility">New accessibility</param>
    /// <returns>True if accessibility has been reduced, false otherwise</returns>
    private bool IsReducedAccessibility(AccessibilityLevel oldAccessibility, AccessibilityLevel newAccessibility)
    {
        // Higher values are more accessible
        var accessibilityRank = new Dictionary<AccessibilityLevel, int>
        {
            { AccessibilityLevel.Public, 5 },
            { AccessibilityLevel.ProtectedInternal, 4 },
            { AccessibilityLevel.Internal, 3 },
            { AccessibilityLevel.Protected, 2 },
            { AccessibilityLevel.ProtectedPrivate, 1 },
            { AccessibilityLevel.Private, 0 }
        };

        return accessibilityRank[newAccessibility] < accessibilityRank[oldAccessibility];
    }
}
