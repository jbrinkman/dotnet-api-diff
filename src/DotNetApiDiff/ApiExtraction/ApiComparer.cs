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
    private readonly ComparisonConfiguration _configuration;
    private readonly ILogger<ApiComparer> _logger;

    /// <summary>
    /// Creates a new instance of the ApiComparer
    /// </summary>
    /// <param name="apiExtractor">API extractor for getting API members</param>
    /// <param name="differenceCalculator">Calculator for detailed change analysis</param>
    /// <param name="nameMapper">Mapper for namespace and type name transformations</param>
    /// <param name="changeClassifier">Classifier for breaking changes and exclusions</param>
    /// <param name="configuration">Configuration used for the comparison</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public ApiComparer(
        IApiExtractor apiExtractor,
        IDifferenceCalculator differenceCalculator,
        INameMapper nameMapper,
        IChangeClassifier changeClassifier,
        ComparisonConfiguration configuration,
        ILogger<ApiComparer> logger)
    {
        _apiExtractor = apiExtractor ?? throw new ArgumentNullException(nameof(apiExtractor));
        _differenceCalculator = differenceCalculator ?? throw new ArgumentNullException(nameof(differenceCalculator));
        _nameMapper = nameMapper ?? throw new ArgumentNullException(nameof(nameMapper));
        _changeClassifier = changeClassifier ?? throw new ArgumentNullException(nameof(changeClassifier));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
            ComparisonTimestamp = DateTime.UtcNow,
            Configuration = _configuration
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
                // Check if any old type maps to this new type
                foreach (var oldType in oldTypesList)
                {
                    var oldTypeName = oldType.FullName ?? oldType.Name;
                    var mappedNames = _nameMapper.MapFullTypeName(oldTypeName).ToList();

                    foreach (var mappedName in mappedNames)
                    {
                        if (string.Equals(mappedName, newTypeName, StringComparison.Ordinal))
                        {
                            foundMatch = true;
                            _logger.LogDebug(
                                "Found mapped type: {OldTypeName} -> {NewTypeName}",
                                oldTypeName,
                                newTypeName);
                            break;
                        }
                    }

                    if (foundMatch)
                    {
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

                        // Check for type-level changes with signature equivalence
                        // For mapped types, check if the old type name maps to the new type name
                        var mappedOldTypeName = _nameMapper.MapTypeName(oldType.Name);
                        var areTypeNamesEquivalent = string.Equals(mappedOldTypeName, mappedNewType.Name, StringComparison.Ordinal);

                        var typeDifference = _differenceCalculator.CalculateTypeChanges(oldType, mappedNewType, areTypeNamesEquivalent);
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
                newMembers.Count);            // Find added members (exist in new but not in old)
            foreach (var newMember in newMembers)
            {
                var equivalentOldMember = FindEquivalentMember(newMember, oldMembers);
                if (equivalentOldMember == null)
                {
                    _logger.LogDebug("Found added member: {MemberName}", newMember.FullName);
                    var addedDifference = _differenceCalculator.CalculateAddedMember(newMember);
                    differences.Add(addedDifference);
                }
            }

            // Find removed members (exist in old but not in new)
            foreach (var oldMember in oldMembers)
            {
                var equivalentNewMember = FindEquivalentMember(oldMember, newMembers);
                if (equivalentNewMember == null)
                {
                    _logger.LogDebug("Found removed member: {MemberName}", oldMember.FullName);
                    var removedDifference = _differenceCalculator.CalculateRemovedMember(oldMember);
                    differences.Add(removedDifference);
                }
            }

            // Find modified members (exist in both but with differences)
            foreach (var oldMember in oldMembers)
            {
                var equivalentNewMember = FindEquivalentMember(oldMember, newMembers);
                if (equivalentNewMember != null)
                {
                    // Check if the members are truly different or just equivalent via type mappings
                    if (AreSignaturesEquivalent(oldMember.Signature, equivalentNewMember.Signature))
                    {
                        // Members are equivalent via type mappings - no difference to report
                        _logger.LogDebug(
                            "Members are equivalent via type mappings: {OldSignature} <-> {NewSignature}",
                            oldMember.Signature,
                            equivalentNewMember.Signature);
                        continue;
                    }

                    // Members match but have other differences beyond type mappings
                    var memberDifference = _differenceCalculator.CalculateMemberChanges(oldMember, equivalentNewMember);
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

    /// <summary>
    /// Applies type mappings to a signature to enable equivalence checking
    /// </summary>
    /// <param name="signature">The original signature</param>
    /// <returns>The signature with type mappings applied</returns>
    private string ApplyTypeMappingsToSignature(string signature)
    {
        if (string.IsNullOrEmpty(signature))
        {
            return signature;
        }

        var mappedSignature = signature;

        // Check if we have type mappings configured
        if (_nameMapper.Configuration?.TypeMappings == null)
        {
            return mappedSignature;
        }

        // Apply all type mappings to the signature
        foreach (var mapping in _nameMapper.Configuration.TypeMappings)
        {
            // Replace the type name in the signature
            // We need to be careful to only replace whole type names, not partial matches
            mappedSignature = ReplaceTypeNameInSignature(mappedSignature, mapping.Key, mapping.Value);

            // Also try with just the type name (without namespace) since signatures might not include full namespaces
            var oldTypeNameOnly = mapping.Key.Split('.').Last();
            var newTypeNameOnly = mapping.Value.Split('.').Last();

            // Only if we had a namespace
            if (oldTypeNameOnly != mapping.Key)
            {
                mappedSignature = ReplaceTypeNameInSignature(mappedSignature, oldTypeNameOnly, newTypeNameOnly);
            }
        }

        return mappedSignature;
    }

    /// <summary>
    /// Replaces a type name in a signature, ensuring we only replace complete type names
    /// </summary>
    /// <param name="signature">The signature to modify</param>
    /// <param name="oldTypeName">The type name to replace</param>
    /// <param name="newTypeName">The replacement type name</param>
    /// <returns>The modified signature</returns>
    private string ReplaceTypeNameInSignature(string signature, string oldTypeName, string newTypeName)
    {
        // We need to replace type names carefully to avoid partial matches
        // For example, when replacing "RedisValue" with "ValkeyValue", we don't want to
        // replace "RedisValueWithExpiry" incorrectly
        var result = signature;

        // Pattern 1: Type name followed by non-word character (space, <, >, ,, etc.)
        // This handles most cases including generic parameters and return types
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            $@"\b{System.Text.RegularExpressions.Regex.Escape(oldTypeName)}\b",
            newTypeName);

        // Pattern 2: Special handling for constructor names
        // Constructor signatures typically look like: "public RedisValue(parameters)"
        // We need to replace the constructor name (which matches the type name) as well
        // This pattern matches: word boundary + type name + opening parenthesis
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            $@"\b{System.Text.RegularExpressions.Regex.Escape(oldTypeName)}(?=\s*\()",
            newTypeName);

        return result;
    }

    /// <summary>
    /// Checks if two signatures are equivalent considering type mappings
    /// </summary>
    /// <param name="sourceSignature">Signature from the source assembly</param>
    /// <param name="targetSignature">Signature from the target assembly</param>
    /// <returns>True if the signatures are equivalent after applying type mappings</returns>
    private bool AreSignaturesEquivalent(string sourceSignature, string targetSignature)
    {
        // Apply type mappings to the source signature to see if it matches the target
        var mappedSourceSignature = ApplyTypeMappingsToSignature(sourceSignature);

        return string.Equals(mappedSourceSignature, targetSignature, StringComparison.Ordinal);
    }

    /// <summary>
    /// Finds an equivalent member in the target collection based on signature equivalence with type mappings
    /// </summary>
    /// <param name="sourceMember">The member from the source assembly (could be old or new)</param>
    /// <param name="targetMembers">The collection of members from the target assembly (could be new or old)</param>
    /// <returns>The equivalent member if found, null otherwise</returns>
    private ApiMember? FindEquivalentMember(ApiMember sourceMember, IEnumerable<ApiMember> targetMembers)
    {
        // First, try to find a member with the same name - this handles "modified" members
        // where the signature might have changed but it's still the same conceptual member
        var sameNameMember = targetMembers.FirstOrDefault(m =>
            m.Name == sourceMember.Name &&
            m.FullName == sourceMember.FullName);

        if (sameNameMember != null)
        {
            return sameNameMember;
        }

        // If no exact name match, check for signature equivalence due to type mappings
        // This handles cases where type mappings make signatures equivalent even with different names
        foreach (var targetMember in targetMembers)
        {
            // Check if source maps to target (source signature with mappings applied == target signature)
            if (AreSignaturesEquivalent(sourceMember.Signature, targetMember.Signature))
            {
                return targetMember;
            }

            // Also check the reverse: if target maps to source (target signature with mappings applied == source signature)
            if (AreSignaturesEquivalent(targetMember.Signature, sourceMember.Signature))
            {
                return targetMember;
            }
        }

        return null;
    }
}
