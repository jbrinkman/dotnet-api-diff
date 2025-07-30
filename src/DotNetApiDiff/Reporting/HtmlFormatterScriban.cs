// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using DotNetApiDiff.Models.Configuration;
using Scriban;
using Scriban.Runtime;
using System.Linq;

namespace DotNetApiDiff.Reporting;

/// <summary>
/// Formatter for HTML output with rich formatting and interactive features using Scriban templates
/// </summary>
public class HtmlFormatterScriban : IReportFormatter
{
    private readonly Template _mainTemplate;

    public HtmlFormatterScriban()
    {
        // Initialize main template from embedded resources
        try
        {
            _mainTemplate = Template.Parse(EmbeddedTemplateLoader.LoadTemplate("main-layout.scriban"));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize Scriban HTML formatter templates. Ensure template resources are properly embedded.", ex);
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
        scriptObject.Import("render_change_group", new Func<object, string>(RenderChangeGroup));

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
        context.TemplateLoader = new CustomTemplateLoader();

        return _mainTemplate.Render(context);
    }

    private object PrepareConfigData(ComparisonConfiguration config)
    {
        var namespaceMappings = config?.Mappings?.NamespaceMappings ?? new Dictionary<string, List<string>>();

        // Convert Dictionary to array of objects with key/value properties for Scriban
        var namespaceMappingsArray = namespaceMappings.Select(kvp => new { key = kvp.Key, value = kvp.Value }).ToList();
        var typeMappingsArray = (config?.Mappings?.TypeMappings ?? new Dictionary<string, string>()).Select(kvp => new { key = kvp.Key, value = kvp.Value }).ToList();

        var mappingsResult = new
        {
            namespace_mappings = namespaceMappingsArray,
            type_mappings = typeMappingsArray,
            auto_map_same_name_types = config?.Mappings?.AutoMapSameNameTypes ?? false,
            ignore_case = config?.Mappings?.IgnoreCase ?? false
        };

        return new
        {
            filters = new
            {
                include_internals = config?.Filters?.IncludeInternals ?? false,
                include_compiler_generated = config?.Filters?.IncludeCompilerGenerated ?? false,
                include_namespaces = config?.Filters?.IncludeNamespaces?.ToList() ?? new List<string>(),
                exclude_namespaces = config?.Filters?.ExcludeNamespaces?.ToList() ?? new List<string>(),
                include_types = config?.Filters?.IncludeTypes?.ToList() ?? new List<string>(),
                exclude_types = config?.Filters?.ExcludeTypes?.ToList() ?? new List<string>()
            },
            mappings = mappingsResult,
            exclusions = new
            {
                excluded_types = config?.Exclusions?.ExcludedTypes?.ToList() ?? new List<string>(),
                excluded_members = config?.Exclusions?.ExcludedMembers?.ToList() ?? new List<string>(),
                excluded_type_patterns = config?.Exclusions?.ExcludedTypePatterns?.ToList() ?? new List<string>(),
                excluded_member_patterns = config?.Exclusions?.ExcludedMemberPatterns?.ToList() ?? new List<string>(),
                exclude_compiler_generated = config?.Exclusions?.ExcludeCompilerGenerated ?? false,
                exclude_obsolete = config?.Exclusions?.ExcludeObsolete ?? false
            },
            breaking_change_rules = new
            {
                treat_type_removal_as_breaking = config?.BreakingChangeRules?.TreatTypeRemovalAsBreaking ?? true,
                treat_member_removal_as_breaking = config?.BreakingChangeRules?.TreatMemberRemovalAsBreaking ?? true,
                treat_signature_change_as_breaking = config?.BreakingChangeRules?.TreatSignatureChangeAsBreaking ?? true,
                treat_reduced_accessibility_as_breaking = config?.BreakingChangeRules?.TreatReducedAccessibilityAsBreaking ?? true,
                treat_added_type_as_breaking = config?.BreakingChangeRules?.TreatAddedTypeAsBreaking ?? false,
                treat_added_member_as_breaking = config?.BreakingChangeRules?.TreatAddedMemberAsBreaking ?? false,
                treat_added_interface_as_breaking = config?.BreakingChangeRules?.TreatAddedInterfaceAsBreaking ?? true,
                treat_removed_interface_as_breaking = config?.BreakingChangeRules?.TreatRemovedInterfaceAsBreaking ?? true,
                treat_parameter_name_change_as_breaking = config?.BreakingChangeRules?.TreatParameterNameChangeAsBreaking ?? false,
                treat_added_optional_parameter_as_breaking = config?.BreakingChangeRules?.TreatAddedOptionalParameterAsBreaking ?? false
            },
            output_format = config?.OutputFormat.ToString() ?? "Console",
            output_path = config?.OutputPath ?? string.Empty,
            fail_on_breaking_changes = config?.FailOnBreakingChanges ?? false
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
            configuration = PrepareConfigData(result.Configuration),
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
                    old_signature = HtmlEscape(c.OldSignature),
                    new_signature = HtmlEscape(c.NewSignature),
                    details_id = $"details-{Guid.NewGuid():N}"
                }).ToArray()
            }).ToArray();
    }

    private static string HtmlEscape(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
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

    private string FormatBooleanValue(bool value)
    {
        return value ? "<span class=\"boolean-true\">✓ True</span>" : "<span class=\"boolean-false\">✗ False</span>";
    }

    private string RenderChangeGroup(object sectionData)
    {
        try
        {
            // Load and parse the change-group template
            var templateContent = EmbeddedTemplateLoader.LoadTemplate("change-group.scriban");
            var template = Template.Parse(templateContent);

            // Create a new context for the template with the section data as root
            var context = new TemplateContext();
            var scriptObject = new ScriptObject();

            // Add the section data properties to the script object
            if (sectionData != null)
            {
                var sectionType = sectionData.GetType();
                foreach (var property in sectionType.GetProperties())
                {
                    var value = property.GetValue(sectionData);
                    scriptObject.SetValue(property.Name.ToLowerInvariant(), value, true);
                }
            }

            context.PushGlobal(scriptObject);

            return template.Render(context);
        }
        catch (Exception ex)
        {
            return $"<!-- Error rendering change group: {ex.Message} -->";
        }
    }

    private string GetCssStyles()
    {
        try
        {
            return EmbeddedTemplateLoader.LoadStyles();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Could not load CSS styles, using fallback: {ex.Message}");
            return GetFallbackStyles();
        }
    }

    private string GetJavaScriptCode()
    {
        try
        {
            return EmbeddedTemplateLoader.LoadScripts();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Could not load JavaScript, using fallback: {ex.Message}");
            return GetFallbackJavaScript();
        }
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
