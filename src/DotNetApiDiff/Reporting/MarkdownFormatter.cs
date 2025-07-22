// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using System.Text;

namespace DotNetApiDiff.Reporting;

/// <summary>
/// Formatter for Markdown output with human-readable documentation
/// </summary>
public class MarkdownFormatter : IReportFormatter
{
    /// <summary>
    /// Formats a comparison result as Markdown
    /// </summary>
    /// <param name="result">The comparison result to format</param>
    /// <returns>Formatted Markdown output as a string</returns>
    public string Format(ComparisonResult result)
    {
        var output = new StringBuilder();

        // Create header with title and assembly information
        output.AppendLine("# API Comparison Report");
        output.AppendLine();

        // Add metadata section
        output.AppendLine("## Metadata");
        output.AppendLine();
        output.AppendLine("| Property | Value |");
        output.AppendLine("|----------|-------|");
        output.AppendLine($"| Source Assembly | `{Path.GetFileName(result.OldAssemblyPath)}` |");
        output.AppendLine($"| Target Assembly | `{Path.GetFileName(result.NewAssemblyPath)}` |");
        output.AppendLine($"| Comparison Date | {result.ComparisonTimestamp:yyyy-MM-dd HH:mm:ss} |");
        output.AppendLine($"| Total Differences | {result.TotalDifferences} |");

        if (result.HasBreakingChanges)
        {
            output.AppendLine($"| Breaking Changes | **{result.Differences.Count(d => d.IsBreakingChange)}** |");
        }

        output.AppendLine();

        // Add summary section
        output.AppendLine("## Summary");
        output.AppendLine();
        output.AppendLine("| Change Type | Count |");
        output.AppendLine("|-------------|-------|");
        output.AppendLine($"| Added | {result.Summary.AddedCount} |");
        output.AppendLine($"| Removed | {result.Summary.RemovedCount} |");
        output.AppendLine($"| Modified | {result.Summary.ModifiedCount} |");
        output.AppendLine($"| Breaking Changes | {result.Summary.BreakingChangesCount} |");
        output.AppendLine($"| **Total Changes** | **{result.Summary.TotalChanges}** |");
        output.AppendLine();

        // Add breaking changes section if any exist
        if (result.HasBreakingChanges)
        {
            output.AppendLine("## Breaking Changes");
            output.AppendLine();
            output.AppendLine("The following changes may break compatibility with existing code:");
            output.AppendLine();

            output.AppendLine("| Type | Element | Description | Severity |");
            output.AppendLine("|------|---------|-------------|----------|");

            foreach (var change in result.Differences.Where(d => d.IsBreakingChange).OrderBy(d => d.Severity).ThenBy(d => d.ElementType).ThenBy(d => d.ElementName))
            {
                string severityText = change.Severity switch
                {
                    SeverityLevel.Critical => $"**{change.Severity}**",
                    SeverityLevel.Error => change.Severity.ToString(),
                    SeverityLevel.Warning => change.Severity.ToString(),
                    _ => change.Severity.ToString()
                };

                output.AppendLine($"| {change.ElementType} | `{change.ElementName}` | {change.Description} | {severityText} |");
            }

            output.AppendLine();
        }

        // Group differences by change type
        var addedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Added).ToList();
        var removedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Removed).ToList();
        var modifiedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Modified).ToList();
        var excludedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Excluded).ToList();
        var movedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Moved).ToList();

        // Add detailed sections for each change type
        if (addedItems.Any())
        {
            output.AppendLine($"## Added Items ({addedItems.Count})");
            output.AppendLine();
            FormatChangeGroup(output, addedItems);
        }

        if (removedItems.Any())
        {
            output.AppendLine($"## Removed Items ({removedItems.Count})");
            output.AppendLine();
            FormatChangeGroup(output, removedItems);
        }

        if (modifiedItems.Any())
        {
            output.AppendLine($"## Modified Items ({modifiedItems.Count})");
            output.AppendLine();
            FormatChangeGroup(output, modifiedItems);
        }

        if (movedItems.Any())
        {
            output.AppendLine($"## Moved Items ({movedItems.Count})");
            output.AppendLine();
            FormatChangeGroup(output, movedItems);
        }

        // Add excluded items section if any exist (requirement 7.2)
        if (excludedItems.Any())
        {
            output.AppendLine($"## Excluded/Unsupported Items ({excludedItems.Count})");
            output.AppendLine();
            output.AppendLine("The following items were intentionally excluded from the comparison:");
            output.AppendLine();
            FormatChangeGroup(output, excludedItems);
        }

        return output.ToString();
    }

    private void FormatChangeGroup(StringBuilder output, List<ApiDifference> changes)
    {
        // Group changes by element type for better organization
        var groupedChanges = changes.GroupBy(c => c.ElementType).OrderBy(g => g.Key);

        foreach (var group in groupedChanges)
        {
            output.AppendLine($"### {group.Key}");
            output.AppendLine();

            output.AppendLine("| Element | Description | Breaking |");
            output.AppendLine("|---------|-------------|----------|");

            foreach (var change in group.OrderBy(c => c.ElementName))
            {
                string breakingText = change.IsBreakingChange ? "Yes" : "No";
                output.AppendLine($"| `{change.ElementName}` | {change.Description} | {breakingText} |");

                // Add signature details if available
                if (!string.IsNullOrEmpty(change.OldSignature) || !string.IsNullOrEmpty(change.NewSignature))
                {
                    output.AppendLine();
                    output.AppendLine("<details>");
                    output.AppendLine("<summary>Signature Details</summary>");
                    output.AppendLine();

                    if (!string.IsNullOrEmpty(change.OldSignature))
                    {
                        output.AppendLine("**Old:**");
                        output.AppendLine("```csharp");
                        output.AppendLine(change.OldSignature);
                        output.AppendLine("```");
                    }

                    if (!string.IsNullOrEmpty(change.NewSignature))
                    {
                        output.AppendLine("**New:**");
                        output.AppendLine("```csharp");
                        output.AppendLine(change.NewSignature);
                        output.AppendLine("```");
                    }

                    output.AppendLine("</details>");
                }
            }

            output.AppendLine();
        }
    }
}
