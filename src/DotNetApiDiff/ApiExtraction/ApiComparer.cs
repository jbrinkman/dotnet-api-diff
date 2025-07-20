// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.Reflection;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using Microsoft.Extensions.Logging;

namespace DotNetApiDiff.ApiExtraction;

/// <summary>
/// Compares APIs between two .NET assemblies to identify differences
/// </summary>
public class ApiComparer : IApiComparer
{
    private readonly IApiExtractor _apiExtractor;
    private readonly IDifferenceCalculator _differenceCalculator;
    private readonly ILogger<ApiComparer> _logger;

    /// <summary>
    /// Creates a new instance of the ApiComparer
    /// </summary>
    /// <param name="apiExtractor">API extractor for getting API members</param>
    /// <param name="differenceCalculator">Calculator for detailed change analysis</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public ApiComparer(
        IApiExtractor apiExtractor,
        IDifferenceCalculator differenceCalculator,
        ILogger<ApiComparer> logger)
    {
        _apiExtractor = apiExtractor ?? throw new ArgumentNullException(nameof(apiExtractor));
        _differenceCalculator = differenceCalculator ?? throw new ArgumentNullException(nameof(differenceCalculator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Compares the public APIs of two assemblies and returns the differences
    /// </summary>
    /// <param name="oldAssembly">The original assembly</param>
    /// <param name="newAssembly">The new assembly to compare against</param>
    /// <returns>Comparison result containing all detected differences</returns>
    public ComparisonResult CompareAssemblies(Assembly oldAssembly, Assembly newAssembly)
    {
        if (oldAssembly == null)
        {
            throw new ArgumentNullException(nameof(oldAssembly));
        }

        if (newAssembly == null)
        {
            throw new ArgumentNullException(nameof(newAssembly));
        }

        _logger.LogInformation(
            "Comparing assemblies: {OldAssembly} and {NewAssembly}",
            oldAssembly.GetName().Name,
            newAssembly.GetName().Name);

        var result = new ComparisonResult
        {
            OldAssemblyPath = oldAssembly.Location,
            NewAssemblyPath = newAssembly.Location,
            ComparisonTimestamp = DateTime.UtcNow
        };

        try
        {
            // Extract API members from both assemblies
            var oldTypes = _apiExtractor.GetPublicTypes(oldAssembly).ToList();
            var newTypes = _apiExtractor.GetPublicTypes(newAssembly).ToList();

            _logger.LogDebug(
                "Found {OldTypeCount} types in old assembly and {NewTypeCount} types in new assembly",
                oldTypes.Count,
                newTypes.Count);

            // Compare types
            var typeDifferences = CompareTypes(oldTypes, newTypes).ToList();

            // Add the differences to the result
            foreach (var diff in typeDifferences)
            {
                result.Differences.Add(diff);
            }

            // Update summary statistics
            result.Summary.AddedCount = result.Differences.Count(d => d.ChangeType == ChangeType.Added);
            result.Summary.RemovedCount = result.Differences.Count(d => d.ChangeType == ChangeType.Removed);
            result.Summary.ModifiedCount = result.Differences.Count(d => d.ChangeType == ChangeType.Modified);
            result.Summary.BreakingChangesCount = result.Differences.Count(d => d.IsBreakingChange);

            _logger.LogInformation(
                "Comparison complete. Found {TotalDifferences} differences ({AddedCount} added, {RemovedCount} removed, {ModifiedCount} modified)",
                result.TotalDifferences,
                result.Summary.AddedCount,
                result.Summary.RemovedCount,
                result.Summary.ModifiedCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error comparing assemblies {OldAssembly} and {NewAssembly}",
                oldAssembly.GetName().Name,
                newAssembly.GetName().Name);
            throw;
        }
    }

    /// <summary>
    /// Compares types between two assemblies
    /// </summary>
    /// <param name="oldTypes">Types from the original assembly</param>
    /// <param name="newTypes">Types from the new assembly</param>
    /// <returns>List of type-level differences</returns>
    public IEnumerable<ApiDifference> CompareTypes(IEnumerable<Type> oldTypes, IEnumerable<Type> newTypes)
    {
        if (oldTypes == null)
        {
            throw new ArgumentNullException(nameof(oldTypes));
        }

        if (newTypes == null)
        {
            throw new ArgumentNullException(nameof(newTypes));
        }

        var differences = new List<ApiDifference>();
        var oldTypesList = oldTypes.ToList();
        var newTypesList = newTypes.ToList();

        // Create dictionaries for faster lookup
        var oldTypesByFullName = oldTypesList.ToDictionary(t => t.FullName ?? t.Name);
        var newTypesByFullName = newTypesList.ToDictionary(t => t.FullName ?? t.Name);

        // Find added types
        foreach (var newType in newTypesList)
        {
            var newTypeName = newType.FullName ?? newType.Name;
            if (!oldTypesByFullName.ContainsKey(newTypeName))
            {
                differences.Add(_differenceCalculator.CalculateAddedType(newType));
            }
        }

        // Find removed types
        foreach (var oldType in oldTypesList)
        {
            var oldTypeName = oldType.FullName ?? oldType.Name;
            if (!newTypesByFullName.ContainsKey(oldTypeName))
            {
                differences.Add(_differenceCalculator.CalculateRemovedType(oldType));
            }
        }

        // Find modified types
        foreach (var oldType in oldTypesList)
        {
            var oldTypeName = oldType.FullName ?? oldType.Name;
            if (newTypesByFullName.TryGetValue(oldTypeName, out var newType))
            {
                // Compare the types
                var memberDifferences = CompareMembers(oldType, newType).ToList();
                differences.AddRange(memberDifferences);

                // Check for type-level changes (e.g., accessibility, base class, interfaces)
                var typeDifference = _differenceCalculator.CalculateTypeChanges(oldType, newType);
                if (typeDifference != null)
                {
                    differences.Add(typeDifference);
                }
            }
        }

        return differences;
    }

    /// <summary>
    /// Compares members (methods, properties, fields) of two types
    /// </summary>
    /// <param name="oldType">Original type</param>
    /// <param name="newType">New type to compare against</param>
    /// <returns>List of member-level differences</returns>
    public IEnumerable<ApiDifference> CompareMembers(Type oldType, Type newType)
    {
        if (oldType == null)
        {
            throw new ArgumentNullException(nameof(oldType));
        }

        if (newType == null)
        {
            throw new ArgumentNullException(nameof(newType));
        }

        var differences = new List<ApiDifference>();

        try
        {
            // Extract members from both types
            var oldMembers = _apiExtractor.ExtractTypeMembers(oldType).ToList();
            var newMembers = _apiExtractor.ExtractTypeMembers(newType).ToList();

            _logger.LogDebug(
                "Found {OldMemberCount} members in old type and {NewMemberCount} members in new type",
                oldMembers.Count,
                newMembers.Count);

            // Create dictionaries for faster lookup
            var oldMembersBySignature = oldMembers.ToDictionary(m => m.Signature);
            var newMembersBySignature = newMembers.ToDictionary(m => m.Signature);

            // Find added members
            foreach (var newMember in newMembers)
            {
                if (!oldMembersBySignature.ContainsKey(newMember.Signature))
                {
                    _logger.LogDebug("Found added member: {MemberName}", newMember.FullName);
                    var addedDifference = _differenceCalculator.CalculateAddedMember(newMember);
                    differences.Add(addedDifference);
                }
            }

            // Find removed members
            foreach (var oldMember in oldMembers)
            {
                if (!newMembersBySignature.ContainsKey(oldMember.Signature))
                {
                    _logger.LogDebug("Found removed member: {MemberName}", oldMember.FullName);
                    var removedDifference = _differenceCalculator.CalculateRemovedMember(oldMember);
                    differences.Add(removedDifference);
                }
            }

            // Find modified members
            foreach (var oldMember in oldMembers)
            {
                if (newMembersBySignature.TryGetValue(oldMember.Signature, out var newMember))
                {
                    var memberDifference = _differenceCalculator.CalculateMemberChanges(oldMember, newMember);
                    if (memberDifference != null)
                    {
                        _logger.LogDebug("Found modified member: {MemberName}", oldMember.FullName);
                        differences.Add(memberDifference);
                    }
                }
            }

            return differences;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error comparing members of types {OldType} and {NewType}",
                oldType.FullName,
                newType.FullName);
            return Enumerable.Empty<ApiDifference>();
        }
    }
}
