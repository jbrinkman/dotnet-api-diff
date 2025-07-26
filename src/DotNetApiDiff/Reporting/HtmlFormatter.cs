// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using System.Text;

namespace DotNetApiDiff.Reporting;

/// <summary>
/// Formatter for HTML output with rich formatting and interactive features
/// </summary>
public class HtmlFormatter : IReportFormatter
{
    /// <summary>
    /// Formats a comparison result as HTML
    /// </summary>
    /// <param name="result">The comparison result to format</param>
    /// <returns>Formatted HTML output as a string</returns>
    public string Format(ComparisonResult result)
    {
        var output = new StringBuilder();

        // HTML Document structure with CSS
        output.AppendLine("<!DOCTYPE html>");
        output.AppendLine("<html lang=\"en\">");
        output.AppendLine("<head>");
        output.AppendLine("    <meta charset=\"UTF-8\">");
        output.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        output.AppendLine("    <title>API Comparison Report</title>");
        output.AppendLine("    <style>");
        output.AppendLine(GetCssStyles());
        output.AppendLine("    </style>");
        output.AppendLine("</head>");
        output.AppendLine("<body>");

        // Header section
        output.AppendLine("    <div class=\"container\">");
        output.AppendLine("        <header>");
        output.AppendLine("            <h1>🔍 API Comparison Report</h1>");
        output.AppendLine("            <div class=\"report-info\">");
        output.AppendLine($"                Generated on {result.ComparisonTimestamp:yyyy-MM-dd HH:mm:ss}");
        output.AppendLine("            </div>");
        output.AppendLine("        </header>");

        // Metadata section
        output.AppendLine("        <section class=\"metadata\">");
        output.AppendLine("            <h2>📊 Metadata</h2>");
        output.AppendLine("            <div class=\"metadata-content\">");
        output.AppendLine("                <div class=\"assembly-info\">");
        output.AppendLine("                    <div class=\"assembly-card source\">");
        output.AppendLine("                        <div class=\"assembly-label\">Source Assembly</div>");
        output.AppendLine($"                        <div class=\"assembly-name\">{Path.GetFileName(result.OldAssemblyPath)}</div>");
        output.AppendLine($"                        <div class=\"assembly-path\">{result.OldAssemblyPath}</div>");
        output.AppendLine("                    </div>");
        output.AppendLine("                    <div class=\"assembly-card target\">");
        output.AppendLine("                        <div class=\"assembly-label\">Target Assembly</div>");
        output.AppendLine($"                        <div class=\"assembly-name\">{Path.GetFileName(result.NewAssemblyPath)}</div>");
        output.AppendLine($"                        <div class=\"assembly-path\">{result.NewAssemblyPath}</div>");
        output.AppendLine("                    </div>");
        output.AppendLine("                </div>");
        output.AppendLine("                <div class=\"stats-row\">");
        output.AppendLine("                    <div class=\"stat-item\">");
        output.AppendLine($"                        <div class=\"stat-value\">{result.TotalDifferences}</div>");
        output.AppendLine("                        <div class=\"stat-label\">Total Differences</div>");
        output.AppendLine("                    </div>");
        output.AppendLine("                    <div class=\"stat-item\">");
        output.AppendLine($"                        <div class=\"stat-value\">{result.ComparisonTimestamp:yyyy-MM-dd}</div>");
        output.AppendLine("                        <div class=\"stat-label\">Comparison Date</div>");
        output.AppendLine("                    </div>");

        if (result.HasBreakingChanges)
        {
            output.AppendLine("                    <div class=\"stat-item breaking\">");
            output.AppendLine($"                        <div class=\"stat-value\">{result.Differences.Count(d => d.IsBreakingChange)}</div>");
            output.AppendLine("                        <div class=\"stat-label\">Breaking Changes</div>");
            output.AppendLine("                    </div>");
        }

        output.AppendLine("                </div>");
        output.AppendLine("            </div>");
        output.AppendLine("        </section>");

        // Summary section
        output.AppendLine("        <section class=\"summary\">");
        output.AppendLine("            <h2>📈 Summary</h2>");
        output.AppendLine("            <div class=\"summary-cards\">");
        output.AppendLine($"                <div class=\"summary-card added\">");
        output.AppendLine($"                    <div class=\"card-number\">{result.Summary.AddedCount}</div>");
        output.AppendLine($"                    <div class=\"card-label\">Added</div>");
        output.AppendLine($"                </div>");
        output.AppendLine($"                <div class=\"summary-card removed\">");
        output.AppendLine($"                    <div class=\"card-number\">{result.Summary.RemovedCount}</div>");
        output.AppendLine($"                    <div class=\"card-label\">Removed</div>");
        output.AppendLine($"                </div>");
        output.AppendLine($"                <div class=\"summary-card modified\">");
        output.AppendLine($"                    <div class=\"card-number\">{result.Summary.ModifiedCount}</div>");
        output.AppendLine($"                    <div class=\"card-label\">Modified</div>");
        output.AppendLine($"                </div>");
        output.AppendLine($"                <div class=\"summary-card breaking\">");
        output.AppendLine($"                    <div class=\"card-number\">{result.Summary.BreakingChangesCount}</div>");
        output.AppendLine($"                    <div class=\"card-label\">Breaking</div>");
        output.AppendLine($"                </div>");
        output.AppendLine("            </div>");
        output.AppendLine("        </section>");

        // Configuration section
        output.AppendLine("        <section class=\"configuration\">");
        output.AppendLine("            <h2>⚙️ Configuration</h2>");
        output.AppendLine("            <div class=\"config-toggle\">");
        output.AppendLine("                <button onclick=\"toggleConfig()\" class=\"toggle-button\">");
        output.AppendLine("                    <span class=\"toggle-icon\">▶</span>");
        output.AppendLine("                    <span class=\"toggle-text\">Show Configuration Details</span>");
        output.AppendLine("                </button>");
        output.AppendLine("            </div>");
        output.AppendLine("            <div id=\"config-details\" class=\"config-details\" style=\"display: none;\">");
        FormatConfiguration(output, result.Configuration);
        output.AppendLine("            </div>");
        output.AppendLine("        </section>");

        // Breaking changes section
        if (result.HasBreakingChanges)
        {
            output.AppendLine("        <section class=\"breaking-changes\">");
            output.AppendLine("            <h2>⚠️ Breaking Changes</h2>");
            output.AppendLine("            <div class=\"alert alert-danger\">");
            output.AppendLine("                <strong>Warning:</strong> The following changes may break compatibility with existing code.");
            output.AppendLine("            </div>");

            var breakingChanges = result.Differences.Where(d => d.IsBreakingChange)
                .OrderBy(d => d.Severity)
                .ThenBy(d => d.ElementType)
                .ThenBy(d => d.ElementName);

            FormatBreakingChanges(output, breakingChanges);
            output.AppendLine("        </section>");
        }

        // Detailed changes sections
        var addedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Added).ToList();
        var removedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Removed).ToList();
        var modifiedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Modified).ToList();
        var excludedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Excluded).ToList();
        var movedItems = result.Differences.Where(d => d.ChangeType == ChangeType.Moved).ToList();

        if (addedItems.Any())
        {
            output.AppendLine("        <section class=\"changes-section\">");
            output.AppendLine($"            <h2>➕ Added Items ({addedItems.Count})</h2>");
            FormatChangeGroup(output, addedItems, "added");
            output.AppendLine("        </section>");
        }

        if (removedItems.Any())
        {
            output.AppendLine("        <section class=\"changes-section\">");
            output.AppendLine($"            <h2>➖ Removed Items ({removedItems.Count})</h2>");
            FormatChangeGroup(output, removedItems, "removed");
            output.AppendLine("        </section>");
        }

        if (modifiedItems.Any())
        {
            output.AppendLine("        <section class=\"changes-section\">");
            output.AppendLine($"            <h2>🔄 Modified Items ({modifiedItems.Count})</h2>");
            FormatChangeGroup(output, modifiedItems, "modified");
            output.AppendLine("        </section>");
        }

        if (movedItems.Any())
        {
            output.AppendLine("        <section class=\"changes-section\">");
            output.AppendLine($"            <h2>📦 Moved Items ({movedItems.Count})</h2>");
            FormatChangeGroup(output, movedItems, "moved");
            output.AppendLine("        </section>");
        }

        if (excludedItems.Any())
        {
            output.AppendLine("        <section class=\"changes-section\">");
            output.AppendLine($"            <h2>🚫 Excluded Items ({excludedItems.Count})</h2>");
            output.AppendLine("            <p class=\"section-description\">The following items were intentionally excluded from the comparison:</p>");
            FormatChangeGroup(output, excludedItems, "excluded");
            output.AppendLine("        </section>");
        }

        // Footer
        output.AppendLine("    </div>");
        output.AppendLine("    <script>");
        output.AppendLine(GetJavaScript());
        output.AppendLine("    </script>");
        output.AppendLine("</body>");
        output.AppendLine("</html>");

        return output.ToString();
    }

    private void FormatBreakingChanges(StringBuilder output, IEnumerable<ApiDifference> breakingChanges)
    {
        output.AppendLine("            <div class=\"breaking-changes-table\">");
        output.AppendLine("                <table>");
        output.AppendLine("                    <thead>");
        output.AppendLine("                        <tr>");
        output.AppendLine("                            <th>Severity</th>");
        output.AppendLine("                            <th>Type</th>");
        output.AppendLine("                            <th>Element</th>");
        output.AppendLine("                            <th>Description</th>");
        output.AppendLine("                        </tr>");
        output.AppendLine("                    </thead>");
        output.AppendLine("                    <tbody>");

        foreach (var change in breakingChanges)
        {
            string severityClass = change.Severity.ToString().ToLower();
            output.AppendLine("                        <tr>");
            output.AppendLine($"                            <td><span class=\"severity {severityClass}\">{change.Severity}</span></td>");
            output.AppendLine($"                            <td>{change.ElementType}</td>");
            output.AppendLine($"                            <td><code>{change.ElementName}</code></td>");
            output.AppendLine($"                            <td>{change.Description}</td>");
            output.AppendLine("                        </tr>");
        }

        output.AppendLine("                    </tbody>");
        output.AppendLine("                </table>");
        output.AppendLine("            </div>");
    }

    private void FormatChangeGroup(StringBuilder output, List<ApiDifference> changes, string changeType)
    {
        var groupedChanges = changes.GroupBy(c => c.ElementType).OrderBy(g => g.Key);

        foreach (var group in groupedChanges)
        {
            output.AppendLine($"            <div class=\"change-group\">");
            output.AppendLine($"                <h3>{group.Key} ({group.Count()})</h3>");
            output.AppendLine($"                <div class=\"change-items\">");

            foreach (var change in group.OrderBy(c => c.ElementName))
            {
                output.AppendLine($"                    <div class=\"change-item {changeType}\">");
                output.AppendLine($"                        <div class=\"change-header\">");
                output.AppendLine($"                            <div class=\"change-name\">");
                output.AppendLine($"                                <code>{change.ElementName}</code>");
                if (change.IsBreakingChange)
                {
                    output.AppendLine($"                                <span class=\"breaking-badge\">BREAKING</span>");
                }
                output.AppendLine($"                            </div>");
                output.AppendLine($"                            <div class=\"change-description\">{change.Description}</div>");
                output.AppendLine($"                        </div>");

                // Add signature details if available
                if (!string.IsNullOrEmpty(change.OldSignature) || !string.IsNullOrEmpty(change.NewSignature))
                {
                    string detailsId = $"details-{Guid.NewGuid():N}";
                    output.AppendLine($"                        <div class=\"signature-toggle\">");
                    output.AppendLine($"                            <button class=\"toggle-btn\" onclick=\"toggleSignature('{detailsId}')\">");
                    output.AppendLine($"                                <span class=\"toggle-icon\">▼</span> View Signature Details");
                    output.AppendLine($"                            </button>");
                    output.AppendLine($"                        </div>");
                    output.AppendLine($"                        <div id=\"{detailsId}\" class=\"signature-details\" style=\"display: none;\">");

                    if (!string.IsNullOrEmpty(change.OldSignature))
                    {
                        output.AppendLine($"                            <div class=\"signature-section\">");
                        output.AppendLine($"                                <h4>Old Signature:</h4>");
                        output.AppendLine($"                                <pre><code class=\"csharp\">{change.OldSignature}</code></pre>");
                        output.AppendLine($"                            </div>");
                    }

                    if (!string.IsNullOrEmpty(change.NewSignature))
                    {
                        output.AppendLine($"                            <div class=\"signature-section\">");
                        output.AppendLine($"                                <h4>New Signature:</h4>");
                        output.AppendLine($"                                <pre><code class=\"csharp\">{change.NewSignature}</code></pre>");
                        output.AppendLine($"                            </div>");
                    }

                    output.AppendLine($"                        </div>");
                }

                output.AppendLine($"                    </div>");
            }

            output.AppendLine($"                </div>");
            output.AppendLine($"            </div>");
        }
    }

    private string GetCssStyles()
    {
        return @"
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f8f9fa;
        }

        .container {
            max-width: 1200px;
            margin: 0 auto;
            padding: 20px;
        }

        header {
            text-align: center;
            margin-bottom: 30px;
            padding: 20px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            border-radius: 8px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }

        header h1 {
            font-size: 2.5rem;
            margin-bottom: 10px;
        }

        .report-info {
            font-size: 1.1rem;
            opacity: 0.9;
        }

        section {
            margin: 30px 0;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }

        section h2 {
            background: #f8f9fa;
            padding: 15px 20px;
            margin: 0;
            border-bottom: 1px solid #dee2e6;
            font-size: 1.5rem;
        }

        .metadata-content {
            padding: 20px;
        }

        .assembly-info {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-bottom: 30px;
        }

        .assembly-card {
            padding: 20px;
            border-radius: 8px;
            border-left: 4px solid;
            background: #f8f9fa;
        }

        .assembly-card.source {
            border-left-color: #007bff;
        }

        .assembly-card.target {
            border-left-color: #28a745;
        }

        .assembly-label {
            font-size: 0.9rem;
            font-weight: 600;
            color: #6c757d;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 8px;
        }

        .assembly-name {
            font-size: 1.1rem;
            font-weight: bold;
            color: #212529;
            margin-bottom: 6px;
            word-break: break-all;
        }

        .assembly-path {
            font-size: 0.85rem;
            color: #6c757d;
            font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
            background: #e9ecef;
            padding: 4px 8px;
            border-radius: 4px;
            word-break: break-all;
        }

        .stats-row {
            display: flex;
            justify-content: center;
            gap: 30px;
            flex-wrap: wrap;
        }

        .stat-item {
            text-align: center;
            padding: 15px 20px;
            background: #f8f9fa;
            border-radius: 8px;
            min-width: 120px;
        }

        .stat-item.breaking {
            background: #fff5f5;
            border: 1px solid #f8d7da;
        }

        .stat-value {
            font-size: 1.8rem;
            font-weight: bold;
            color: #212529;
            margin-bottom: 4px;
        }

        .stat-item.breaking .stat-value {
            color: #dc3545;
        }

        .stat-label {
            font-size: 0.9rem;
            color: #6c757d;
            font-weight: 500;
        }

        .summary-cards {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: 20px;
            padding: 20px;
        }

        .summary-card {
            text-align: center;
            padding: 20px;
            border-radius: 8px;
            color: white;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        }

        .summary-card.added { background: linear-gradient(135deg, #28a745, #20c997); }
        .summary-card.removed { background: linear-gradient(135deg, #dc3545, #e83e8c); }
        .summary-card.modified { background: linear-gradient(135deg, #ffc107, #fd7e14); }
        .summary-card.breaking { background: linear-gradient(135deg, #dc3545, #6f42c1); }

        .card-number {
            font-size: 2.5rem;
            font-weight: bold;
            margin-bottom: 5px;
        }

        .card-label {
            font-size: 1rem;
            opacity: 0.9;
        }

        .alert {
            padding: 15px 20px;
            margin: 20px;
            border-radius: 6px;
            border-left: 4px solid;
        }

        .alert-danger {
            background: #fff5f5;
            border-left-color: #dc3545;
            color: #721c24;
        }

        /* Configuration Section Styles */
        .config-toggle {
            padding: 20px;
            text-align: center;
        }

        .toggle-button {
            background: #007bff;
            color: white;
            border: none;
            padding: 12px 24px;
            border-radius: 6px;
            cursor: pointer;
            font-size: 1rem;
            transition: background-color 0.3s ease;
            display: inline-flex;
            align-items: center;
            gap: 8px;
        }

        .toggle-button:hover {
            background: #0056b3;
        }

        .toggle-icon {
            font-family: monospace;
            font-weight: bold;
        }

        .config-details {
            padding: 20px;
        }

        .config-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(380px, 1fr));
            gap: 20px;
        }

        .config-section {
            background: #f8f9fa;
            border-radius: 8px;
            padding: 20px;
            border-left: 4px solid #007bff;
        }

        .config-section h3 {
            margin: 0 0 15px 0;
            color: #495057;
            font-size: 1.2rem;
        }

        .config-item {
            margin-bottom: 12px;
        }

        .config-label {
            font-weight: 600;
            color: #495057;
            display: block;
            margin-bottom: 4px;
            word-wrap: break-word;
        }
        }

        .config-value {
            color: #212529;
            font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
        }

        .config-empty {
            color: #6c757d;
            font-style: italic;
        }

        .config-path {
            word-break: break-all;
            background: #e9ecef;
            padding: 2px 6px;
            border-radius: 3px;
        }

        .boolean-true {
            color: #28a745;
            font-weight: bold;
        }

        .boolean-false {
            color: #dc3545;
            font-weight: bold;
        }

        /* Special layout for breaking change rules section to prevent wrapping */
        .config-section.breaking-rules {
            grid-column: span 2;
        }

        .config-section.breaking-rules .config-grid-inner {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 15px;
        }

        .config-list {
            margin-top: 8px;
        }

        .config-list-item {
            display: inline-block;
            background: #e9ecef;
            padding: 4px 8px;
            margin: 2px 4px 2px 0;
            border-radius: 4px;
            font-size: 0.9rem;
            font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
        }

        .config-mappings {
            margin-top: 8px;
        }

        .config-mapping {
            display: flex;
            align-items: center;
            gap: 8px;
            margin: 8px 0;
            padding: 8px;
            background: white;
            border-radius: 4px;
            border: 1px solid #dee2e6;
        }

        .mapping-from, .mapping-to {
            font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 0.9rem;
        }

        .mapping-from {
            background: #e3f2fd;
            color: #1565c0;
        }

        .mapping-to {
            background: #e8f5e8;
            color: #2e7d32;
        }

        .mapping-arrow {
            color: #6c757d;
            font-weight: bold;
        }

        .mapping-to-list {
            display: flex;
            flex-direction: column;
            gap: 4px;
        }

        .breaking-changes-table {
            padding: 20px;
            overflow-x: auto;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
        }

        th, td {
            text-align: left;
            padding: 12px;
            border-bottom: 1px solid #dee2e6;
        }

        th {
            background: #f8f9fa;
            font-weight: 600;
            color: #495057;
        }

        .severity {
            padding: 4px 8px;
            border-radius: 4px;
            font-size: 0.8rem;
            font-weight: bold;
            text-transform: uppercase;
        }

        .severity.critical { background: #dc3545; color: white; }
        .severity.error { background: #fd7e14; color: white; }
        .severity.warning { background: #ffc107; color: #212529; }
        .severity.info { background: #17a2b8; color: white; }

        .change-group {
            margin: 20px;
        }

        .change-group h3 {
            color: #495057;
            margin-bottom: 15px;
            padding-bottom: 8px;
            border-bottom: 2px solid #dee2e6;
        }

        .change-items {
            display: grid;
            gap: 10px;
        }

        .change-item {
            border: 1px solid #dee2e6;
            border-radius: 6px;
            overflow: hidden;
        }

        .change-item.added { border-left: 4px solid #28a745; }
        .change-item.removed { border-left: 4px solid #dc3545; }
        .change-item.modified { border-left: 4px solid #ffc107; }
        .change-item.moved { border-left: 4px solid #17a2b8; }
        .change-item.excluded { border-left: 4px solid #6c757d; }

        .change-header {
            padding: 15px;
            background: #f8f9fa;
        }

        .change-name {
            display: flex;
            align-items: center;
            gap: 10px;
            margin-bottom: 8px;
        }

        .change-name code {
            background: #e9ecef;
            padding: 4px 8px;
            border-radius: 4px;
            font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
            font-size: 0.9rem;
        }

        .breaking-badge {
            background: #dc3545;
            color: white;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 0.7rem;
            font-weight: bold;
        }

        .change-description {
            color: #6c757d;
            font-size: 0.95rem;
        }

        .signature-toggle {
            padding: 0 15px 15px;
        }

        .toggle-btn {
            background: #007bff;
            color: white;
            border: none;
            padding: 8px 12px;
            border-radius: 4px;
            cursor: pointer;
            font-size: 0.9rem;
            transition: background-color 0.2s;
        }

        .toggle-btn:hover {
            background: #0056b3;
        }

        .toggle-icon {
            transition: transform 0.2s;
        }

        .toggle-btn.expanded .toggle-icon {
            transform: rotate(180deg);
        }

        .signature-details {
            padding: 15px;
            background: #f8f9fa;
            border-top: 1px solid #dee2e6;
        }

        .signature-section {
            margin-bottom: 15px;
        }

        .signature-section h4 {
            color: #495057;
            margin-bottom: 8px;
            font-size: 1rem;
        }

        .signature-section pre {
            background: #2d3748;
            color: #e2e8f0;
            padding: 15px;
            border-radius: 6px;
            overflow-x: auto;
            font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
            font-size: 0.9rem;
            line-height: 1.4;
        }

        .section-description {
            padding: 0 20px 10px;
            color: #6c757d;
            font-style: italic;
        }

        @media (max-width: 768px) {
            .container {
                padding: 10px;
            }

            header h1 {
                font-size: 2rem;
            }

            .assembly-info {
                grid-template-columns: 1fr;
            }

            .stats-row {
                flex-direction: column;
                align-items: center;
                gap: 15px;
            }

            .summary-cards {
                grid-template-columns: repeat(2, 1fr);
            }
        }
        ";
    }

    private string GetJavaScript()
    {
        return @"
        function toggleSignature(detailsId) {
            const details = document.getElementById(detailsId);
            const button = details.previousElementSibling.querySelector('.toggle-btn');
            const icon = button.querySelector('.toggle-icon');

            if (details.style.display === 'none') {
                details.style.display = 'block';
                button.classList.add('expanded');
                button.innerHTML = '<span class=""toggle-icon"">▲</span> Hide Signature Details';
            } else {
                details.style.display = 'none';
                button.classList.remove('expanded');
                button.innerHTML = '<span class=""toggle-icon"">▼</span> View Signature Details';
            }
        }

        function toggleConfig() {
            const details = document.getElementById('config-details');
            const button = document.querySelector('.toggle-button');
            const icon = button.querySelector('.toggle-icon');
            const text = button.querySelector('.toggle-text');

            if (details.style.display === 'none') {
                details.style.display = 'block';
                icon.textContent = '▼';
                text.textContent = 'Hide Configuration Details';
            } else {
                details.style.display = 'none';
                icon.textContent = '▶';
                text.textContent = 'Show Configuration Details';
            }
        }
        ";
    }

    /// <summary>
    /// Formats the configuration details for the HTML report
    /// </summary>
    /// <param name="output">StringBuilder to append the formatted configuration to</param>
    /// <param name="config">Configuration object to format</param>
    private void FormatConfiguration(StringBuilder output, DotNetApiDiff.Models.Configuration.ComparisonConfiguration config)
    {
        output.AppendLine("                <div class=\"config-grid\">");

        // Filters Section
        output.AppendLine("                    <div class=\"config-section\">");
        output.AppendLine("                        <h3>🔍 Filters</h3>");
        output.AppendLine("                        <div class=\"config-item\">");
        output.AppendLine($"                            <span class=\"config-label\">Include Internals:</span> <span class=\"config-value\">{FormatBooleanValue(config.Filters.IncludeInternals)}</span>");
        output.AppendLine("                        </div>");
        output.AppendLine("                        <div class=\"config-item\">");
        output.AppendLine($"                            <span class=\"config-label\">Include Compiler Generated:</span> <span class=\"config-value\">{FormatBooleanValue(config.Filters.IncludeCompilerGenerated)}</span>");
        output.AppendLine("                        </div>");
        FormatStringList(output, "Include Namespaces", config.Filters.IncludeNamespaces);
        FormatStringList(output, "Exclude Namespaces", config.Filters.ExcludeNamespaces);
        FormatStringList(output, "Include Types", config.Filters.IncludeTypes);
        FormatStringList(output, "Exclude Types", config.Filters.ExcludeTypes);
        output.AppendLine("                    </div>");

        // Mappings Section
        output.AppendLine("                    <div class=\"config-section\">");
        output.AppendLine("                        <h3>🔗 Mappings</h3>");
        output.AppendLine("                        <div class=\"config-item\">");
        output.AppendLine($"                            <span class=\"config-label\">Auto Map Same Name Types:</span> <span class=\"config-value\">{FormatBooleanValue(config.Mappings.AutoMapSameNameTypes)}</span>");
        output.AppendLine("                        </div>");
        output.AppendLine("                        <div class=\"config-item\">");
        output.AppendLine($"                            <span class=\"config-label\">Ignore Case:</span> <span class=\"config-value\">{FormatBooleanValue(config.Mappings.IgnoreCase)}</span>");
        output.AppendLine("                        </div>");
        FormatMappingDictionary(output, "Type Mappings", config.Mappings.TypeMappings);
        FormatNamespaceMappings(output, "Namespace Mappings", config.Mappings.NamespaceMappings);
        output.AppendLine("                    </div>");

        // Exclusions Section
        output.AppendLine("                    <div class=\"config-section\">");
        output.AppendLine("                        <h3>❌ Exclusions</h3>");
        FormatStringList(output, "Excluded Types", config.Exclusions.ExcludedTypes);
        FormatStringList(output, "Excluded Members", config.Exclusions.ExcludedMembers);
        FormatStringList(output, "Excluded Type Patterns", config.Exclusions.ExcludedTypePatterns);
        FormatStringList(output, "Excluded Member Patterns", config.Exclusions.ExcludedMemberPatterns);
        output.AppendLine("                    </div>");

        // Breaking Change Rules Section
        output.AppendLine("                    <div class=\"config-section breaking-rules\">");
        output.AppendLine("                        <h3>⚠️ Breaking Change Rules</h3>");
        output.AppendLine("                        <div class=\"config-grid-inner\">");
        output.AppendLine("                            <div>");
        output.AppendLine("                                <div class=\"config-item\">");
        output.AppendLine($"                                    <span class=\"config-label\">Treat Type Removal as Breaking:</span> <span class=\"config-value\">{FormatBooleanValue(config.BreakingChangeRules.TreatTypeRemovalAsBreaking)}</span>");
        output.AppendLine("                                </div>");
        output.AppendLine("                                <div class=\"config-item\">");
        output.AppendLine($"                                    <span class=\"config-label\">Treat Member Removal as Breaking:</span> <span class=\"config-value\">{FormatBooleanValue(config.BreakingChangeRules.TreatMemberRemovalAsBreaking)}</span>");
        output.AppendLine("                                </div>");
        output.AppendLine("                            </div>");
        output.AppendLine("                            <div>");
        output.AppendLine("                                <div class=\"config-item\">");
        output.AppendLine($"                                    <span class=\"config-label\">Treat Signature Change as Breaking:</span> <span class=\"config-value\">{FormatBooleanValue(config.BreakingChangeRules.TreatSignatureChangeAsBreaking)}</span>");
        output.AppendLine("                                </div>");
        output.AppendLine("                                <div class=\"config-item\">");
        output.AppendLine($"                                    <span class=\"config-label\">Treat Reduced Accessibility as Breaking:</span> <span class=\"config-value\">{FormatBooleanValue(config.BreakingChangeRules.TreatReducedAccessibilityAsBreaking)}</span>");
        output.AppendLine("                                </div>");
        output.AppendLine("                            </div>");
        output.AppendLine("                        </div>");
        output.AppendLine("                    </div>");

        // General Settings Section
        output.AppendLine("                    <div class=\"config-section\">");
        output.AppendLine("                        <h3>⚙️ General Settings</h3>");
        output.AppendLine("                        <div class=\"config-item\">");
        output.AppendLine($"                            <span class=\"config-label\">Output Format:</span> <span class=\"config-value\">{config.OutputFormat}</span>");
        output.AppendLine("                        </div>");
        output.AppendLine("                        <div class=\"config-item\">");
        output.AppendLine($"                            <span class=\"config-label\">Fail On Breaking Changes:</span> <span class=\"config-value\">{FormatBooleanValue(config.FailOnBreakingChanges)}</span>");
        output.AppendLine("                        </div>");
        if (!string.IsNullOrEmpty(config.OutputPath))
        {
            output.AppendLine("                        <div class=\"config-item\">");
            output.AppendLine($"                            <span class=\"config-label\">Output Path:</span> <span class=\"config-value config-path\">{config.OutputPath}</span>");
            output.AppendLine("                        </div>");
        }
        output.AppendLine("                    </div>");

        output.AppendLine("                </div>");
    }

    /// <summary>
    /// Formats a boolean value with appropriate styling
    /// </summary>
    private string FormatBooleanValue(bool value)
    {
        return value ? "<span class=\"boolean-true\">✓ True</span>" : "<span class=\"boolean-false\">✗ False</span>";
    }

    /// <summary>
    /// Formats a list of strings for display
    /// </summary>
    private void FormatStringList(StringBuilder output, string label, List<string> items)
    {
        output.AppendLine("                        <div class=\"config-item\">");
        output.AppendLine($"                            <span class=\"config-label\">{label}:</span>");

        if (items == null || items.Count == 0)
        {
            output.AppendLine("                            <span class=\"config-value config-empty\">(None)</span>");
        }
        else
        {
            output.AppendLine("                            <div class=\"config-list\">");
            foreach (var item in items)
            {
                output.AppendLine($"                                <span class=\"config-list-item\">{item}</span>");
            }
            output.AppendLine("                            </div>");
        }
        output.AppendLine("                        </div>");
    }

    /// <summary>
    /// Formats a mapping dictionary for display
    /// </summary>
    private void FormatMappingDictionary(StringBuilder output, string label, Dictionary<string, string> mappings)
    {
        output.AppendLine("                        <div class=\"config-item\">");
        output.AppendLine($"                            <span class=\"config-label\">{label}:</span>");

        if (mappings == null || mappings.Count == 0)
        {
            output.AppendLine("                            <span class=\"config-value config-empty\">(None)</span>");
        }
        else
        {
            output.AppendLine("                            <div class=\"config-mappings\">");
            foreach (var mapping in mappings)
            {
                output.AppendLine($"                                <div class=\"config-mapping\">");
                output.AppendLine($"                                    <span class=\"mapping-from\">{mapping.Key}</span>");
                output.AppendLine($"                                    <span class=\"mapping-arrow\">→</span>");
                output.AppendLine($"                                    <span class=\"mapping-to\">{mapping.Value}</span>");
                output.AppendLine($"                                </div>");
            }
            output.AppendLine("                            </div>");
        }
        output.AppendLine("                        </div>");
    }

    /// <summary>
    /// Formats namespace mappings (one-to-many) for display
    /// </summary>
    private void FormatNamespaceMappings(StringBuilder output, string label, Dictionary<string, List<string>> mappings)
    {
        output.AppendLine("                        <div class=\"config-item\">");
        output.AppendLine($"                            <span class=\"config-label\">{label}:</span>");

        if (mappings == null || mappings.Count == 0)
        {
            output.AppendLine("                            <span class=\"config-value config-empty\">(None)</span>");
        }
        else
        {
            output.AppendLine("                            <div class=\"config-mappings\">");
            foreach (var mapping in mappings)
            {
                output.AppendLine($"                                <div class=\"config-mapping\">");
                output.AppendLine($"                                    <span class=\"mapping-from\">{mapping.Key}</span>");
                output.AppendLine($"                                    <span class=\"mapping-arrow\">→</span>");
                output.AppendLine($"                                    <div class=\"mapping-to-list\">");
                foreach (var target in mapping.Value)
                {
                    output.AppendLine($"                                        <span class=\"mapping-to\">{target}</span>");
                }
                output.AppendLine($"                                    </div>");
                output.AppendLine($"                                </div>");
            }
            output.AppendLine("                            </div>");
        }
        output.AppendLine("                        </div>");
    }
}
