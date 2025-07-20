// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.Reflection;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using DotNetApiDiff.Models.Configuration;
using Microsoft.Extensions.Logging;

namespace DotNetApiDiff.ApiExtraction;

/// <summary>
/// Compares APIs between two .NET assemblies to identify differences
/// </summary>
public class ApiComparer : IApiComparer
{
    private readonly IApiExtractor _apiExtractor;
    private readonly IDifferenceCalculator _differenceCalculator;
    private readonly INameMapper _nameMapper;
    private readonly IChangeClassifier _changeClassifier;
    private readonly ILogger<ApiComparer> _logger;

    /// <summary>
    /// Creates a new instance of the ApiComparer
    /// </summary>
    /// <param name="apiExtractor">API extractor for getting API members</param>
    /// <param name="differenceCalculator">Calculator for detailed change analysis</param>
    /// <param name="nameMapper">Mapper for namespace and type name transformations</param>
    /// <param name="changeClassifier">Classifier for breaking changes and exclusions</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public ApiComparer(
        IApiExtractor apiExtractor,
        IDifferenceCalculator differenceCalculator,
        INameMapper nameMapper,
        IChangeClassifier changeClassifier,
        ILogger<ApiComparer> logger)
    {
        _apiExtractor = apiExtractor ?? throw new ArgumentNullException(nameof(apiExtractor));
        _differenceCalculator = differenceCalculator ?? throw new ArgumentNullException(nameof(differenceCalculator));
        _nameMapper = nameMapper ?? throw new ArgumentNullException(nameof(nameMapper));
        _changeClassifier = changeClassifier ?? throw new ArgumentNullException(nameof(changeClassifier));
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

            // Classify and add the differences to the result
            foreach (var diff in typeDifferences)
            {
                // Classify the difference using the change classifier
                var classifiedDiff = _changeClassifier.ClassifyChange(diff);
                result.Differences.Add(classifiedDiff);
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

        // Create a lookup for mapped types
        var mappedTypeLookup = new Dictionary<string, List<Type>>();

        // Build the mapped type lookup
        foreach (var oldType in oldTypesList)
        {
            var oldTypeName = oldType.FullName ?? oldType.Name;
            var mappedNames = _nameMapper.MapFullTypeName(oldTypeName).ToList();

            foreach (var mappedName in mappedNames)
            {
                if (mappedName != oldTypeName)
                {
                    _logger.LogDebug("Mapped type {OldTypeName} to {MappedTypeName}", oldTypeName, mappedName);

                    if (!mappedTypeLookup.ContainsKey(mappedName))
                    {
                        mappedTypeLookup[mappedName] = new List<Type>();
                    }

                    mappedTypeLookup[mappedName].Add(oldType);
                }
            }
        }

        // Find added types
        foreach (var newType in newTypesList)
        {
            var newTypeName = newType.FullName ?? newType.Name;
            bool foundMatch = false;

            // Check direct match
            if (oldTypesByFullName.ContainsKey(newTypeName))
            {
                foundMatch = true;
            }
            else
            {
                // Check if this type matches any mapped old types
                var mappedOldNames = _nameMapper.MapFullTypeName(newTypeName).ToList();

                foreach (var mappedName in mappedOldNames)
                {
                    if (oldTypesByFullName.ContainsKey(mappedName))
                    {
                        foundMatch = true;
                        break;
                    }
                }

                // Check for auto-mapping if enabled
                if (!foundMatch && _nameMapper.ShouldAutoMapType(newTypeName))
                {
                    if (TryFindTypeBySimpleName(newTypeName, oldTypesList, out var matchedOldTypeName))
                    {
                        foundMatch = true;
                        _logger.LogDebug(
                            "Auto-mapped type {NewTypeName} to {OldTypeName} by simple name",
                            newTypeName,
                            matchedOldTypeName);
                    }
                }
            }

            if (!foundMatch)
            {
                differences.Add(_differenceCalculator.CalculateAddedType(newType));
            }
        }

        // Find removed types
        foreach (var oldType in oldTypesList)
        {
            var oldTypeName = oldType.FullName ?? oldType.Name;
            bool foundMatch = false;

            // Check direct match
            if (newTypesByFullName.ContainsKey(oldTypeName))
            {
                foundMatch = true;
            }
            else
            {
                // Check mapped names
                var mappedNames = _nameMapper.MapFullTypeName(oldTypeName).ToList();

                foreach (var mappedName in mappedNames)
                {
                    if (newTypesByFullName.ContainsKey(mappedName))
                    {
                        foundMatch = true;
                        break;
                    }
                }

                // Check for auto-mapping if enabled
                if (!foundMatch && _nameMapper.ShouldAutoMapType(oldTypeName))
                {
                    if (TryFindTypeBySimpleName(oldTypeName, newTypesList, out var matchedNewTypeName))
                    {
                        foundMatch = true;
                        _logger.LogDebug(
                            "Auto-mapped type {OldTypeName} to {NewTypeName} by simple name",
                            oldTypeName,
                            matchedNewTypeName);
                    }
                }
            }

            if (!foundMatch)
            {
                differences.Add(_differenceCalculator.CalculateRemovedType(oldType));
            }
        }

        // Find modified types - direct matches
        foreach (var oldType in oldTypesList)
        {
            var oldTypeName = oldType.FullName ?? oldType.Name;

            // Check direct match first
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
            else
            {
                // Check mapped names
                var mappedNames = _nameMapper.MapFullTypeName(oldTypeName).ToList();

                foreach (var mappedName in mappedNames)
                {
                    if (newTypesByFullName.TryGetValue(mappedName, out var mappedNewType))
                    {
                        _logger.LogDebug("Comparing mapped types: {OldTypeName} -> {MappedTypeName}", oldTypeName, mappedName);

                        // Compare the types
                        var memberDifferences = CompareMembers(oldType, mappedNewType).ToList();
                        differences.AddRange(memberDifferences);

                        // Check for type-level changes
                        var typeDifference = _differenceCalculator.CalculateTypeChanges(oldType, mappedNewType);
                        if (typeDifference != null)
                        {
                            differences.Add(typeDifference);
                        }

                        break;
                    }
                }
            }
        }

        return differences;
    }

    /// <summary>
    /// Tries to find a matching type by simple name (without namespace)
    /// </summary>
    /// <param name="typeName">The type name to find a match for</param>
    /// <param name="candidateTypes">List of candidate types to search</param>
    /// <param name="matchedTypeName">The matched type name, if found</param>
    /// <returns>True if a match was found, false otherwise</returns>
    private bool TryFindTypeBySimpleName(string typeName, IEnumerable<Type> candidateTypes, out string? matchedTypeName)
    {
        matchedTypeName = null;

        // Extract simple type name for auto-mapping
        int lastDotIndex = typeName.LastIndexOf('.');
        if (lastDotIndex <= 0)
        {
            return false;
        }

        string simpleTypeName = typeName.Substring(lastDotIndex + 1);

        // Look for any type with the same simple name
        foreach (var candidateType in candidateTypes)
        {
            var candidateTypeName = candidateType.FullName ?? candidateType.Name;
            int candidateLastDotIndex = candidateTypeName.LastIndexOf('.');

            if (candidateLastDotIndex > 0)
            {
                string candidateSimpleTypeName = candidateTypeName.Substring(candidateLastDotIndex + 1);

                if (string.Equals(
                    simpleTypeName,
                    candidateSimpleTypeName,
                    _nameMapper.Configuration.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    matchedTypeName = candidateTypeName;
                    return true;
                }
            }
        }

        return false;
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
