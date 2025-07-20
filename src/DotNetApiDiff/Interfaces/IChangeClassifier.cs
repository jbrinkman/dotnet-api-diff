// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models;

namespace DotNetApiDiff.Interfaces;

/// <summary>
/// Interface for classifying API changes as breaking, non-breaking, or excluded
/// </summary>
public interface IChangeClassifier
{
    /// <summary>
    /// Classifies an API difference as breaking, non-breaking, or excluded
    /// </summary>
    /// <param name="difference">The API difference to classify</param>
    /// <returns>The classified API difference with updated properties</returns>
    ApiDifference ClassifyChange(ApiDifference difference);

    /// <summary>
    /// Determines if a type should be excluded from comparison
    /// </summary>
    /// <param name="typeName">The fully qualified type name</param>
    /// <returns>True if the type should be excluded, false otherwise</returns>
    bool IsTypeExcluded(string typeName);

    /// <summary>
    /// Determines if a member should be excluded from comparison
    /// </summary>
    /// <param name="memberName">The fully qualified member name</param>
    /// <returns>True if the member should be excluded, false otherwise</returns>
    bool IsMemberExcluded(string memberName);
}
