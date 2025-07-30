// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models;

namespace DotNetApiDiff.Interfaces;

/// <summary>
/// Interface for calculating detailed API differences
/// </summary>
public interface IDifferenceCalculator
{
    /// <summary>
    /// Calculates an ApiDifference for an added type
    /// </summary>
    /// <param name="newType">The new type that was added</param>
    /// <returns>ApiDifference representing the addition</returns>
    ApiDifference CalculateAddedType(Type newType);

    /// <summary>
    /// Calculates an ApiDifference for a removed type
    /// </summary>
    /// <param name="oldType">The old type that was removed</param>
    /// <returns>ApiDifference representing the removal</returns>
    ApiDifference CalculateRemovedType(Type oldType);

    /// <summary>
    /// Calculates an ApiDifference for changes between two types
    /// </summary>
    /// <param name="oldType">The original type</param>
    /// <param name="newType">The new type</param>
    /// <param name="signaturesEquivalent">Whether the signatures are equivalent after applying type mappings</param>
    /// <returns>ApiDifference representing the changes, or null if no changes</returns>
    ApiDifference? CalculateTypeChanges(Type oldType, Type newType, bool signaturesEquivalent = false);

    /// <summary>
    /// Calculates an ApiDifference for an added member
    /// </summary>
    /// <param name="newMember">The new member that was added</param>
    /// <returns>ApiDifference representing the addition</returns>
    ApiDifference CalculateAddedMember(ApiMember newMember);

    /// <summary>
    /// Calculates an ApiDifference for a removed member
    /// </summary>
    /// <param name="oldMember">The old member that was removed</param>
    /// <returns>ApiDifference representing the removal</returns>
    ApiDifference CalculateRemovedMember(ApiMember oldMember);

    /// <summary>
    /// Calculates an ApiDifference for changes between two members
    /// </summary>
    /// <param name="oldMember">The original member</param>
    /// <param name="newMember">The new member</param>
    /// <returns>ApiDifference representing the changes, or null if no changes</returns>
    ApiDifference? CalculateMemberChanges(ApiMember oldMember, ApiMember newMember);
}
