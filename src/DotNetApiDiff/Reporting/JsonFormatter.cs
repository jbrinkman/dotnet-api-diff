// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetApiDiff.Reporting;

/// <summary>
/// Formatter for JSON output with complete comparison details
/// </summary>
public class JsonFormatter : IReportFormatter
{
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFormatter"/> class.
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    public JsonFormatter(bool indented = true)
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Formats a comparison result as JSON
    /// </summary>
    /// <param name="result">The comparison result to format</param>
    /// <returns>Formatted JSON output as a string</returns>
    public string Format(ComparisonResult result)
    {
        // Create a JSON-friendly representation of the comparison result
        var jsonModel = CreateJsonModel(result);

        // Serialize to JSON
        return JsonSerializer.Serialize(jsonModel, _jsonOptions);
    }

    private static JsonComparisonResult CreateJsonModel(ComparisonResult result)
    {
        // Create a JSON-specific model that includes all necessary information
        var jsonResult = new JsonComparisonResult
        {
            OldAssemblyPath = result.OldAssemblyPath,
            NewAssemblyPath = result.NewAssemblyPath,
            ComparisonTimestamp = result.ComparisonTimestamp,
            HasBreakingChanges = result.HasBreakingChanges,
            TotalDifferences = result.TotalDifferences,
            Summary = new JsonComparisonSummary
            {
                AddedCount = result.Summary.AddedCount,
                RemovedCount = result.Summary.RemovedCount,
                ModifiedCount = result.Summary.ModifiedCount,
                BreakingChangesCount = result.Summary.BreakingChangesCount,
                TotalChanges = result.Summary.TotalChanges
            }
        };

        // Group differences by change type for better organization
        var addedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Added).ToList();
        var removedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Removed).ToList();
        var modifiedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Modified).ToList();
        var excludedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Excluded).ToList();
        var movedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Moved).ToList();

        // Convert differences to JSON model
        jsonResult.Added = addedItems.Select(ConvertToJsonDifference).ToList();
        jsonResult.Removed = removedItems.Select(ConvertToJsonDifference).ToList();
        jsonResult.Modified = modifiedItems.Select(ConvertToJsonDifference).ToList();
        jsonResult.Excluded = excludedItems.Select(ConvertToJsonDifference).ToList();
        jsonResult.Moved = movedItems.Select(ConvertToJsonDifference).ToList();

        // Add breaking changes separately for easy access
        jsonResult.BreakingChanges = result.Differences
            .Where(d => d.IsBreakingChange)
            .Select(ConvertToJsonDifference)
            .ToList();

        return jsonResult;
    }

    private static JsonApiDifference ConvertToJsonDifference(ApiDifference difference)
    {
        return new JsonApiDifference
        {
            ChangeType = difference.ChangeType.ToString(),
            ElementType = difference.ElementType.ToString(),
            ElementName = difference.ElementName,
            Description = difference.Description,
            IsBreakingChange = difference.IsBreakingChange,
            Severity = difference.Severity.ToString(),
            OldSignature = difference.OldSignature,
            NewSignature = difference.NewSignature
        };
    }

    #region JSON Models

    /// <summary>
    /// JSON-specific representation of a comparison result
    /// </summary>
    private class JsonComparisonResult
    {
        public string OldAssemblyPath { get; set; } = string.Empty;

        public string NewAssemblyPath { get; set; } = string.Empty;

        public DateTime ComparisonTimestamp { get; set; }

        public bool HasBreakingChanges { get; set; }

        public int TotalDifferences { get; set; }

        public JsonComparisonSummary Summary { get; set; } = new();

        public List<JsonApiDifference> Added { get; set; } = new();

        public List<JsonApiDifference> Removed { get; set; } = new();

        public List<JsonApiDifference> Modified { get; set; } = new();

        public List<JsonApiDifference> Excluded { get; set; } = new();

        public List<JsonApiDifference> Moved { get; set; } = new();

        public List<JsonApiDifference> BreakingChanges { get; set; } = new();
    }

    /// <summary>
    /// JSON-specific representation of comparison summary
    /// </summary>
    private class JsonComparisonSummary
    {
        public int AddedCount { get; set; }

        public int RemovedCount { get; set; }

        public int ModifiedCount { get; set; }

        public int BreakingChangesCount { get; set; }

        public int TotalChanges { get; set; }
    }

    /// <summary>
    /// JSON-specific representation of an API difference
    /// </summary>
    private class JsonApiDifference
    {
        public string ChangeType { get; set; } = string.Empty;

        public string ElementType { get; set; } = string.Empty;

        public string ElementName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsBreakingChange { get; set; }

        public string Severity { get; set; } = string.Empty;

        public string? OldSignature { get; set; }

        public string? NewSignature { get; set; }
    }

    #endregion
}
