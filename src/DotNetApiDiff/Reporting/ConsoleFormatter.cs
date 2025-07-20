// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using Spectre.Console;
using System.Text;

namespace DotNetApiDiff.Reporting;

/// <summary>
/// Formatter for console output with colored text for different change types
/// </summary>
public class ConsoleFormatter : IReportFormatter
{
    private readonly bool _testMode;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleFormatter"/> class.
    /// </summary>
    /// <param name="testMode">Whether to run in test mode (simplified output)</param>
    public ConsoleFormatter(bool testMode = false)
    {
        _testMode = testMode;
    }

    /// <summary>
    /// Formats a comparison result for console output with colored text
    /// </summary>
    /// <param name="result">The comparison result to format</param>
    /// <returns>Formatted console output as a string</returns>
    public string Format(ComparisonResult result)
    {
        if (_testMode)
        {
            return FormatForTests(result);
        }

        var output = new StringBuilder();

        // Create header with assembly information
        output.AppendLine(FormatHeader(result));
        output.AppendLine();

        // Format summary statistics
        output.AppendLine(FormatSummary(result));
        output.AppendLine();

        // Format breaking changes (if any)
        if (result.HasBreakingChanges)
        {
            output.AppendLine(FormatBreakingChanges(result));
            output.AppendLine();
        }

        // Format all differences by category
        output.AppendLine(FormatDetailedChanges(result));

        return output.ToString();
    }

    private string FormatForTests(ComparisonResult result)
    {
        var output = new StringBuilder();

        // Header
        output.AppendLine("API Comparison Report");
        output.AppendLine($"Source Assembly: {Path.GetFileName(result.OldAssemblyPath)}");
        output.AppendLine($"Target Assembly: {Path.GetFileName(result.NewAssemblyPath)}");
        output.AppendLine($"Comparison Date: {result.ComparisonTimestamp:yyyy-MM-dd HH:mm:ss}");
        output.AppendLine($"Total Differences: {result.TotalDifferences}");

        if (result.HasBreakingChanges)
        {
            output.AppendLine($"Breaking Changes: {result.Differences.Count(d => d.IsBreakingChange)}");
        }

        output.AppendLine();

        // Summary
        output.AppendLine("Summary");
        output.AppendLine($"Added: {result.Summary.AddedCount}");
        output.AppendLine($"Removed: {result.Summary.RemovedCount}");
        output.AppendLine($"Modified: {result.Summary.ModifiedCount}");
        output.AppendLine($"Breaking Changes: {result.Summary.BreakingChangesCount}");
        output.AppendLine($"Total Changes: {result.Summary.TotalChanges}");
        output.AppendLine();

        // Breaking Changes
        if (result.HasBreakingChanges)
        {
            output.AppendLine("Breaking Changes");
            foreach (var change in result.Differences.Where(d => d.IsBreakingChange))
            {
                output.AppendLine($"{change.ElementType} | {change.ElementName} | {change.Description} | {change.Severity}");
            }
            output.AppendLine();
        }

        // Group differences by change type
        var addedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Added).ToList();
        var removedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Removed).ToList();
        var modifiedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Modified).ToList();
        var excludedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Excluded).ToList();

        // Added Items
        if (addedItems.Any())
        {
            output.AppendLine($"Added Items ({addedItems.Count})");
            foreach (var change in addedItems)
            {
                output.AppendLine($"{change.ElementType} | {change.ElementName} | {change.Description}");
                if (!string.IsNullOrEmpty(change.NewSignature))
                {
                    output.AppendLine($"+ {change.NewSignature}");
                }
            }
            output.AppendLine();
        }

        // Removed Items
        if (removedItems.Any())
        {
            output.AppendLine($"Removed Items ({removedItems.Count})");
            foreach (var change in removedItems)
            {
                output.AppendLine($"{change.ElementType} | {change.ElementName} | {change.Description}");
                if (!string.IsNullOrEmpty(change.OldSignature))
                {
                    output.AppendLine($"- {change.OldSignature}");
                }
            }
            output.AppendLine();
        }

        // Modified Items
        if (modifiedItems.Any())
        {
            output.AppendLine($"Modified Items ({modifiedItems.Count})");
            foreach (var change in modifiedItems)
            {
                output.AppendLine($"{change.ElementType} | {change.ElementName} | {change.Description}");
                if (!string.IsNullOrEmpty(change.OldSignature))
                {
                    output.AppendLine($"- {change.OldSignature}");
                }
                if (!string.IsNullOrEmpty(change.NewSignature))
                {
                    output.AppendLine($"+ {change.NewSignature}");
                }
            }
            output.AppendLine();
        }

        // Excluded Items
        if (excludedItems.Any())
        {
            output.AppendLine($"Excluded Items ({excludedItems.Count})");
            foreach (var change in excludedItems)
            {
                output.AppendLine($"{change.ElementType} | {change.ElementName} | {change.Description}");
            }
        }

        return output.ToString();
    }

    private string FormatHeader(ComparisonResult result)
    {
        var table = new Table();

        table.AddColumn("API Comparison Report");
        table.AddColumn(new TableColumn("Value").RightAligned());

        table.AddRow("Source Assembly", Path.GetFileName(result.OldAssemblyPath));
        table.AddRow("Target Assembly", Path.GetFileName(result.NewAssemblyPath));
        table.AddRow("Comparison Date", result.ComparisonTimestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        table.AddRow("Total Differences", result.TotalDifferences.ToString());

        if (result.HasBreakingChanges)
        {
            table.AddRow("[bold red]Breaking Changes[/]", $"[bold red]{result.Differences.Count(d => d.IsBreakingChange)}[/]");
        }

        return table.ToString() ?? string.Empty;
    }

    private string FormatSummary(ComparisonResult result)
    {
        var panel = new Panel(new Rows(
            new Text("Summary Statistics"),
            new Text(" "),
            new Markup($"Added: [green]{result.Summary.AddedCount}[/]"),
            new Markup($"Removed: [red]{result.Summary.RemovedCount}[/]"),
            new Markup($"Modified: [yellow]{result.Summary.ModifiedCount}[/]"),
            new Markup($"Breaking Changes: [bold red]{result.Summary.BreakingChangesCount}[/]"),
            new Text(" "),
            new Markup($"Total Changes: [blue]{result.Summary.TotalChanges}[/]")
        ))
        {
            Header = new PanelHeader("Summary"),
            Border = BoxBorder.Rounded,
            Expand = true
        };

        return panel.ToString() ?? string.Empty;
    }

    private string FormatBreakingChanges(ComparisonResult result)
    {
        var breakingChanges = result.Differences.Where(d => d.IsBreakingChange).ToList();

        if (!breakingChanges.Any())
        {
            return string.Empty;
        }

        var table = new Table();
        table.AddColumn("Type");
        table.AddColumn("Element");
        table.AddColumn("Description");
        table.AddColumn("Severity");

        table.Title = new TableTitle("[bold red]Breaking Changes[/]");
        table.Border = TableBorder.Rounded;

        foreach (var change in breakingChanges)
        {
            string severityText = change.Severity switch
            {
                SeverityLevel.Critical => $"[bold red]{change.Severity}[/]",
                SeverityLevel.Error => $"[red]{change.Severity}[/]",
                SeverityLevel.Warning => $"[yellow]{change.Severity}[/]",
                _ => $"[blue]{change.Severity}[/]"
            };

            table.AddRow(
                change.ElementType.ToString(),
                change.ElementName,
                change.Description,
                severityText
            );
        }

        return table.ToString() ?? string.Empty;
    }

    private string FormatDetailedChanges(ComparisonResult result)
    {
        var output = new StringBuilder();

        // Group differences by change type
        var addedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Added).ToList();
        var removedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Removed).ToList();
        var modifiedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Modified).ToList();
        var excludedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Excluded).ToList();

        // Format added items
        if (addedItems.Any())
        {
            output.AppendLine(FormatChangeGroup("Added Items", addedItems, "green"));
            output.AppendLine();
        }

        // Format removed items
        if (removedItems.Any())
        {
            output.AppendLine(FormatChangeGroup("Removed Items", removedItems, "red"));
            output.AppendLine();
        }

        // Format modified items
        if (modifiedItems.Any())
        {
            output.AppendLine(FormatChangeGroup("Modified Items", modifiedItems, "yellow"));
            output.AppendLine();
        }

        // Format excluded items
        if (excludedItems.Any())
        {
            output.AppendLine(FormatChangeGroup("Excluded Items", excludedItems, "gray"));
        }

        return output.ToString();
    }

    private string FormatChangeGroup(string title, List<ApiDifference> changes, string color)
    {
        var table = new Table();

        table.AddColumn("Type");
        table.AddColumn("Element");
        table.AddColumn("Details");

        if (changes.Any(c => c.IsBreakingChange))
        {
            table.AddColumn("Breaking");
        }

        table.Title = new TableTitle($"[bold {color}]{title}[/] ({changes.Count})");
        table.Border = TableBorder.Rounded;

        // Group changes by element type for better organization
        var groupedChanges = changes.GroupBy(c => c.ElementType).OrderBy(g => g.Key);

        foreach (var group in groupedChanges)
        {
            foreach (var change in group.OrderBy(c => c.ElementName))
            {
                var row = new List<string>
                {
                    change.ElementType.ToString(),
                    change.ElementName,
                    FormatChangeDetails(change)
                };

                if (changes.Any(c => c.IsBreakingChange))
                {
                    row.Add(change.IsBreakingChange ? "[red]Yes[/]" : "No");
                }

                table.AddRow(row.ToArray());
            }

            // Add a separator between groups
            if (group.Key != groupedChanges.Last().Key)
            {
                table.AddEmptyRow();
            }
        }

        return table.ToString() ?? string.Empty;
    }

    private string FormatChangeDetails(ApiDifference change)
    {
        var details = new StringBuilder(change.Description);

        if (!string.IsNullOrEmpty(change.OldSignature) && !string.IsNullOrEmpty(change.NewSignature))
        {
            details.AppendLine();
            details.AppendLine($"[red]- {change.OldSignature}[/]");
            details.AppendLine($"[green]+ {change.NewSignature}[/]");
        }
        else if (!string.IsNullOrEmpty(change.OldSignature))
        {
            details.AppendLine();
            details.AppendLine($"[red]- {change.OldSignature}[/]");
        }
        else if (!string.IsNullOrEmpty(change.NewSignature))
        {
            details.AppendLine();
            details.AppendLine($"[green]+ {change.NewSignature}[/]");
        }

        return details.ToString();
    }
}
