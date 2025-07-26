// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using DotNetApiDiff.Models.Configuration;
using Scriban;
using Scriban.Runtime;
using System.Text;

namespace DotNetApiDiff.Reporting;

/// <summary>
/// Formatter for HTML output with rich formatting and interactive features using Scriban templates
/// </summary>
public class HtmlFormatterScriban : IReportFormatter
{
    private readonly Template _mainTemplate;
    private readonly Dictionary<string, Template> _partialTemplates;

    public HtmlFormatterScriban()
    {
        // Initialize templates from embedded resources
        try
        {
            _mainTemplate = Template.Parse(EmbeddedTemplateLoader.LoadTemplate("main-layout.scriban"));

            _partialTemplates = new Dictionary<string, Template>();

            // Load all partial templates
            _partialTemplates["change-group"] = Template.Parse(EmbeddedTemplateLoader.LoadTemplate("change-group.scriban"));
            _partialTemplates["breaking-changes"] = Template.Parse(EmbeddedTemplateLoader.LoadTemplate("breaking-changes.scriban"));
            _partialTemplates["configuration"] = Template.Parse(EmbeddedTemplateLoader.LoadTemplate("configuration.scriban"));
            _partialTemplates["config-string-list"] = Template.Parse(EmbeddedTemplateLoader.LoadTemplate("config-string-list.scriban"));
            _partialTemplates["config-mappings"] = Template.Parse(EmbeddedTemplateLoader.LoadTemplate("config-mappings.scriban"));
            _partialTemplates["config-namespace-mappings"] = Template.Parse(EmbeddedTemplateLoader.LoadTemplate("config-namespace-mappings.scriban"));
        }
        catch (Exception ex)
        {
            // Fallback to basic template if templates can't be loaded
            Console.WriteLine($"Warning: Could not load templates, using fallback: {ex.Message}");
            _mainTemplate = Template.Parse(GetFallbackTemplate());
            _partialTemplates = new Dictionary<string, Template>();
        }
    }

    /// <summary>
    /// Formats a comparison result as HTML using Scriban templates
    /// </summary>
    /// <param name="result">The comparison result to format</param>
    /// <returns>Formatted HTML output as a string</returns>
    public string Format(ComparisonResult result)
    {
        // Create the template context with custom functions
        var context = new TemplateContext();
        var scriptObject = new ScriptObject();

        // Add custom functions
        scriptObject.Import("format_boolean", new Func<bool, string>(FormatBooleanValue));

        // Prepare data for the main template
        var resultData = PrepareResultData(result);
        var changeSections = PrepareChangeSections(result);
        var cssStyles = GetCssStyles();
        var javascriptCode = GetJavaScriptCode();

        // Add template data to script object
        scriptObject.SetValue("result", resultData, true);
        scriptObject.SetValue("change_sections", changeSections, true);
        scriptObject.SetValue("css_styles", cssStyles, true);
        scriptObject.SetValue("javascript_code", javascriptCode, true);
        scriptObject.SetValue("config", PrepareConfigData(result.Configuration), true);

        context.PushGlobal(scriptObject);

        // Set up template loader for includes
        context.TemplateLoader = new CustomTemplateLoader(_partialTemplates);

        return _mainTemplate.Render(context);
    }

    private object PrepareConfigData(ComparisonConfiguration config)
    {
        return new
        {
            filters = new
            {
                include_internals = config.Filters.IncludeInternals,
                include_compiler_generated = config.Filters.IncludeCompilerGenerated,
                include_namespaces = config.Filters.IncludeNamespaces?.ToList() ?? new List<string>(),
                exclude_namespaces = config.Filters.ExcludeNamespaces?.ToList() ?? new List<string>(),
                include_types = config.Filters.IncludeTypes?.ToList() ?? new List<string>(),
                exclude_types = config.Filters.ExcludeTypes?.ToList() ?? new List<string>()
            },
            mappings = new
            {
                namespace_mappings = config.Mappings.NamespaceMappings ?? new Dictionary<string, List<string>>(),
                type_mappings = config.Mappings.TypeMappings ?? new Dictionary<string, string>(),
                auto_map_same_name_types = config.Mappings.AutoMapSameNameTypes,
                ignore_case = config.Mappings.IgnoreCase
            },
            exclusions = new
            {
                excluded_types = config.Exclusions.ExcludedTypes?.ToList() ?? new List<string>(),
                excluded_members = config.Exclusions.ExcludedMembers?.ToList() ?? new List<string>(),
                excluded_type_patterns = config.Exclusions.ExcludedTypePatterns?.ToList() ?? new List<string>(),
                excluded_member_patterns = config.Exclusions.ExcludedMemberPatterns?.ToList() ?? new List<string>(),
                exclude_compiler_generated = config.Exclusions.ExcludeCompilerGenerated,
                exclude_obsolete = config.Exclusions.ExcludeObsolete
            },
            breaking_change_rules = new
            {
                treat_type_removal_as_breaking = config.BreakingChangeRules.TreatTypeRemovalAsBreaking,
                treat_member_removal_as_breaking = config.BreakingChangeRules.TreatMemberRemovalAsBreaking,
                treat_signature_change_as_breaking = config.BreakingChangeRules.TreatSignatureChangeAsBreaking,
                treat_reduced_accessibility_as_breaking = config.BreakingChangeRules.TreatReducedAccessibilityAsBreaking
            },
            output_format = config.OutputFormat.ToString(),
            output_path = config.OutputPath ?? ""
        };
    }

    private object PrepareResultData(ComparisonResult result)
    {
        return new
        {
            comparison_timestamp = result.ComparisonTimestamp,
            old_assembly_name = Path.GetFileName(result.OldAssemblyPath),
            old_assembly_path = result.OldAssemblyPath,
            new_assembly_name = Path.GetFileName(result.NewAssemblyPath),
            new_assembly_path = result.NewAssemblyPath,
            total_differences = result.TotalDifferences,
            has_breaking_changes = result.HasBreakingChanges,
            breaking_changes_count = result.Differences.Count(d => d.IsBreakingChange),
            summary = new
            {
                added_count = result.Summary.AddedCount,
                removed_count = result.Summary.RemovedCount,
                modified_count = result.Summary.ModifiedCount,
                breaking_changes_count = result.Summary.BreakingChangesCount
            },
            configuration = PrepareConfigurationData(result.Configuration),
            breaking_changes = PrepareBreakingChangesData(result.Differences.Where(d => d.IsBreakingChange))
        };
    }

    private object[] PrepareChangeSections(ComparisonResult result)
    {
        var sections = new List<object>();

        var addedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Added).ToList();
        if (addedItems.Any())
        {
            sections.Add(new
            {
                icon = "➕",
                title = "Added Items",
                count = addedItems.Count,
                change_type = "added",
                grouped_changes = GroupChanges(addedItems)
            });
        }

        var removedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Removed).ToList();
        if (removedItems.Any())
        {
            sections.Add(new
            {
                icon = "➖",
                title = "Removed Items",
                count = removedItems.Count,
                change_type = "removed",
                grouped_changes = GroupChanges(removedItems)
            });
        }

        var modifiedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Modified).ToList();
        if (modifiedItems.Any())
        {
            sections.Add(new
            {
                icon = "🔄",
                title = "Modified Items",
                count = modifiedItems.Count,
                change_type = "modified",
                grouped_changes = GroupChanges(modifiedItems)
            });
        }

        var movedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Moved).ToList();
        if (movedItems.Any())
        {
            sections.Add(new
            {
                icon = "📦",
                title = "Moved Items",
                count = movedItems.Count,
                change_type = "moved",
                grouped_changes = GroupChanges(movedItems)
            });
        }

        var excludedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Excluded).ToList();
        if (excludedItems.Any())
        {
            sections.Add(new
            {
                icon = "🚫",
                title = "Excluded Items",
                count = excludedItems.Count,
                change_type = "excluded",
                description = "The following items were intentionally excluded from the comparison:",
                grouped_changes = GroupChanges(excludedItems)
            });
        }

        return sections.ToArray();
    }

    private object[] GroupChanges(List<ApiDifference> changes)
    {
        return changes.GroupBy(c => c.ElementType)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                key = g.Key,
                count = g.Count(),
                changes = g.OrderBy(c => c.ElementName).Select(c => new
                {
                    element_name = c.ElementName,
                    description = c.Description,
                    is_breaking_change = c.IsBreakingChange,
                    has_signatures = !string.IsNullOrEmpty(c.OldSignature) || !string.IsNullOrEmpty(c.NewSignature),
                    old_signature = c.OldSignature,
                    new_signature = c.NewSignature,
                    details_id = $"details-{Guid.NewGuid():N}"
                }).ToArray()
            }).ToArray();
    }

    private object[] PrepareBreakingChangesData(IEnumerable<ApiDifference> breakingChanges)
    {
        return breakingChanges.OrderBy(d => d.Severity)
            .ThenBy(d => d.ElementType)
            .ThenBy(d => d.ElementName)
            .Select(change => new
            {
                severity = change.Severity.ToString(),
                severity_class = change.Severity.ToString().ToLower(),
                element_type = change.ElementType,
                element_name = change.ElementName,
                description = change.Description
            }).ToArray();
    }

    private object PrepareConfigurationData(DotNetApiDiff.Models.Configuration.ComparisonConfiguration config)
    {
        return new
        {
            filters = new
            {
                include_internals = config.Filters.IncludeInternals,
                include_compiler_generated = config.Filters.IncludeCompilerGenerated,
                include_namespaces = config.Filters.IncludeNamespaces,
                exclude_namespaces = config.Filters.ExcludeNamespaces,
                include_types = config.Filters.IncludeTypes,
                exclude_types = config.Filters.ExcludeTypes
            },
            mappings = new
            {
                auto_map_same_name_types = config.Mappings.AutoMapSameNameTypes,
                ignore_case = config.Mappings.IgnoreCase,
                type_mappings = config.Mappings.TypeMappings.Select(kvp => new { key = kvp.Key, value = kvp.Value }),
                namespace_mappings = config.Mappings.NamespaceMappings.Select(kvp => new { key = kvp.Key, value = kvp.Value })
            },
            exclusions = new
            {
                excluded_types = config.Exclusions.ExcludedTypes,
                excluded_members = config.Exclusions.ExcludedMembers,
                excluded_type_patterns = config.Exclusions.ExcludedTypePatterns,
                excluded_member_patterns = config.Exclusions.ExcludedMemberPatterns
            },
            breaking_change_rules = new
            {
                treat_type_removal_as_breaking = config.BreakingChangeRules.TreatTypeRemovalAsBreaking,
                treat_member_removal_as_breaking = config.BreakingChangeRules.TreatMemberRemovalAsBreaking,
                treat_signature_change_as_breaking = config.BreakingChangeRules.TreatSignatureChangeAsBreaking,
                treat_reduced_accessibility_as_breaking = config.BreakingChangeRules.TreatReducedAccessibilityAsBreaking
            },
            output_format = config.OutputFormat,
            fail_on_breaking_changes = config.FailOnBreakingChanges,
            output_path = config.OutputPath
        };
    }

    private string RenderPartial(string templateName, object data)
    {
        if (_partialTemplates.TryGetValue(templateName, out var template))
        {
            return template.Render(data, member => member.Name);
        }

        // Fallback for missing templates
        return $"<!-- Template '{templateName}' not found -->";
    }

    private string FormatBooleanValue(bool value)
    {
        return value ? "<span class=\"boolean-true\">✓ True</span>" : "<span class=\"boolean-false\">✗ False</span>";
    }

    private string GetCssStyles()
    {
        try
        {
            return EmbeddedTemplateLoader.LoadStyles();
        }
        catch
        {
            return GetFallbackStyles();
        }
    }

    private string GetJavaScriptCode()
    {
        try
        {
            return EmbeddedTemplateLoader.LoadScripts();
        }
        catch
        {
            return GetFallbackJavaScript();
        }
    }

    private string GetFallbackTemplate()
    {
        return @"<!DOCTYPE html>
<html>
<head>
    <title>API Comparison Report</title>
    <style>{{ css_styles }}</style>
</head>
<body>
    <h1>API Comparison Report</h1>
    <p>Generated on {{ result.comparison_timestamp }}</p>
    <p>Total Differences: {{ result.total_differences }}</p>
    <!-- Fallback template - source generator not available -->
    <script>{{ javascript_code }}</script>
</body>
</html>";
    }

    private string GetFallbackStyles()
    {
        return "body { font-family: Arial, sans-serif; margin: 20px; }";
    }

    private string GetFallbackJavaScript()
    {
        return "// Fallback JavaScript";
    }
}
