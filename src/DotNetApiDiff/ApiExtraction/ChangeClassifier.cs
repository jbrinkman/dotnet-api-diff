// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.Text.RegularExpressions;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using DotNetApiDiff.Models.Configuration;
using Microsoft.Extensions.Logging;

namespace DotNetApiDiff.ApiExtraction;

/// <summary>
/// Classifies API changes as breaking, non-breaking, or excluded based on configuration rules
/// </summary>
public class ChangeClassifier : IChangeClassifier
{
    private readonly BreakingChangeRules _breakingChangeRules;
    private readonly ExclusionConfiguration _exclusionConfig;
    private readonly ILogger<ChangeClassifier> _logger;
    private readonly Dictionary<string, Regex> _typePatternCache = new();
    private readonly Dictionary<string, Regex> _memberPatternCache = new();

    /// <summary>
    /// Creates a new instance of the ChangeClassifier
    /// </summary>
    /// <param name="breakingChangeRules">Rules for determining breaking changes</param>
    /// <param name="exclusionConfig">Configuration for exclusions</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public ChangeClassifier(
        BreakingChangeRules breakingChangeRules,
        ExclusionConfiguration exclusionConfig,
        ILogger<ChangeClassifier> logger)
    {
        _breakingChangeRules = breakingChangeRules ?? throw new ArgumentNullException(nameof(breakingChangeRules));
        _exclusionConfig = exclusionConfig ?? throw new ArgumentNullException(nameof(exclusionConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize regex pattern caches
        InitializePatternCaches();
    }

    /// <summary>
    /// Classifies an API difference as breaking, non-breaking, or excluded
    /// </summary>
    /// <param name="difference">The API difference to classify</param>
    /// <returns>The classified API difference with updated properties</returns>
    public ApiDifference ClassifyChange(ApiDifference difference)
    {
        if (difference == null)
        {
            throw new ArgumentNullException(nameof(difference));
        }

        // Check if the element should be excluded
        if (ShouldExcludeElement(difference))
        {
            difference.ChangeType = ChangeType.Excluded;
            difference.IsBreakingChange = false;
            difference.Severity = SeverityLevel.Info;
            difference.Description = $"Excluded {difference.ElementType}: {difference.ElementName}";

            _logger.LogDebug("Classified {ElementType} '{ElementName}' as excluded",
                difference.ElementType, difference.ElementName);

            return difference;
        }

        // Classify based on change type and breaking change rules
        switch (difference.ChangeType)
        {
            case ChangeType.Added:
                ClassifyAddedChange(difference);
                break;

            case ChangeType.Removed:
                ClassifyRemovedChange(difference);
                break;

            case ChangeType.Modified:
                ClassifyModifiedChange(difference);
                break;

            case ChangeType.Moved:
                ClassifyMovedChange(difference);
                break;
        }

        _logger.LogDebug("Classified {ElementType} '{ElementName}' as {ChangeType}, Breaking: {IsBreaking}",
            difference.ElementType, difference.ElementName, difference.ChangeType, difference.IsBreakingChange);

        return difference;
    }

    /// <summary>
    /// Determines if a type should be excluded from comparison
    /// </summary>
    /// <param name="typeName">The fully qualified type name</param>
    /// <returns>True if the type should be excluded, false otherwise</returns>
    public bool IsTypeExcluded(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        // Check exact matches first
        if (_exclusionConfig.ExcludedTypes.Contains(typeName))
        {
            _logger.LogDebug("Type '{TypeName}' excluded by exact match", typeName);
            return true;
        }

        // Check pattern matches
        foreach (var pattern in _typePatternCache.Keys)
        {
            if (_typePatternCache[pattern].IsMatch(typeName))
            {
                _logger.LogDebug("Type '{TypeName}' excluded by pattern '{Pattern}'", typeName, pattern);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a member should be excluded from comparison
    /// </summary>
    /// <param name="memberName">The fully qualified member name</param>
    /// <returns>True if the member should be excluded, false otherwise</returns>
    public bool IsMemberExcluded(string memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName))
        {
            return false;
        }

        // Check exact matches first
        if (_exclusionConfig.ExcludedMembers.Contains(memberName))
        {
            _logger.LogDebug("Member '{MemberName}' excluded by exact match", memberName);
            return true;
        }

        // Check pattern matches
        foreach (var pattern in _memberPatternCache.Keys)
        {
            if (_memberPatternCache[pattern].IsMatch(memberName))
            {
                _logger.LogDebug("Member '{MemberName}' excluded by pattern '{Pattern}'", memberName, pattern);
                return true;
            }
        }

        // Check if the declaring type is excluded
        int lastDotIndex = memberName.LastIndexOf('.');
        if (lastDotIndex > 0)
        {
            string declaringTypeName = memberName.Substring(0, lastDotIndex);
            if (IsTypeExcluded(declaringTypeName))
            {
                _logger.LogDebug("Member '{MemberName}' excluded because its declaring type is excluded", memberName);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Initializes the regex pattern caches for type and member exclusion patterns
    /// </summary>
    private void InitializePatternCaches()
    {
        // Convert type patterns to regex
        foreach (var pattern in _exclusionConfig.ExcludedTypePatterns)
        {
            try
            {
                var regex = new Regex(WildcardToRegex(pattern), RegexOptions.Compiled);
                _typePatternCache[pattern] = regex;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid type exclusion pattern: {Pattern}", pattern);
            }
        }

        // Convert member patterns to regex
        foreach (var pattern in _exclusionConfig.ExcludedMemberPatterns)
        {
            try
            {
                var regex = new Regex(WildcardToRegex(pattern), RegexOptions.Compiled);
                _memberPatternCache[pattern] = regex;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid member exclusion pattern: {Pattern}", pattern);
            }
        }

        _logger.LogDebug(
            "Initialized exclusion pattern caches with {TypePatternCount} type patterns and {MemberPatternCount} member patterns",
            _typePatternCache.Count,
            _memberPatternCache.Count);
    }

    /// <summary>
    /// Converts a wildcard pattern to a regular expression
    /// </summary>
    /// <param name="pattern">The wildcard pattern</param>
    /// <returns>A regular expression pattern</returns>
    private static string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern)
                          .Replace("\\*", ".*")
                          .Replace("\\?", ".") + "$";
    }

    /// <summary>
    /// Determines if an API difference should be excluded based on configuration
    /// </summary>
    /// <param name="difference">The API difference to check</param>
    /// <returns>True if the difference should be excluded, false otherwise</returns>
    private bool ShouldExcludeElement(ApiDifference difference)
    {
        // Check if the element is excluded by name
        switch (difference.ElementType)
        {
            case ApiElementType.Type:
                return IsTypeExcluded(difference.ElementName);

            case ApiElementType.Method:
            case ApiElementType.Property:
            case ApiElementType.Field:
            case ApiElementType.Event:
            case ApiElementType.Constructor:
                return IsMemberExcluded(difference.ElementName);

            default:
                return false;
        }
    }

    /// <summary>
    /// Classifies an added change based on breaking change rules
    /// </summary>
    /// <param name="difference">The difference to classify</param>
    private void ClassifyAddedChange(ApiDifference difference)
    {
        // By default, added changes are not breaking
        difference.IsBreakingChange = false;
        difference.Severity = SeverityLevel.Info;

        // Check specific rules for added changes
        if (difference.ElementType == ApiElementType.Type && _breakingChangeRules.TreatAddedTypeAsBreaking)
        {
            difference.IsBreakingChange = true;
            difference.Severity = SeverityLevel.Warning;
        }
        else if (difference.ElementType != ApiElementType.Type && _breakingChangeRules.TreatAddedMemberAsBreaking)
        {
            difference.IsBreakingChange = true;
            difference.Severity = SeverityLevel.Warning;
        }
    }

    /// <summary>
    /// Classifies a removed change based on breaking change rules
    /// </summary>
    /// <param name="difference">The difference to classify</param>
    private void ClassifyRemovedChange(ApiDifference difference)
    {
        // By default, removed changes are breaking
        difference.IsBreakingChange = true;
        difference.Severity = SeverityLevel.Error;

        // Check specific rules for removed changes
        if (difference.ElementType == ApiElementType.Type && !_breakingChangeRules.TreatTypeRemovalAsBreaking)
        {
            difference.IsBreakingChange = false;
            difference.Severity = SeverityLevel.Warning;
        }
        else if (difference.ElementType != ApiElementType.Type && !_breakingChangeRules.TreatMemberRemovalAsBreaking)
        {
            difference.IsBreakingChange = false;
            difference.Severity = SeverityLevel.Warning;
        }
    }

    /// <summary>
    /// Classifies a modified change based on breaking change rules
    /// </summary>
    /// <param name="difference">The difference to classify</param>
    private void ClassifyModifiedChange(ApiDifference difference)
    {
        // For modified changes, we need to analyze what changed
        // The DifferenceCalculator already set IsBreakingChange based on its analysis
        // Here we can refine that classification based on additional rules

        // If signature changed and we treat signature changes as breaking
        if (difference.OldSignature != difference.NewSignature && _breakingChangeRules.TreatSignatureChangeAsBreaking)
        {
            difference.IsBreakingChange = true;
            difference.Severity = SeverityLevel.Error;
        }
        // If not already classified as breaking, keep the original classification
        else if (!difference.IsBreakingChange)
        {
            difference.Severity = SeverityLevel.Info;
        }
    }

    /// <summary>
    /// Classifies a moved change based on breaking change rules
    /// </summary>
    /// <param name="difference">The difference to classify</param>
    private void ClassifyMovedChange(ApiDifference difference)
    {
        // Moved changes are typically breaking unless configured otherwise
        difference.IsBreakingChange = true;
        difference.Severity = SeverityLevel.Warning;
    }
}
